using ScottPlot;
using ScottPlot.Maui;
using ScottPlot.Plottables;
using TRION_SDK_UI.POCO;

namespace TRION_SDK_UI.Models
{
    public class RecorderGraph
    {
        private readonly Plot _plot;
        private readonly Border _cursorLabel;
        private readonly MauiPlot _mauiPlot;
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];
        private readonly Dictionary<string, DataLogger> _loggers = [];
        private readonly Microsoft.Maui.Controls.Label _cursorLabelText;
        private readonly ScottPlot.Palettes.Category10 _palette = new();
        private const double _cursorLabelOffsetX = 14;
        private const double _cursorLabelOffsetY = 14;
        private const double _viewWidthSeconds = 2.2;
        private const double _markerGrabTolerancePx = 12;
        private VerticalLine _lockLine;
        private Crosshair _crosshair;
        private Coordinates _lastCursorCoordinates;
        private bool _hasCursor;
        private VerticalLine? _markerA;
        private VerticalLine? _markerB;
        private double? _markerAx;
        private double? _markerBx;
        private HorizontalSpan? _calculationSpan;
        private enum DragTarget { NONE, MARKER_A, MARKER_B }
        private DragTarget _dragTarget = DragTarget.NONE;
        private bool AnyDataPresent() => _loggers.Values.Any(l => l.Data.Coordinates.Count != 0);


        public bool IsScrollLocked;
        public bool IsDraggingMarker => _dragTarget != DragTarget.NONE;
        public Pixel LastCursorPixel { get; private set; }

        public RecorderGraph(MauiPlot mauiPlot,
                             Border cursorLabel,
                             Microsoft.Maui.Controls.Label cursorLabelText,
                             VerticalLine lockLine,
                             Crosshair crossHair)
        {
            _mauiPlot = mauiPlot;
            _plot = mauiPlot.Plot;
            _cursorLabel = cursorLabel;
            _cursorLabelText = cursorLabelText;
            _lockLine = lockLine;
            _crosshair = crossHair;

            SetLockCrossVisibility();
        }

        public bool TryBeginMarkerDrag(Pixel pixel)
        {
            if (!_markerAx.HasValue && !_markerBx.HasValue)
            {
                return false;
            }

            var lastRender = _plot.LastRender;
            double? distanceA = null;
            double? distanceB = null;

            if (_markerAx.HasValue)
            {
                distanceA = Math.Abs(pixel.X - DoubleXToPixelX(_markerAx.Value, lastRender));
            }

            if (_markerBx.HasValue)
            {
                distanceB = Math.Abs(pixel.X - DoubleXToPixelX(_markerBx.Value, lastRender));
            }

            DragTarget best = DragTarget.NONE;
            double bestDistance = _markerGrabTolerancePx;

            if (distanceA.HasValue && distanceA.Value < bestDistance)
            {
                bestDistance = distanceA.Value;
                best = DragTarget.MARKER_A;
            }
            if (distanceB.HasValue && distanceB.Value < bestDistance)
            {
                best = DragTarget.MARKER_B;
            }

            _dragTarget = best;
            return DragTarget.NONE != _dragTarget;
        }

        public void UpdateMarkerDrag(Pixel pixel)
        {
            if (DragTarget.NONE == _dragTarget)
            {
                return;
            }

            var coordinates = _mauiPlot.Plot.GetCoordinates(pixel);

            _lockLine.X = coordinates.X;

            switch (_dragTarget)
            {
                case DragTarget.MARKER_A when _markerA is not null:
                    _markerA.X = coordinates.X;
                    _markerAx = coordinates.X;
                    break;
                case DragTarget.MARKER_B when _markerB is not null:
                    _markerB.X = coordinates.X;
                    _markerBx = coordinates.X;
                    break;
            }

            UpdateCursorLabel(pixel);
            RebuildCalculationSpan();
            _mauiPlot.Refresh();
        }

        public void EndMarkerDrag()
        {
            _dragTarget = DragTarget.NONE;
        }

