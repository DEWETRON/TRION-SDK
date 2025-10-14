using ScottPlot.Plottables;
using System.Collections.Specialized;
using System.Diagnostics;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI
{
    /// <summary>
    /// Main page hosting the live plot and the side panel.
    ///
    /// Responsibilities:
    /// - Own a MainViewModel instance and wire its events to UI updates.
    /// - Plot per-channel data using one ScottPlot DataStreamer per channel.
    /// - Keep the UI log scrolled to the newest item.
    /// - Provide drag-to-resize behavior for the sidebar via a PanGestureRecognizer.
    ///
    /// Plotting model:
    /// - DataStreamer is a fixed-capacity ring optimized for live streaming.
    /// - We append only the newest sample batch from the VM (SamplesAppended).
    /// - X uses implicit indexing (SamplePeriod=1 => X is "sample index").
    ///
    /// Notes:
    /// - This page intentionally avoids Y-axis management (user pans/zooms freely).
    /// - ManageAxisLimits is toggled per event by the VM's FollowLatest flag.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Sidebar column width captured at the start of a drag, used to apply deltas during the gesture.
        /// </summary>
        double _startWidth;

        /// <summary>
        /// One live DataStreamer per channel. Key format: "BoardID/ChannelName" (e.g., "1/AI0").
        /// </summary>
        private readonly Dictionary<string, DataStreamer> _streams = [];

        /// <summary>
        /// Palette used to derive stable, distinct colors for each channel.
        /// </summary>
        private readonly ScottPlot.Palettes.Category10 Palette = new();

        /// <summary>
        /// Cache for assigned colors per channel key to keep colors stable across updates.
        /// </summary>
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];

        /// <summary>
        /// When true, perform a one-time X autoscale after acquisition starts so traces are visible.
        /// </summary>
        private bool _needInitialAutoScaleX = false;

        /// <summary>
        /// Visible window width (samples) and capacity for each DataStreamer.
        /// Increase for a wider recent history; decrease for a tighter live window.
        /// </summary>
        private readonly int _followWindowSamples = 1_000;

        /// <summary>
        /// Returns a stable color for the provided channel key using a categorical palette.
        /// </summary>
        private ScottPlot.Color GetColorForChannel(string channelKey)
        {
            if (_lineColors.TryGetValue(channelKey, out var c)) return c;

            // Hash the key to pick a palette index deterministically
            int idx = Math.Abs(channelKey.GetHashCode()) % 10;
            var color = Palette.GetColor(idx);
            _lineColors[channelKey] = color;
            return color;
        }

        /// <summary>
        /// Construct the UI, create/bind the ViewModel, wire events, and set initial plot labels.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Instantiate and bind the ViewModel for this page
            var vm = new MainViewModel();
            BindingContext = vm;

            // Wire ViewModel events:
            // - AcquisitionStarting: reset plot and create per-channel plottables
            // - SamplesAppended: append newest batch into the corresponding streamer
            vm.AcquisitionStarting += VmOnAcquisitionStarted;

            // keep old per-channel subscription if you want backward compatibility
            // vm.SamplesAppended += VmOnSamplesAppended;

            // NEW: subscribe to the batched event
            vm.SamplesBatchAppended += VmOnSamplesBatchAppended;

            vm.LogMessages.CollectionChanged += VmLogMessages_CollectionChanged;

            // Initial plot cosmetics
            MauiPlot1.Plot.Title("Live Signals");
            MauiPlot1.Plot.XLabel("Samples");
            MauiPlot1.Plot.YLabel("Value");
            MauiPlot1.Refresh();

            // Enable drag-to-resize for the left sidebar column
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);
        }

        // NEW: batch handler (one UI refresh per tick)
        private void VmOnSamplesBatchAppended(object? sender, MainViewModel.SamplesBatchAppendedEventArgs e)
        {
            if (sender is not MainViewModel vm)
            {
                return;
            }

            foreach (var (channelKey, samples) in e.Batches)
            {
                if (!_streams.TryGetValue(channelKey, out var ds))
                {
                    // create streamer lazily if needed
                    ds = MauiPlot1.Plot.Add.DataStreamer(_followWindowSamples);
                    ds.LineWidth = 2;
                    ds.Color = GetColorForChannel(channelKey);
                    ds.ViewScrollLeft();
                    ds.Data.SamplePeriod = 1;
                    ds.Data.OffsetX = 0;
                    ds.ManageAxisLimits = true;
                    _streams[channelKey] = ds;
                }

                ds.ManageAxisLimits = vm.FollowLatest;
                ds.Add(samples);
            }

            // optional: do a one-time autoscale when data first arrives
            if (_needInitialAutoScaleX)
            {
                MauiPlot1.Plot.Axes.AutoScale();
                _needInitialAutoScaleX = false;
            }

            MauiPlot1.Refresh();
        }

        /// <summary>
        /// When new log entries are added, scroll the CollectionView/ListView to the bottom.
        /// </summary>
        private void VmLogMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is { Count: > 0 })
            {
                var last = e.NewItems[^1];
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // LogView is defined in XAML as a scrollable list control
                    LogView?.ScrollTo(last, position: ScrollToPosition.End, animate: true);
                });
            }
        }

        /// <summary>
        /// Acquisition about to start:
        /// - Clear old plottables and reset state.
        /// - Create one DataStreamer per selected channel and configure its visuals/behavior.
        /// </summary>
        private void VmOnAcquisitionStarted(object? sender, IReadOnlyList<Channel> channels)
        {
            _streams.Clear();
            MauiPlot1.Plot.Clear();

            // We explicitly manage axes; per-channel streamer can steer X while following.
            MauiPlot1.Plot.Axes.ContinuouslyAutoscale = false;

            // Create a streamer for each selected channel
            foreach (var ch in channels)
            {
                string key = $"{ch.BoardID}/{ch.Name}";
                var ds = MauiPlot1.Plot.Add.DataStreamer(_followWindowSamples);
                ds.LineWidth = 2;
                ds.Color = GetColorForChannel(key);

                // Choose a view mode:
                // - ViewWipeRight(rate): oscilloscope-like wipe; rate 0.0 means rely on capacity/window
                // - ViewScrollLeft(): classic scrolling window leftwards
                ds.ViewScrollLeft();

                // Implicit X: 1 unit per sample => X is sample index (set to 1/sampleRate for seconds)
                ds.Data.SamplePeriod = 1;
                ds.Data.OffsetX = 0;

                // Let the streamer keep X-limits aligned while "follow latest" is on (toggled per update)
                ds.ManageAxisLimits = true;

                _streams[key] = ds;
            }

            // Auto-scale X once after data begins to flow so initial traces are visible
            _needInitialAutoScaleX = true;
            MauiPlot1.Refresh();
        }

        /// <summary>
        /// New samples arrived for a specific channel:
        /// - Ensure a streamer exists for it (create lazily if needed),
        /// - Append the newest batch,
        /// </summary>
        private void VmOnSamplesAppended(object? sender, MainViewModel.SamplesAppendedEventArgs e)
        {
            //Debug.WriteLine($"SamplesAppended: {e.ChannelKey} +{e.Count}");
            if (sender is not MainViewModel vm) return;

            // Make sure a streamer exists for this channel (handles late channels)
            if (!_streams.TryGetValue(e.ChannelKey, out var ds))
            {
                //Debug.WriteLine($"Creating lazy DataStreamer for {e.ChannelKey}");
                ds = MauiPlot1.Plot.Add.DataStreamer(_followWindowSamples);
                ds.LineWidth = 2;
                ds.Color = GetColorForChannel(e.ChannelKey);
                ds.ViewScrollLeft();      // scroll view for lazily created channels
                ds.Data.SamplePeriod = 1; // keep X in sample indexes
                ds.Data.OffsetX = 0;
                ds.ManageAxisLimits = true;
                _streams[e.ChannelKey] = ds;
            }

            ds.ManageAxisLimits = vm.FollowLatest;

            // Adding samples to the Data Streamer
            ds.Add(e.Samples);

            // Redraw the plot
            MauiPlot1.Refresh();
        }

        /// <summary>
        /// Handle drag gestures on the sidebar handle to resize the left column.
        /// Applies clamping to keep the sidebar within a usable width range.
        /// </summary>
        private void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // Capture the starting width when the drag begins
                    _startWidth = SidebarColumn.Width.Value;
                    break;

                case GestureStatus.Running:
                    // Compute and clamp the new width based on horizontal drag delta
                    double newWidth = _startWidth - e.TotalX;
                    if (newWidth < 100) newWidth = 100;
                    if (newWidth > 400) newWidth = 400;
                    SidebarColumn.Width = new GridLength(newWidth);
                    break;
            }
        }
    }
}