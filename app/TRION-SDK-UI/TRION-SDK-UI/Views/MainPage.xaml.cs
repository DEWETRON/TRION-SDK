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

        // only view-mapping from channel -> plottable
        private readonly Dictionary<string, Scatter> _lines = [];
        IPalette Palette = new ScottPlot.Palettes.Category10();

        // stable colors per channel
        private readonly Dictionary<string, ScottPlot.Color> _lineColors = [];

        private ScottPlot.Color GetColorForChannel(string channelName)
        {
            if (_lineColors.TryGetValue(channelName, out var c))
                return c;

            // stable index based on channel name
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
            // keep _lineColors so channels persist their color across restarts
        }

        private void VmOnChannelDataUpdated(object? sender, string channelName)
        {
            if (sender is not MainViewModel vm) return;

            var (xs, ys) = vm.GetChannelData(channelName);

            if (!_lines.TryGetValue(channelName, out var line))
            {
                line = MauiPlot1.Plot.Add.Scatter([], []);
                // set style once when created
                line.Color = GetColorForChannel(channelName);
                line.LineWidth = 1;      // thinner line
                line.MarkerSize = 0;     // no markers for performance/clarity
                _lines[channelName] = line;
            }

            // Replace data (re-adding still preserves style by re-applying below)
            MauiPlot1.Plot.Remove(line);
            line = MauiPlot1.Plot.Add.Scatter(xs.ToArray(), ys.ToArray());

            // re-apply styling to avoid color/width changing on each update
            line.Color = GetColorForChannel(channelName);
            line.LineWidth = 1;
            line.MarkerSize = 0;
            _lines[channelName] = line;

            ApplyAxes(vm);
            MauiPlot1.Refresh();
        }

        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not MainViewModel vm) return;

            if (e.PropertyName is nameof(MainViewModel.YAxisMin) or nameof(MainViewModel.YAxisMax)
                or nameof(MainViewModel.WindowSize) or nameof(MainViewModel.ScrollIndex))
            {
                ApplyAxes(vm);
                MauiPlot1.Refresh();
            }
        }

        private void ApplyAxes(MainViewModel vm)
        {
            MauiPlot1.Plot.Axes.SetLimitsY(vm.YAxisMin, vm.YAxisMax);

            var xMin = Math.Max(0, vm.ScrollIndex);
            var xMax = xMin + Math.Max(1, vm.WindowSize);
            MauiPlot1.Plot.Axes.SetLimitsX(xMin, xMax);
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