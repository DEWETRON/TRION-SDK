using ScottPlot;
using ScottPlot.Plottables;
using System.Collections.Specialized;
using System.ComponentModel;
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
        double _startWidth;

        // One DataStreamer per channel (key: "BoardID/ChannelName")
        private readonly Dictionary<string, DataStreamer> _streams = [];

        private readonly ScottPlot.Palettes.Category10 Palette = new();
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];

        // Render throttle (30 FPS) to avoid UI thrash when many channels push data
        private readonly IDispatcherTimer? _renderTimer;
        private volatile bool _plotDirty;

        // If you want a time axis, set this to 1.0 / sampleRate at acquisition start
        private double _samplePeriod = 1;

        private bool _needInitialAutoScaleX = false;

        // Cap plotted channels to avoid excessive memory/CPU (extras still acquire but won’t be plotted)
        private const int MaxPlottedChannels = 16;

        // Visible window width (streamer capacity)
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
            vm.LogMessages.CollectionChanged += VmLogMessages_CollectionChanged; // keep log scrolled

            // Initial plot labels
            MauiPlot1.Plot.Title("Live Signals");
            MauiPlot1.Plot.XLabel("Samples");
            MauiPlot1.Plot.YLabel("Value");
            MauiPlot1.Plot.Axes.ContinuouslyAutoscale = false;
            MauiPlot1.Refresh();

            // Throttle renders to ~30 FPS
            _renderTimer = Dispatcher.CreateTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(33);
            _renderTimer.Tick += (s, e) =>
            {
                if (_plotDirty)
                {
                    MauiPlot1.Refresh();
                    _plotDirty = false;
                }
            };
            _renderTimer.Start();

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

            var vm = (MainViewModel)BindingContext;

            // Pick first plotted channel as the axis manager
            var plotted = channels.Take(MaxPlottedChannels).ToList();
            
            // Optionally set true time axis if you know the sample rate:
            // _samplePeriod = 1.0 / sampleRate;
            _samplePeriod = 1;

            foreach (var ch in plotted)
            {
                string key = $"{ch.BoardID}/{ch.Name}";
                var ds = MauiPlot1.Plot.Add.DataStreamer(_followWindowSamples);
                ds.LineWidth = 2;
                ds.Color = GetColorForChannel(key);

                // Pick a view mode: wipe or scroll. Keep one consistently.
                ds.ViewWipeRight(0.0);
                // ds.ViewScrollLeft();

                ds.Data.SamplePeriod = _samplePeriod;
                ds.Data.OffsetX = 0;

                _streams[key] = ds;
            }

            // Inform the user if more channels were selected than we plot
            if (channels.Count > MaxPlottedChannels)
                vm.LogMessages.Add($"Plotting limited to first {MaxPlottedChannels} channels out of {channels.Count} selected.");

            _needInitialAutoScaleX = true;
            _plotDirty = true; // schedule a refresh via timer
        }

        /// <summary>
        /// Append newest samples for a channel into its DataStreamer and update the plot.
        /// </summary>
        private void VmOnSamplesAppended(object? sender, MainViewModel.SamplesAppendedEventArgs e)
        {
            try
            {
                var vm = (MainViewModel)BindingContext;

                // Ignore channels beyond cap (still acquired, just not plotted)
                if (!_streams.TryGetValue(e.ChannelKey, out var ds))
                    return;

                if (e.Count > 0)
                {
                    // Prefer Span if your ScottPlot has this overload. If not, keep ToArray().
                    // ds.AddRange(e.Samples.Span);
                    ds.AddRange(e.Samples.ToArray());
                }

                if (_needInitialAutoScaleX)
                {
                    MauiPlot1.Plot.Axes.AutoScaleX();
                    _needInitialAutoScaleX = false;
                }

                // Defer redraw to the render timer
                _plotDirty = true;
            }
            catch (Exception ex)
            {
                // Never let the UI event chain crash the app; log and keep going
                if (BindingContext is MainViewModel vm)
                    vm.LogMessages.Add($"Plot error for {e.ChannelKey}: {ex.Message}");
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