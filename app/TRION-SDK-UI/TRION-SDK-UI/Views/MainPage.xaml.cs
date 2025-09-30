using System.ComponentModel;
using System.Collections.Specialized;
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

            vm.AcquisitionStarting += VmOnAcquisitionStarted;
            vm.SamplesAppended += VmOnSamplesAppended; // updated
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
                var last = e.NewItems[e.NewItems.Count - 1];
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LogView?.ScrollTo(last, position: ScrollToPosition.End, animate: true);
                });
            }
        }

        private void VmOnAcquisitionStarted(object? sender, IReadOnlyList<Channel> channels)
        {
            _lines.Clear();
            MauiPlot1.Plot.Clear();
            _needInitialAutoScaleX = true;
        }

        // UPDATED: use SamplesAppendedEventArgs and GetFullSeries()
        private void VmOnSamplesAppended(object? sender, MainViewModel.SamplesAppendedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            string channelKey = e.ChannelKey;
            var (xs, ys) = vm.GetFullSeries(channelKey);

            if (!_lines.TryGetValue(channelKey, out var line))
            {
                line = MauiPlot1.Plot.Add.Scatter([], []);
                line.Color = GetColorForChannel(channelKey);
                line.LineWidth = 2;
                line.MarkerSize = 0;
                _lines[channelKey] = line;
            }

            MauiPlot1.Plot.Remove(line);
            line = MauiPlot1.Plot.Add.Scatter(xs.ToArray(), ys.ToArray());
            line.Color = GetColorForChannel(channelKey);
            line.LineWidth = 2;
            line.MarkerSize = 0;
            _lines[channelKey] = line;

            MauiPlot1.Plot.Axes.SetLimitsY(vm.YAxisMin, vm.YAxisMax);

            if (_needInitialAutoScaleX)
            {
                MauiPlot1.Plot.Axes.AutoScaleX();
                _needInitialAutoScaleX = false;
            }

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