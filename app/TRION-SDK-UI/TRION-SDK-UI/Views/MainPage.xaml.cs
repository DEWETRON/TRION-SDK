using ScottPlot;
using ScottPlot.Plottables;
using System.Collections.Specialized;
using System.Diagnostics;
using TRION_SDK_UI.Models;
using TRION_SDK_UI.ViewModels;

namespace TRION_SDK_UI
{
    public partial class MainPage : ContentPage
    {
        double _startWidth;
        private readonly Dictionary<string, DataLogger> _loggers = [];
        private readonly ScottPlot.Palettes.Category10 Palette = new();
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];
        private Crosshair _crosshair = null!;

        // Optional: offset for label so it does not sit directly under the cursor
        private const double CursorLabelOffsetX = 14;
        private const double CursorLabelOffsetY = 14;

        void OnPointerEntered(object sender, PointerEventArgs e)
        {
            _crosshair.IsVisible = true;
            CursorLabel.IsVisible = true;
            MauiPlot1.Refresh();
        }

        void OnPointerExited(object sender, PointerEventArgs e)
        {
            _crosshair.IsVisible = false;
            CursorLabel.IsVisible = false;
            MauiPlot1.Refresh();
        }

        void OnPointerMoved(object sender, PointerEventArgs e)
        {
            var pointerPos = e.GetPosition(MauiPlot1);
            if (pointerPos is null)
                return;

            var cursorPixel = new Pixel(pointerPos.Value.X, pointerPos.Value.Y);
            //Debug.WriteLine($"CursorPixel = {cursorPixel}");
            var cursorCoordinates = MauiPlot1.Plot.GetCoordinates(cursorPixel);

            var lastRender = MauiPlot1.Plot.LastRender;

            DataPoint nearestPoint = DataPoint.None;
            DataLogger? nearestLogger = null;
            double minDeltaX = double.MaxValue;

            foreach (var logger in _loggers.Values)
            {
                // Skip series without data to avoid index issues
                if (logger.Data.Coordinates.Count == 0)
                    continue;

                var candidatePoint = logger.GetNearestX(cursorCoordinates, lastRender.DataRect, maxDistance: 5);
                Debug.WriteLine($"  CandidatePoint from '{logger.LegendText}' = {candidatePoint.X} - {cursorCoordinates.X})");

                double deltaX = Math.Abs(candidatePoint.X - cursorCoordinates.X);
                if (deltaX >= minDeltaX)
                    continue;


                minDeltaX = deltaX;
                nearestPoint = candidatePoint;
                nearestLogger = logger;
            }

            if (!nearestPoint.IsReal || nearestLogger is null)
                return;

            _crosshair.X = nearestPoint.X;
            _crosshair.Y = nearestPoint.Y;

            CursorLabelText.Text = $"{nearestLogger.LegendText}\nX: {nearestPoint.X:F3}\nY: {nearestPoint.Y:F3}";

            double labelX = cursorPixel.X + CursorLabelOffsetX;
            double labelY = cursorPixel.Y + CursorLabelOffsetY;

            double maxLabelX = MauiPlot1.Width - CursorLabel.Width - 4;
            double maxLabelY = MauiPlot1.Height - CursorLabel.Height - 4;

            if (maxLabelX > 0 && labelX > maxLabelX) labelX = maxLabelX;
            if (maxLabelY > 0 && labelY > maxLabelY) labelY = maxLabelY;

            CursorLabel.TranslationX = labelX;
            CursorLabel.TranslationY = labelY;

            MauiPlot1.Refresh();
        }

        private ScottPlot.Color GetColorForChannel(string channelKey)
        {
            if (_lineColors.TryGetValue(channelKey, out var c)) return c;
            int idx = Math.Abs(channelKey.GetHashCode()) % 10;
            var color = Palette.GetColor(idx);
            _lineColors[channelKey] = color;
            return color;
        }

        private DataLogger GetOrCreateLogger(string channelKey)
        {
            if (_loggers.TryGetValue(channelKey, out var existing))
                return existing;

            var dl = MauiPlot1.Plot.Add.DataLogger();
            dl.LineWidth = 2;
            dl.LegendText = channelKey;
            dl.Color = GetColorForChannel(channelKey);
            dl.ViewSlide(22.0);
            _loggers[channelKey] = dl;
            return dl;
        }

        public MainPage()
        {
            InitializeComponent();

            BindingContext = new MainViewModel();
            var vm = (MainViewModel)BindingContext;
            vm.AcquisitionStarting += VmOnAcquisitionStarted;
            vm.SamplesBatchAppended += VmOnSamplesBatchAppended;
            vm.LogMessages.CollectionChanged += VmLogMessagesCollectionChanged;

            MauiPlot1.Plot.Title("Live Signals");
            MauiPlot1.Plot.XLabel("Elapsed Seconds");
            MauiPlot1.Plot.YLabel("Value");
            MauiPlot1.Plot.Axes.Hairline(true);
            MauiPlot1.Plot.Axes.ContinuouslyAutoscale = false;

            _crosshair = MauiPlot1.Plot.Add.Crosshair(0, 0);
            _crosshair.IsVisible = false;

            MauiPlot1.Refresh();

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);
        }

        private static (double[] ys, double[] xs) ConvertSamplesToXYArrays(ReadOnlySpan<Sample> samples)
        {
            int n = samples.Length;
            var ys = GC.AllocateUninitializedArray<double>(n);
            var xs = GC.AllocateUninitializedArray<double>(n);
            for (int i = 0; i < n; i++)
            {
                ref readonly var s = ref samples[i];
                ys[i] = s.Value;
                xs[i] = s.ElapsedSeconds;
            }
            return (ys, xs);
        }

        private void VmOnSamplesBatchAppended(object? sender, MainViewModel.SamplesBatchAppendedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var (channelKey, samples) in e.Batches)
                {
                    if (samples is { Length: > 0 })
                    {
                        var dl = GetOrCreateLogger(channelKey);
                        dl.ManageAxisLimits = vm.FollowLatest;
                        var (ys, xs) = ConvertSamplesToXYArrays(samples);
                        dl.Add(xs, ys);
                    }
                }
                MauiPlot1.Refresh();
            });
        }

        private void VmLogMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is { Count: > 0 })
            {
                var last = e.NewItems[^1];
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LogView?.ScrollTo(last, position: ScrollToPosition.End, animate: true);
                });
            }
        }

        private void VmOnAcquisitionStarted(object? sender, IReadOnlyList<Channel> channels)
        {
            var keys = channels.Select(ch => $"{ch.BoardID}/{ch.Name}").ToHashSet();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _loggers.Clear();
                MauiPlot1.Plot.Clear();
                MauiPlot1.Plot.Axes.ContinuouslyAutoscale = false;

                foreach (var key in keys)
                    _ = GetOrCreateLogger(key);

                _crosshair = MauiPlot1.Plot.Add.Crosshair(0, 0);
                _crosshair.IsVisible = CursorLabel.IsVisible; // keep sync

                MauiPlot1.Refresh();
            });
        }

        private void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startWidth = SidebarColumn.Width.Value;
                    break;
                case GestureStatus.Running:
                    double newWidth = _startWidth - e.TotalX;
                    if (newWidth < 100) newWidth = 100;
                    if (newWidth > 400) newWidth = 400;
                    SidebarColumn.Width = new GridLength(newWidth);
                    break;
            }
        }
    }
}