        public void PlaceRangeMarker()
        {
            if (!IsScrollLocked)
            {
                return;
            }

            if (_calculationSpan is not null)
            {
                _plot.Remove(_calculationSpan);
                _calculationSpan = null;
            }

            ClearRangeMarkers();

            var limits = _plot.Axes.GetLimits();
            var xMin = limits.Left;
            var xRange = _plot.Axes.Bottom.Range;
            var currentSpan = xRange.Span;

            var markerAStartPosition = xMin + (currentSpan * 1 / 3);
            var markerBStartPosition = xMin + (currentSpan * 2 / 3);

            _markerA = _plot.Add.VerticalLine(markerAStartPosition);
            _markerA.LineWidth = 2;
            _markerA.LineStyle.Color = ScottPlot.Colors.Cyan;
            _markerA.LineStyle.Pattern = LinePattern.Dashed;
            _markerAx = markerAStartPosition;

            _markerB = _plot.Add.VerticalLine(markerBStartPosition);
            _markerB.LineWidth = 2;
            _markerB.LineStyle.Color = ScottPlot.Colors.Cyan;
            _markerB.LineStyle.Pattern = LinePattern.Dashed;
            _markerBx = markerBStartPosition;
            
            RebuildCalculationSpan();
            _mauiPlot.Refresh();
        }

        public void ClearRangeMarkers()
        {
            if (_markerA is not null) 
            { 
                _plot.Remove(_markerA); 
                _markerA = null; 
            }
            if (_markerB is not null) 
            { 
                _plot.Remove(_markerB); 
                _markerB = null; 
            }
            if (_calculationSpan != null)
            {
                _plot.Remove(_calculationSpan);
                _calculationSpan = null;
            }
            _markerAx = null;
            _markerBx = null;
            _dragTarget = DragTarget.NONE;
            _mauiPlot.Refresh();
        }

        public List<ChannelRangeStats> ComputeRangeStats()
        {
            var results = new List<ChannelRangeStats>();

            if (!_markerAx.HasValue || !_markerBx.HasValue)
            {
                return results;
            }

            double xMin = Math.Min(_markerAx.Value, _markerBx.Value);
            double xMax = Math.Max(_markerAx.Value, _markerBx.Value);

            foreach (var (channelKey, logger) in _loggers)
            {
                var coordinates = logger.Data.Coordinates;
                if (0 == coordinates.Count)
                {
                    continue;
                }

                double min = double.MaxValue;
                double max = double.MinValue;
                double sum = 0;
                int count = 0;

                for (int i = 0; i < coordinates.Count; ++i)
                {
                    var c = coordinates[i];
                    if (c.X < xMin) continue;
                    if (c.X > xMax) break;

                    if (c.Y < min) min = c.Y;
                    if (c.Y > max) max = c.Y;
                    sum += c.Y;
                    count++;
                }

                if (count > 0)
                {
                    results.Add(new ChannelRangeStats(channelKey, min, max, sum / count, count));
                }
            }

            return results;
        }

        public void SetLockLineX()
        {
            if (IsScrollLocked)
            {
                _lockLine.X = _hasCursor ? _lastCursorCoordinates.X : _crosshair.X;
            }
        }


        public void SetLockCrossVisibility()
        {
            _crosshair ??= _mauiPlot.Plot.Add.Crosshair(0, 0);
            _lockLine ??= _mauiPlot.Plot.Add.VerticalLine(0);
            _cursorLabel.IsVisible = true;
            if (false == AnyDataPresent())
            {
                HideLockCross();
                return;
            }
            if (IsScrollLocked)
            {
                _lockLine.IsVisible = true;
                _crosshair.IsVisible = false;
            }
            else
            {
                _crosshair.IsVisible = true;
                _lockLine.IsVisible = false;
            }
            _mauiPlot.Refresh();
        }

        public void HideLockCross()
        {
            _crosshair.IsVisible = false;
            _lockLine.IsVisible = false;
            _cursorLabel.IsVisible = false;
            _mauiPlot.Refresh();
        }

