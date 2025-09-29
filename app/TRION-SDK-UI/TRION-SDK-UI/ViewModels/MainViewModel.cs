using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<DigitalMeter> DigitalMeters { get; } = new();
    public ObservableCollection<Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    public ICommand StartAcquisitionCommand { get; private set; }
    public ICommand StopAcquisitionCommand { get; private set; }
    public ICommand LockScrollingCommand { get; private set; }
    public ICommand ToggleThemeCommand { get; private set; }

    private readonly AcquisitionManager _acquisitionManager;
    private bool _isScrollingLocked = true; // kept in case you later want a "follow latest" toggle in the View

    // NEW: expose follow flag to the View
    private bool _followLatest = true;
    public bool FollowLatest
    {
        get => _followLatest;
        private set
        {
            if (_followLatest != value)
            {
                _followLatest = value;
                OnPropertyChanged();
            }
        }
    }

    private double _yAxisMin = -10;
    private double _yAxisMax = 10;

    // events for the View
    public event EventHandler<IReadOnlyList<Channel>>? AcquisitionStarted;
    public event EventHandler<string>? ChannelDataUpdated;

    public double YAxisMax
    {
        get => _yAxisMax;
        set { if (_yAxisMax != value) { _yAxisMax = value; OnPropertyChanged(); } }
    }
    public double YAxisMin
    {
        get => _yAxisMin;
        set { if (_yAxisMin != value) { _yAxisMin = value; OnPropertyChanged(); } }
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

    // plotting buffer (per channel), unlimited history when capacity == 0
    private sealed class ChannelBuffer
    {
        public List<double> Y { get; } = new(capacity: 8192);
        public long StartIndex { get; private set; } = 0;
        public long NextIndex { get; private set; } = 0;
        public int Capacity { get; }

        public ChannelBuffer(int capacity) => Capacity = capacity;

        public void AddRange(IEnumerable<double> samples)
        {
            foreach (var v in samples)
            {
                Y.Add(v);
                NextIndex++;
                if (Capacity > 0 && Y.Count > Capacity)
                {
                    int remove = Y.Count - Capacity;
                    Y.RemoveRange(0, remove);
                    StartIndex += remove;
                }
            }
        }

        public (double[] X, double[] Y) GetAll()
        {
            int count = Y.Count;
            if (count == 0) return (Array.Empty<double>(), Array.Empty<double>());
            var ys = Y.ToArray();
            var xs = new double[count];
            double x = StartIndex;
            for (int i = 0; i < count; i++, x++) xs[i] = x;
            return (xs, ys);
        }
    }

    private readonly Dictionary<string, ChannelBuffer> _plot = [];
    private const int PlotBufferCapacitySamples = 0; // 0 = unlimited history; set >0 to cap memory

    public MainViewModel()
    {
        LogMessages.Add("App started.");

        var numberOfBoards = TrionApi.Initialize();
        if (numberOfBoards < 0) Debug.WriteLine($"Number of simulated Boards found: {Math.Abs(numberOfBoards)}");
        else if (numberOfBoards > 0) Debug.WriteLine($"Number of real Boards found: {numberOfBoards}");
        else Debug.WriteLine("No Trion Boards found.");

        numberOfBoards = Math.Abs(numberOfBoards);

        MyEnc.Init(numberOfBoards);
        OnPropertyChanged(nameof(MyEnc));

        foreach (var board in MyEnc.Boards)
        {
            LogMessages.Add($"Board: {board.Name} (ID: {board.Id})");
            foreach (var channel in board.BoardProperties.GetChannels())
            {
                if (channel.Type is Channel.ChannelType.Analog or Channel.ChannelType.Digital)
                    Channels.Add(channel);
            }
        }

        _acquisitionManager = new AcquisitionManager(MyEnc);
        OnPropertyChanged(nameof(Channels));

        StartAcquisitionCommand = new Command(async () => await StartAcquisition());
        StopAcquisitionCommand = new Command(async () => await StopAcquisition());
        LockScrollingCommand = new Command(LockScrolling);
        ToggleThemeCommand = new Command(ToggleTheme);
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

        // notify view early so it can clear plottables
        AcquisitionStarted?.Invoke(this, selectedChannels);

        await _acquisitionManager.StartAcquisitionAsync(selectedChannels, OnSamplesReceived);
    }

    private void PrepareUIForAcquisition(List<Channel> selectedChannels)
    {
        DigitalMeters.Clear();

        foreach (var channel in selectedChannels)
        {
            var meter = new DigitalMeter
            {
                Label = $"{channel.BoardID}/{channel.Name}",
                Value = channel.CurrentValue,
                Unit = channel.Unit
            };
            DigitalMeters.Add(meter);
        }

        // clear plot buffers for selected channels
        foreach (var ch in selectedChannels)
        {
            var key = $"{ch.BoardID}/{ch.Name}";
            _plot[key] = new ChannelBuffer(PlotBufferCapacitySamples);
        }

        OnPropertyChanged(nameof(DigitalMeters));
    }

    private async Task StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
        await _acquisitionManager.StopAcquisitionAsync();
    }

    private void LockScrolling()
    {
        _isScrollingLocked = !_isScrollingLocked;
        FollowLatest = _isScrollingLocked;
        LogMessages.Add(_isScrollingLocked ? "Scrolling locked." : "Scrolling unlocked.");
    }

    private void ToggleTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            LogMessages.Add($"Theme changed to {Application.Current.UserAppTheme}.");
        }
    }

    private void OnSamplesReceived(string channelName, IEnumerable<double> samples)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var latestValue = samples.LastOrDefault();

            // update meter value
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

            // plot buffer (full history)
            if (!_plot.TryGetValue(channelName, out var buf))
            {
                buf = new ChannelBuffer(PlotBufferCapacitySamples);
                _plot[channelName] = buf;
            }
            buf.AddRange(samples);

            ChannelDataUpdated?.Invoke(this, channelName);
        });
    }

    // ScottPlot: full history accessor
    public (IReadOnlyList<double> X, IReadOnlyList<double> Y) GetChannelAllData(string channelName)
    {
        if (_plot.TryGetValue(channelName, out var buf))
        {
            var (x, y) = buf.GetAll();
            return (x, y);
        }
        return (Array.Empty<double>(), Array.Empty<double>());
    }
}