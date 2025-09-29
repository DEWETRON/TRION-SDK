using System.Collections.ObjectModel;
using System.ComponentModel;
using ScottPlot;
using ScottPlot.Plottables;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI
{
    public partial class MainPage : ContentPage
    {
        double _startWidth;

        private readonly Dictionary<string, Scatter> _lines = [];
        IPalette Palette = new ScottPlot.Palettes.Category10();
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];
        private bool _needInitialAutoScaleX = false;

        // NEW: how many samples to keep visible while following
        private int _followWindowSamples = 600;

        private ScottPlot.Color GetColorForChannel(string channelName)
        {
            if (_lineColors.TryGetValue(channelName, out var c))
                return c;
            int idx = Math.Abs(channelName.GetHashCode()) % 10;
            var color = Palette.GetColor(idx);
            _lineColors[channelName] = color;
            return color;
        }

        public MainPage()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            BindingContext = vm;

            vm.AcquisitionStarted += VmOnAcquisitionStarted;
            vm.ChannelDataUpdated += VmOnChannelDataUpdated;
            vm.PropertyChanged += VmOnPropertyChanged;

            MauiPlot1.Plot.Title("Live Signals");
            MauiPlot1.Plot.XLabel("Samples");
            MauiPlot1.Plot.YLabel("Value");
            MauiPlot1.Refresh();

            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDragHandlePanUpdated;
            DragHandle.GestureRecognizers.Add(panGesture);
        }

        private void VmOnAcquisitionStarted(object? sender, IReadOnlyList<Channel> channels)
        {
            _lines.Clear();
            MauiPlot1.Plot.Clear();
            _needInitialAutoScaleX = true;
        }

        private void VmOnChannelDataUpdated(object? sender, string channelName)
        {
            if (sender is not MainViewModel vm) return;

            var (xs, ys) = vm.GetChannelAllData(channelName);

            if (!_lines.TryGetValue(channelName, out var line))
            {
                line = MauiPlot1.Plot.Add.Scatter([], []);
                line.Color = GetColorForChannel(channelName);
                line.LineWidth = 1;
                line.MarkerSize = 0;
                _lines[channelName] = line;
            }

            MauiPlot1.Plot.Remove(line);
            line = MauiPlot1.Plot.Add.Scatter(xs.ToArray(), ys.ToArray());
            line.Color = GetColorForChannel(channelName);
            line.LineWidth = 1;
            line.MarkerSize = 0;
            _lines[channelName] = line;

            // Always respect Y limits from VM
            MauiPlot1.Plot.Axes.SetLimitsY(vm.YAxisMin, vm.YAxisMax);

            // Initial autoscale X once
            if (_needInitialAutoScaleX)
            {
                MauiPlot1.Plot.Axes.AutoScaleX();
                _needInitialAutoScaleX = false;
            }

            // Follow latest if enabled: show a trailing window ending at the newest sample
            if (vm.FollowLatest && xs.Count > 0)
            {
                double right = xs[^1];
                double left = Math.Max(0, right - _followWindowSamples);
                MauiPlot1.Plot.Axes.SetLimitsX(left, right);
            }

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

            // Optional: when FollowLatest is toggled on, snap to newest immediately using current data
            if (e.PropertyName is nameof(MainViewModel.FollowLatest) && vm.FollowLatest)
            {
                // pick any plotted series that has data
                var firstWithData = _lines.Values.FirstOrDefault();
                if (firstWithData is not null && firstWithData.Data is not null && firstWithData.Data.GetScatterPoints().Count > 0)
                {
                    // last X value (series is drawn from arrays we provided)
                    double right = firstWithData.GetAxisLimits().Right; // fallback
                    // Prefer using our last xs if desired (requires storing last xs)
                    // Here we just keep the next update to reposition.
                }
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