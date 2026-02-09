using ScottPlot;
using ScottPlot.Maui;
using ScottPlot.Plottables;
using TRION_SDK_UI.POCO;

namespace TRION_SDK_UI.Models
{
    public class RecorderGraph(MauiPlot mauiPlot, 
                               Border cursorLabel, 
                               Microsoft.Maui.Controls.Label cursorLabelText,
                               VerticalLine lockLine,
                               Crosshair crossHair)
    {
        private readonly Plot _plot = mauiPlot.Plot;
        private readonly Border _cursorLabel = cursorLabel;
        private readonly MauiPlot _mauiPlot = mauiPlot;
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];
        private readonly Dictionary<string, DataLogger> _loggers = [];
        private readonly Microsoft.Maui.Controls.Label _cursorLabelText = cursorLabelText;
        private readonly ScottPlot.Palettes.Category10 _palette = new();
        private const double _cursorLabelOffsetX = 14;
        private const double _cursorLabelOffsetY = 14;
        private const double _viewWidthSeconds = 2.2;
        private VerticalLine _lockLine = lockLine;
        private Crosshair _crosshair = crossHair;
        private Coordinates _lastCursorCoordinates;
        private bool _hasCursor;

        public double StartWidth { get; set; }
        public bool IsScrollLocked;

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
                _ = GerOrCreateDataLogger(key);
            }

            _crosshair = _plot.Add.Crosshair(0, 0);
            _crosshair.IsVisible = !IsScrollLocked && _cursorLabel.IsVisible;

            _lockLine = _plot.Add.VerticalLine(0);
            _lockLine.IsVisible = IsScrollLocked && _cursorLabel.IsVisible;

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
            var cursorCoordinates = _mauiPlot.Plot.GetCoordinates(cursorPixel);
            var lastRender = _mauiPlot.Plot.LastRender;

            _lastCursorCoordinates = cursorCoordinates;
            _hasCursor = true;

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

            double labelX = cursorPixel.X + _cursorLabelOffsetX;
            double labelY = cursorPixel.Y + _cursorLabelOffsetY;

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

            _mauiPlot.Refresh();
        }
        public void RenderLine(Pixel cursorPixel, Coordinates cursorCoordinates)
        {
            _lockLine.X = cursorCoordinates.X;
            UpdateValuesAtLockLine();

            double labelX = cursorPixel.X + _cursorLabelOffsetX;
            double labelY = cursorPixel.Y + _cursorLabelOffsetY;

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

            _mauiPlot.Refresh();
        }

        private DataLogger GerOrCreateDataLogger(string channelKey)
        {
           if (_loggers.TryGetValue(channelKey, out var existing))
            {
                return existing;
            }
            var newDataLogger = _plot.Add.DataLogger();
            newDataLogger.ViewSlide(_viewWidthSeconds);
            newDataLogger.LineWidth = 2;
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
            var dataLogger = GerOrCreateDataLogger(channelKey);
            dataLogger.ManageAxisLimits = followLatest;
            var (ySamples, xSamples) = ConvertSamplesToXYArrays(samples);
            dataLogger.Add(xSamples, ySamples);
        }
    }
}
