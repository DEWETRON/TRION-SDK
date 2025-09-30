using System.ComponentModel;
using System.Collections.Specialized;
using ScottPlot;
using ScottPlot.Plottables;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI
{
    /// <summary>
    /// Main page hosting the live plot and the side panel.
    ///
    /// Responsibilities:
    /// - Own a MainViewModel instance and wire its events to UI updates.
    /// - Plot per-channel data using one ScottPlot DataStreamer per channel.
    /// - Maintain a live "follow latest" view window and apply Y-axis bounds from the ViewModel.
    /// - Keep the log scrolled to the newest item.
    /// - Provide drag-to-resize behavior for the sidebar via a PanGestureRecognizer.
    ///
    /// Threading:
    /// - All UI updates happen on the UI thread. The ViewModel already marshals its events appropriately.
    ///
    /// Plotting model:
    /// - The DataStreamer is a fixed-capacity ring that efficiently appends new points and updates the view.
    /// - We feed only the newest batch from the VM (SamplesAppended).
    /// - X is implicit (1 sample per index). Use SamplePeriod to map to seconds if needed.
    ///
    /// Performance notes:
    /// - ds.AddRange(e.Samples.ToArray()) materializes the batch; if your ScottPlot version supports it,
    ///   prefer passing a Span (e.Samples.Span) to avoid allocations.
    /// - Continuous autoscale is disabled; X is managed by each DataStreamer when FollowLatest is true.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Starting width of the sidebar column at the beginning of a drag gesture.
        /// Used to calculate deltas while dragging the resize handle.
        /// </summary>
        double _startWidth;

        /// <summary>
        /// One DataStreamer per channel (key format: "BoardID/ChannelName").
        /// The streamer holds a fixed width history used for on-screen rendering.
        /// </summary>
        private readonly Dictionary<string, DataStreamer> _streams = [];

        /// <summary>
        /// Palette for assigning stable colors per channel.
        /// </summary>
        private readonly ScottPlot.Palettes.Category10 Palette = new();

        /// <summary>
        /// Cache mapping from channel key to a chosen color (stable across updates).
        /// </summary>
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];

        /// <summary>
        /// Flag to allow a one-time X autoscale after acquisition starts so initial content is visible.
        /// </summary>
        private bool _needInitialAutoScaleX = false;

        /// <summary>
        /// Width of the visible window in samples and the capacity of each DataStreamer.
        /// Increase for a wider live view; reduce for tighter focus.
        /// </summary>
        private readonly int _followWindowSamples = 10_000;

        /// <summary>
        /// Deterministically assign a palette color per channel name.
        /// </summary>
        private ScottPlot.Color GetColorForChannel(string channelKey)
        {
            if (_lineColors.TryGetValue(channelKey, out var c)) return c;

            int idx = Math.Abs(channelKey.GetHashCode()) % 10;
            var color = Palette.GetColor(idx);
            _lineColors[channelKey] = color;
            return color;
        }

        /// <summary>
        /// Initialize visual tree, create and bind the ViewModel, wire events, and configure the plot.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Create and bind the ViewModel
            var vm = new MainViewModel();
            BindingContext = vm;

            // Wire ViewModel events to UI handlers
            vm.AcquisitionStarting += VmOnAcquisitionStarted;     // prepare plot surface
            vm.SamplesAppended += VmOnSamplesAppended;            // append newest batch per channel
            vm.PropertyChanged += VmOnPropertyChanged;            // react to Y-axis bounds changes
            vm.LogMessages.CollectionChanged += VmLogMessages_CollectionChanged; // keep log scrolled

            // Initial plot labels
            MauiPlot1.Plot.Title("Live Signals");
            MauiPlot1.Plot.XLabel("Samples");
            MauiPlot1.Plot.YLabel("Value");
            MauiPlot1.Refresh();

            // Drag-to-resize setup for the sidebar column
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);
        }

        /// <summary>
        /// Keep the log list scrolled to the newest item as messages are appended.
        /// </summary>
        private void VmLogMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is { Count: > 0 })
            {
                var last = e.NewItems[^1];
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // LogView is a CollectionView/ListView defined in XAML
                    LogView?.ScrollTo(last, position: ScrollToPosition.End, animate: true);
                });
            }
        }

        /// <summary>
        /// Acquisition is starting. Clear existing plottables and (optionally) pre-create streamers for selected channels.
        /// </summary>
        private void VmOnAcquisitionStarted(object? sender, IReadOnlyList<Channel> channels)
        {
            _streams.Clear();
            MauiPlot1.Plot.Clear();

            // We will manage axes explicitly; per-channel X is managed by streamers when following
            MauiPlot1.Plot.Axes.ContinuouslyAutoscale = false;

            // Pre-create one DataStreamer per selected channel (lazy creation would also work)
            foreach (var ch in channels)
            {
                string key = $"{ch.BoardID}/{ch.Name}";
                var ds = MauiPlot1.Plot.Add.DataStreamer(_followWindowSamples);
                ds.LineWidth = 2;
                ds.Color = GetColorForChannel(key);

                // Choose a scrolling view (wipe is also available via ds.ViewWipeRight(rate))
                ds.ViewScrollLeft();

                // Use implicit X: 1 sample per index (set to 1/sampleRate for seconds)
                ds.Data.SamplePeriod = 1;
                ds.Data.OffsetX = 0;

                // Let the streamer keep X limits aligned while "follow latest" is on
                ds.ManageAxisLimits = true;

                _streams[key] = ds;
            }

            _needInitialAutoScaleX = true;
            MauiPlot1.Refresh();
        }

        /// <summary>
        /// Append newest samples for a channel into its DataStreamer and update the plot.
        /// </summary>
        private void VmOnSamplesAppended(object? sender, MainViewModel.SamplesAppendedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            // Ensure a streamer exists for this channel (create on-demand if it was not pre-created)
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

            // Feed only the newest batch into the streamer.
            // NOTE: ToArray() materializes the batch. If your ScottPlot supports Span, prefer:
            // ds.AddRange(e.Samples.Span) to avoid allocation.
            if (e.Count > 0)
                ds.AddRange(e.Samples.ToArray());

            // Apply current Y limits from the ViewModel (kept in sync with the UI)
            MauiPlot1.Plot.Axes.SetLimitsY(vm.YAxisMin, vm.YAxisMax);

            // Perform a one-time X autoscale after acquisition starts so initial lines are visible
            if (_needInitialAutoScaleX)
            {
                MauiPlot1.Plot.Axes.AutoScaleX();
                _needInitialAutoScaleX = false;
            }

            // Follow behavior: let streamer manage X when following, freeze when not
            ds.ManageAxisLimits = vm.FollowLatest;

            // Request a redraw
            MauiPlot1.Refresh();
        }

        /// <summary>
        /// React to ViewModel property changes that affect the plot.
        /// Currently supports Y-axis bounds updates.
        /// </summary>
        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            if (e.PropertyName is nameof(MainViewModel.YAxisMin) or nameof(MainViewModel.YAxisMax))
            {
                MauiPlot1.Plot.Axes.SetLimitsY(vm.YAxisMin, vm.YAxisMax);
                MauiPlot1.Refresh();
            }
        }

        /// <summary>
        /// Handle drag gestures on the sidebar resize handle to adjust the left column width.
        /// Clamps width to a sane range to maintain usability.
        /// </summary>
        private void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // Remember the starting width when the drag begins
                    _startWidth = SidebarColumn.Width.Value;
                    break;

                case GestureStatus.Running:
                    // Compute the new width based on horizontal drag delta
                    double newWidth = _startWidth - e.TotalX;

                    // Clamp to a reasonable range
                    if (newWidth < 100) newWidth = 100;
                    if (newWidth > 400) newWidth = 400;

                    SidebarColumn.Width = new GridLength(newWidth);
                    break;
            }
        }
    }
}