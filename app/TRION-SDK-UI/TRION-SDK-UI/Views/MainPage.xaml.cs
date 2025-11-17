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
            var pt = e.GetPosition(MauiPlot1);
            if (pt is null)
                return;

            var pixel = new Pixel(pt.Value.X, pt.Value.Y);
            var coords = MauiPlot1.Plot.GetCoordinates(pixel);
            var rd = MauiPlot1.Plot.LastRender;
            DataPoint best = DataPoint.None;
            DataLogger? bestLogger = null;
            double bestDx = double.MaxValue;

            foreach (var dl in _loggers.Values)
            {
                var dp = dl.GetNearestX(coords, rd.DataRect, maxDistance: 25);

                double dx = Math.Abs(dp.X - coords.X);
                if (dx < bestDx)
                {
                    bestDx = dx;
                    best = dp;
                    bestLogger = dl;
                }
            }

            
            // With this block:
            if (!best.IsReal || bestLogger is null)
            {
                return;
            }
            _crosshair.X = best.X;
            _crosshair.Y = best.Y;

            // Build label text
            CursorLabelText.Text =
                $"{bestLogger.LegendText}\nX: {best.X:F3}\nY: {best.Y:F3}";

            // Position label (clamp inside plot area)
            double targetX = pixel.X + CursorLabelOffsetX;
            double targetY = pixel.Y + CursorLabelOffsetY;

            // After first measure Width/Height may be 0; allow translation anyway
            double maxX = MauiPlot1.Width - CursorLabel.Width - 4;
            double maxY = MauiPlot1.Height - CursorLabel.Height - 4;

            if (maxX > 0 && targetX > maxX) targetX = maxX;
            if (maxY > 0 && targetY > maxY) targetY = maxY;

            CursorLabel.TranslationX = targetX;
            CursorLabel.TranslationY = targetY;

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