        public void ApplyTheme()
        {
            if (Application.Current is null)
            {
                return;
            }
            PlotThemeUtil.ApplyTheme(_plot, Application.Current.RequestedTheme, _crosshair, _lockLine);
        }

        public void StartAcquisition(HashSet<string> keys)
        {
            _loggers.Clear();
            _plot.Clear();

            foreach (var key in keys)
            {
                _ = GetOrCreateDataLogger(key);
            }

            _crosshair = _plot.Add.Crosshair(0, 0);
            _lockLine = _plot.Add.VerticalLine(0);
            SetLockCrossVisibility();

            if (Application.Current is not null)
            {
                PlotThemeUtil.ApplyTheme(
                    _plot,
                    Application.Current.RequestedTheme,
                    _crosshair,
                    _lockLine
                );
            }

            if (IsScrollLocked)
            {
                UpdateValuesAtLockLine();
            }
            _mauiPlot.Refresh();
        }

        public void UpdatePointer(PointerEventArgs e)
        {
            var pointerPos = e.GetPosition(_mauiPlot);
            if (pointerPos is null) return;

            var cursorPixel = new Pixel(pointerPos.Value.X, pointerPos.Value.Y);
            LastCursorPixel = cursorPixel;
            var cursorCoordinates = _mauiPlot.Plot.GetCoordinates(cursorPixel);
            var lastRender = _mauiPlot.Plot.LastRender;

            _lastCursorCoordinates = cursorCoordinates;
            _hasCursor = true;

            if (DragTarget.NONE != _dragTarget)
            {
                UpdateMarkerDrag(cursorPixel);
                return;
            }

            if (IsScrollLocked)
            {
                RenderLine(cursorPixel, cursorCoordinates);
            }
            else
            {
                RenderCrosshair(cursorPixel, cursorCoordinates, lastRender);
            }
            _mauiPlot.Refresh();
        }

        public void UpdateValuesAtLockLine()
        {

            if (!IsScrollLocked && !_lockLine.IsVisible)
            {
                return;
            }

            var lastRender = _plot.LastRender;

            var x = _lockLine.X;
            var queryCoordinates = new Coordinates(x, 0);

            var lines = new List<string>();

            foreach (var logger in _loggers.Values)
            {
                if (0 == logger.Data.Coordinates.Count)
                {
                    continue;
                }

                var dp = logger.GetNearestX(queryCoordinates, lastRender.DataRect, maxDistance: 1_000_000);
                if (!dp.IsReal)
                {
                    dp = logger.GetNearest(queryCoordinates, lastRender.DataRect, maxDistance: 1_000_000);
                }

                if (!dp.IsReal)
                {
                    continue;
                }

                lines.Add($"{logger.LegendText}: {dp.Y:F3}");
            }

            if (0 == lines.Count) return;

            _cursorLabelText.Text = string.Join("\n", lines) + $"\nX: {x:F3}";
        }

        public void RenderCrosshair(Pixel cursorPixel, Coordinates cursorCoordinates, RenderDetails lastRender)
        {
            var nearestPoint = DataPoint.None;
            DataLogger? nearestLogger = null;
            var bestDistance = double.MaxValue;

            SetLockCrossVisibility();

            foreach (var logger in _loggers.Values)
            {
                if (logger.Data.Coordinates.Count == 0)
                {
                    continue;
                }

                var candidate = logger.GetNearest(cursorCoordinates, lastRender.DataRect, maxDistance: 128);
                if (!candidate.IsReal)
                {
                    continue;
                }

                double dx = candidate.X - cursorCoordinates.X;
                double dy = candidate.Y - cursorCoordinates.Y;
                double d2 = dx * dx + dy * dy;
                if (d2 >= bestDistance)
                {
                    continue;
                }

                bestDistance = d2;
                nearestPoint = candidate;
                nearestLogger = logger;
            }

            if (!nearestPoint.IsReal || nearestLogger is null)
            {
                return;
            }

            _crosshair.X = nearestPoint.X;
            _crosshair.Y = nearestPoint.Y;

            _cursorLabelText.Text = $"{nearestLogger.LegendText}\nX: {nearestPoint.X:F3}\nY: {nearestPoint.Y:F3}";

            UpdateCursorLabel(cursorPixel);
            _mauiPlot.Refresh();
        }
        public void RenderLine(Pixel cursorPixel, Coordinates cursorCoordinates)
        {
            _lockLine.X = cursorCoordinates.X;
            UpdateValuesAtLockLine();

            UpdateCursorLabel(cursorPixel);
            _mauiPlot.Refresh();
        }

