using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ChartRecorder Recorder { get; } = new();
    public ObservableCollection<DigitalMeter> DigitalMeters { get; } = new();
    public ISeries[] MeasurementSeries { get; set; } = [];
    public ObservableCollection<ISeries[]> ChannelSeries { get; } = new();
    public ObservableCollection<Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];
    public int WindowSize
    {
        get => Recorder.WindowSize;
        set
        {
            if (Recorder.WindowSize != value)
            {
                Recorder.WindowSize = value;
                OnPropertyChanged(nameof(WindowSize));
                OnPropertyChanged(nameof(MaxScrollIndex));
            }
        }
    }
    public int ScrollIndex
    {
        get => Recorder.ScrollIndex;
        set
        {
            if (Recorder.ScrollIndex != value)
            {
                Recorder.ScrollIndex = value;
                OnPropertyChanged(nameof(ScrollIndex));
            }
        }
    }
    public Axis[]? YAxes { get; set; }
    public int MaxScrollIndex => Recorder.MaxScrollIndex;
    public ICommand StartAcquisitionCommand { get; private set; }
    public ICommand StopAcquisitionCommand { get; private set; }
    public ICommand LockScrollingCommand { get; private set; }
    public ICommand ToggleThemeCommand { get; private set; }
    private readonly AcquisitionManager _acquisitionManager;
    private bool _isScrollingLocked = true;

    private double _yAxisMin = -10;
    private double _yAxisMax = 10;

    // Plot data managed in the VM
    private sealed class ChannelBuffer
    {
        public List<double> Y { get; } = new(capacity: 8192);
        public long StartIndex { get; private set; } = 0; // global index of Y[0]
        public long NextIndex { get; private set; } = 0;  // next global index to assign
        public int Capacity { get; }

        public ChannelBuffer(int capacity)
        {
            Capacity = capacity;
        }

        public void AddRange(IEnumerable<double> samples)
        {
            foreach (var v in samples)
            {
                Y.Add(v);
                NextIndex++;

                if (Y.Count > Capacity)
                {
                    int remove = Y.Count - Capacity;
                    Y.RemoveRange(0, remove);
                    StartIndex += remove;
                }
            }
        }

        public (double[] X, double[] Y) GetWindow(int scrollIndex, int windowSize)
        {
            long globalStart = StartIndex;
            long globalEndExclusive = StartIndex + Y.Count;

            long desiredStart = Math.Max(scrollIndex, (int)globalStart);
            long desiredEnd = Math.Min((long)scrollIndex + windowSize, globalEndExclusive);
            int count = (int)Math.Max(0, desiredEnd - desiredStart);
            if (count <= 0) return (Array.Empty<double>(), Array.Empty<double>());

            int localStart = (int)(desiredStart - globalStart);
            var ys = Y.GetRange(localStart, count).ToArray();
            var xs = new double[count];
            double x0 = desiredStart;
            for (int i = 0; i < count; i++) xs[i] = x0 + i;
            return (xs, ys);
        }
    }

    private readonly Dictionary<string, ChannelBuffer> _plot = [];
    private const int PlotBufferCapacitySamples = 200_000; // cap per channel

    // Events for the View
    public sealed class SamplesReceivedEventArgs : EventArgs
    {
        public string ChannelName { get; }
        public IReadOnlyList<double> Samples { get; }
        public SamplesReceivedEventArgs(string channelName, IEnumerable<double> samples)
        {
            ChannelName = channelName;
            Samples = samples.ToArray();
        }
    }
    public event EventHandler<IReadOnlyList<Channel>>? AcquisitionStarted;
    public event EventHandler<SamplesReceivedEventArgs>? SamplesAvailable;
    public event EventHandler<string>? ChannelDataUpdated; // channelName

    public double YAxisMax
    {
        get => _yAxisMax;
        set
        {
            if (_yAxisMax != value)
            {
                _yAxisMax = value;
                UpdateYAxes();
                OnPropertyChanged();
            }
        }
    }
    public double YAxisMin
    {
        get => _yAxisMin;
        set
        {
            if (_yAxisMin != value)
            {
                _yAxisMin = value;
                UpdateYAxes();
                OnPropertyChanged();
            }
        }
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);
        TrionApi.Uninitialize();
    }

    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = []
    };

    public MainViewModel()
    {
        LogMessages.Add("App started.");

        var numberOfBoards = TrionApi.Initialize();
        if (numberOfBoards < 0)
        {
            Debug.WriteLine($"Number of simulated Boards found: {Math.Abs(numberOfBoards)}");
        }
        else if (numberOfBoards > 0)
        {
            Debug.WriteLine($"Number of real Boards found: {numberOfBoards}");
        }
        else
        {
            Debug.WriteLine("No Trion Boards found.");
        }

        numberOfBoards = Math.Abs(numberOfBoards);

        MyEnc.Init(numberOfBoards);
        OnPropertyChanged(nameof(MyEnc));

        foreach (var board in MyEnc.Boards)
        {
            LogMessages.Add($"Board: {board.Name} (ID: {board.Id})");
            foreach (var channel in board.BoardProperties.GetChannels())
            {
                if (channel.Type != Channel.ChannelType.Analog && channel.Type != Channel.ChannelType.Digital)
                    continue;

                Channels.Add(channel);
            }
        }

        _acquisitionManager = new AcquisitionManager(MyEnc);
        OnPropertyChanged(nameof(Channels));
        StartAcquisitionCommand = new Command(async () => await StartAcquisition());
        StopAcquisitionCommand = new Command(async () => await StopAcquisition());
        LockScrollingCommand = new Command(LockScrolling);
        ToggleThemeCommand = new Command(ToggleTheme);
        UpdateYAxes();
    }

    private async Task StartAcquisition()
    {
        LogMessages.Add("Starting acquisition...");

        var selectedChannels = Channels.Where(c => c.IsSelected).ToList();
        if (!selectedChannels.Any())
        {
            LogMessages.Add("No channels selected. Please select at least one channel.");
            return;
        }

        PrepareUIForAcquisition(selectedChannels);

        // reset VM-side plot buffers
        foreach (var channel in selectedChannels)
        {
            Recorder.GetWindow($"{channel.BoardID}/{channel.Name}");
        }

        MeasurementSeries = [.. selectedChannels.Select(ch => new LineSeries<double>
        {
            Values = Recorder.GetWindow($"{ch.BoardID}/{ch.Name}"),
            Name = $"{ch.BoardID}/{ch.Name}",
            AnimationsSpeed = TimeSpan.Zero,
            GeometrySize = 0
        })];

        foreach (var ch in selectedChannels)
        {
            var series = new LineSeries<double>
            {
                Values = Recorder.GetWindow($"{ch.BoardID}/{ch.Name}"),
                Name = $"{ch.BoardID}/{ch.Name}",
                AnimationsSpeed = TimeSpan.Zero,
                GeometrySize = 0
            };
            ChannelSeries.Add([series]);
        }

        OnPropertyChanged(nameof(DigitalMeters));
        OnPropertyChanged(nameof(MeasurementSeries));
        OnPropertyChanged(nameof(ChannelSeries));

        AcquisitionStarted?.Invoke(this, selectedChannels);

        await _acquisitionManager.StartAcquisitionAsync(selectedChannels, OnSamplesReceived);
    }

    private void PrepareUIForAcquisition(List<Channel> selectedChannels)
    {
        ChannelSeries.Clear();
        DigitalMeters.Clear();

        Recorder.UpdateAllWindows();

        foreach (var channel in selectedChannels)
        {
            var meter = new DigitalMeter
            {
                Label = $"{channel.BoardID}/{channel.Name}",
                Value = channel.CurrentValue,
                Unit = channel.Unit
            };
            DigitalMeters.Add(meter);

            Recorder.GetWindow($"{channel.BoardID}/{channel.Name}");
        }
    }

    private async Task StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
        await _acquisitionManager.StopAcquisitionAsync();
        OnPropertyChanged(nameof(MeasurementSeries));
        OnPropertyChanged(nameof(ChannelSeries));
    }

    private void LockScrolling()
    {
        _isScrollingLocked = !_isScrollingLocked;
        LogMessages.Add(_isScrollingLocked ? "Scrolling locked." : "Scrolling unlocked.");

        if (_isScrollingLocked)
            ScrollIndex = MaxScrollIndex;
    }

    private void ToggleTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            LogMessages.Add($"Theme changed to {Application.Current.UserAppTheme}.");
        }
    }

    private void UpdateYAxes()
    {
        YAxes = [
            new Axis
            {
                MinLimit = YAxisMin,
                MaxLimit = YAxisMax,
                Name = "Voltage"
            }
        ];
        OnPropertyChanged(nameof(YAxes));
    }

    private void OnSamplesReceived(string channelName, IEnumerable<double> samples)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Persist for LiveCharts bindings and meters
            Recorder.AddSamples(channelName, samples);
            var latestValue = samples.Last();

            var parts = channelName.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out var boardId))
            {
                var chName = parts[1];
                var channel = Channels.FirstOrDefault(c => c.BoardID == boardId && c.Name == chName);
                if (channel is not null)
                {
                    channel.CurrentValue = latestValue;
                    var meter = DigitalMeters.FirstOrDefault(m => m.Label == channelName);
                    meter?.AddSample(latestValue);
                }
            }

            if (_isScrollingLocked)
            {
                Recorder.AutoScroll();
                OnPropertyChanged(nameof(ScrollIndex));
            }

            OnPropertyChanged(nameof(MaxScrollIndex));
            OnPropertyChanged(nameof(MeasurementSeries));
            OnPropertyChanged(nameof(ChannelSeries));

            // NEW: bounded plot buffer
            if (!_plot.TryGetValue(channelName, out var buf))
            {
                buf = new ChannelBuffer(PlotBufferCapacitySamples);
                _plot[channelName] = buf;
            }
            buf.AddRange(samples);

            // notify view for this channel
            ChannelDataUpdated?.Invoke(this, channelName);

            // optional: keep SamplesAvailable if you use it elsewhere
            SamplesAvailable?.Invoke(this, new SamplesReceivedEventArgs(channelName, samples));
        });
    }

    // Return ONLY the visible window to the plot (keeps arrays small)
    public (IReadOnlyList<double> X, IReadOnlyList<double> Y) GetChannelData(string channelName)
    {
        if (_plot.TryGetValue(channelName, out var buf))
        {
            var (x, y) = buf.GetWindow(ScrollIndex, WindowSize);
            return (x, y);
        }
        return (Array.Empty<double>(), Array.Empty<double>());
    }
}