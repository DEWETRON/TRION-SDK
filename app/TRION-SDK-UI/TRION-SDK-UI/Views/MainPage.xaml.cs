using System.ComponentModel;
using System.Collections.Specialized;
using ScottPlot;
using ScottPlot.Plottables;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI
{
    /// <summary>
    /// Main page: uses a ScottPlot DataStreamer per channel and feeds newest batches only.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        double _startWidth;

        // One DataStreamer per channel ("BoardID/ChannelName")
        private readonly Dictionary<string, DataStreamer> _streams = [];

        private readonly ScottPlot.Palettes.Category10 Palette = new();
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];

        private bool _needInitialAutoScaleX = false;

        // Visible window width in samples (and streamer capacity)
        private readonly int _followWindowSamples = 8000;

        private ScottPlot.Color GetColorForChannel(string channelKey)
        {
            if (_lineColors.TryGetValue(channelKey, out var c)) return c;
            int idx = Math.Abs(channelKey.GetHashCode()) % 10;
            var color = Palette.GetColor(idx);
            _lineColors[channelKey] = color;
            return color;
        }

        public MainPage()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            BindingContext = vm;

            vm.AcquisitionStarting += VmOnAcquisitionStarted;
            vm.SamplesAppended += VmOnSamplesAppended;
            vm.PropertyChanged += VmOnPropertyChanged;
            vm.LogMessages.CollectionChanged += VmLogMessages_CollectionChanged;

            MauiPlot1.Plot.Title("Live Signals");
            MauiPlot1.Plot.XLabel("Samples");
            MauiPlot1.Plot.YLabel("Value");
            MauiPlot1.Refresh();

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);
        }

        private void VmLogMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
            _streams.Clear();
            MauiPlot1.Plot.Clear();

            // We'll manage axes explicitly; streamers manage X while following
            MauiPlot1.Plot.Axes.ContinuouslyAutoscale = false;

            // Pre-create streamers for selected channels (optional; they can be created lazily too)
            foreach (var ch in channels)
            {
                string key = $"{ch.BoardID}/{ch.Name}";
                var ds = MauiPlot1.Plot.Add.DataStreamer(_followWindowSamples);
                ds.LineWidth = 2;
                ds.Color = GetColorForChannel(key);

                // Choose scrolling view (alternatively, ds.ViewWipeRight(0.1))
                ds.ViewScrollLeft();

                // Implicit X: 1 unit per sample (set to 1/sampleRate for seconds)
                ds.Data.SamplePeriod = 1;
                ds.Data.OffsetX = 0;

                // Let streamer manage X; toggled per-event by FollowLatest
                ds.ManageAxisLimits = true;

                _streams[key] = ds;
            }

            _needInitialAutoScaleX = true;
            MauiPlot1.Refresh();
        }

        private void VmOnSamplesAppended(object? sender, MainViewModel.SamplesAppendedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            // Ensure we have a streamer for this channel
            if (!_streams.TryGetValue(e.ChannelKey, out var ds))
            {
                ds = MauiPlot1.Plot.Add.DataStreamer(_followWindowSamples);
                ds.LineWidth = 2;
                ds.Color = GetColorForChannel(e.ChannelKey);
                ds.ViewScrollLeft();
                ds.Data.SamplePeriod = 1;
                ds.Data.OffsetX = 0;
                ds.ManageAxisLimits = true;
                _streams[e.ChannelKey] = ds;
            }

            // Feed only the newest batch (zero-copy via Span)
            if (e.Count > 0)
                ds.AddRange(e.Samples.ToArray());

            // Apply Y-axis limits from VM
            MauiPlot1.Plot.Axes.SetLimitsY(vm.YAxisMin, vm.YAxisMax);

            // One-time X autoscale after start so initial lines are visible
            if (_needInitialAutoScaleX)
            {
                MauiPlot1.Plot.Axes.AutoScaleX();
                _needInitialAutoScaleX = false;
            }

            // Follow behavior: let streamer manage X when following, freeze when not
            ds.ManageAxisLimits = vm.FollowLatest;

            MauiPlot1.Refresh();
        }

        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            if (e.PropertyName is nameof(MainViewModel.YAxisMin) or nameof(MainViewModel.YAxisMax))
            {
                MauiPlot1.Plot.Axes.SetLimitsY(vm.YAxisMin, vm.YAxisMax);
                MauiPlot1.Refresh();
            }
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