        private void UpdateCursorLabel(Pixel targetPixel)
        {
            double labelX = targetPixel.X + _cursorLabelOffsetX;
            double labelY = targetPixel.Y + _cursorLabelOffsetY;

            double maxLabelX = _mauiPlot.Width - _cursorLabel.Width - 4;
            double maxLabelY = _mauiPlot.Height - _cursorLabel.Height - 4;
            if (maxLabelX > 0 && labelX > maxLabelX)
            {
                labelX = maxLabelX;
            }
            if (maxLabelY > 0 && labelY > maxLabelY)
            {
                labelY = maxLabelY;
            }

            _cursorLabel.TranslationX = labelX;
            _cursorLabel.TranslationY = labelY;
        }

        private DataLogger GetOrCreateDataLogger(string channelKey)
        {
           if (_loggers.TryGetValue(channelKey, out var existing))
            {
                return existing;
            }
            var newDataLogger = _plot.Add.DataLogger();
            newDataLogger.ViewSlide(_viewWidthSeconds);
            newDataLogger.LineWidth = 2;
            newDataLogger.LegendText = channelKey;
            newDataLogger.Color = GetColorForChannel(channelKey);
            _loggers[channelKey] = newDataLogger;
            return newDataLogger;
        }

        private static (double[] ys, double[] xs) ConvertSamplesToXYArrays(ReadOnlySpan<Sample> samples)
        {
            var ys = GC.AllocateUninitializedArray<double>(samples.Length);
            var xs = GC.AllocateUninitializedArray<double>(samples.Length);
            for (int i = 0; i < samples.Length; ++i)
            {
                ys[i] = samples[i].Value;
                xs[i] = samples[i].ElapsedSeconds;
            }
            return (ys, xs);
        }

        private ScottPlot.Color GetColorForChannel(string channelKey)
        {
            if (_lineColors.TryGetValue(channelKey, out var color))
            {
                return color;
            }
            var index = Math.Abs(channelKey.GetHashCode()) % 10;
            var newColor = _palette.GetColor(index);
            _lineColors[channelKey] = newColor;
            return newColor;
        }

        public void AddSamples(Sample[] samples, string channelKey, bool followLatest)
        {
            var dataLogger = GetOrCreateDataLogger(channelKey);
            dataLogger.ManageAxisLimits = followLatest;
            var (ySamples, xSamples) = ConvertSamplesToXYArrays(samples);
            dataLogger.Add(xSamples, ySamples);
        }

        private double DoubleXToPixelX(double dataX, RenderDetails lastRender)
        {
            var dataRect = lastRender.DataRect;
            var xRange = _plot.Axes.Bottom.Range;
            double fraction = (dataX - xRange.Min) / (xRange.Max - xRange.Min);
            return dataRect.Left + fraction * dataRect.Width;
        }

        private void RebuildCalculationSpan()
        {
            if (_calculationSpan is not null)
            {
                _plot.Remove(_calculationSpan);
                _calculationSpan = null;
            }

            if (_markerAx.HasValue && _markerBx.HasValue)
            {
                _calculationSpan = _plot.Add.HorizontalSpan(
                    _markerAx.Value,
                    _markerBx.Value,
                    ScottPlot.Colors.Cyan.WithAlpha(0.2));
            }
        }
    }
}
