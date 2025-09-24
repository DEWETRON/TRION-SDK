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
    public ICommand ChannelSelectedCommand { get; private set; }
    public ICommand StartAcquisitionCommand { get; private set; }
    public ICommand StopAcquisitionCommand { get; private set; }
    public ICommand LockScrollingCommand { get; private set; }
    public ICommand ToggleThemeCommand { get; private set; }
    private readonly AcquisitionManager _acquisitionManager;
    private bool _isScrollingLocked = true;
    private double _yAxisMin = -10;
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

        // Call your uninitialize function here
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
        ChannelSeries.Clear();
        DigitalMeters.Clear(); // Clear existing meters

        Recorder.UpdateAllWindows();
        OnPropertyChanged(nameof(MeasurementSeries));
        OnPropertyChanged(nameof(ChannelSeries));

        var selectedChannels = Channels.Where(c => c.IsSelected).ToList();

        // Create a digital meter for each selected channel
        foreach (var channel in selectedChannels)
        {
            var meter = new DigitalMeter();
            meter.Label = $"{channel.BoardID}/{channel.Name}";
            meter.Value = channel.CurrentValue;
            meter.Unit = channel.Unit; // Set appropriate unit based on channel mode
            DigitalMeters.Add(meter);
        }
        OnPropertyChanged(nameof(DigitalMeters));

        await _acquisitionManager.StartAcquisitionAsync(selectedChannels, OnSamplesReceived);

        MeasurementSeries = [.. selectedChannels.Select(ch => new LineSeries<double>
        {
            Values = Recorder.GetWindow($"{ch.BoardID}/{ch.Name}"),
            Name = $"{ch.BoardID}/{ch.Name}",
            AnimationsSpeed = TimeSpan.Zero,
            GeometrySize = 0
        })];
        OnPropertyChanged(nameof(MeasurementSeries));

        foreach (var ch in Channels.Where(c => c.IsSelected))
        {
            var window = Recorder.GetWindow($"{ch.BoardID}/{ch.Name}");

            var series = new LineSeries<double>
            {
                Values = Recorder.GetWindow($"{ch.BoardID}/{ch.Name}"),
                Name = $"{ch.BoardID}/{ch.Name}",
                AnimationsSpeed = TimeSpan.Zero,
                GeometrySize = 0
            };
            ChannelSeries.Add([series]);
        }
        OnPropertyChanged(nameof(ChannelSeries));
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
        {
            ScrollIndex = MaxScrollIndex;
        }
    }
    private void ToggleTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            LogMessages.Add($"Theme changed to {Application.Current.UserAppTheme}.");
        }
    }
    private double _yAxisMax = 10;
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
            Recorder.AddSamples(channelName, samples);
            var latestValue = samples.Last();
            Debug.WriteLine($"Latest value for {channelName}: {latestValue}");

            // Find which channel this belongs to
            var channelParts = channelName.Split('/');
            if (channelParts.Length == 2)
            {
                var boardId = int.Parse(channelParts[0]);
                var chName = channelParts[1];
                
                // Find the channel and update its current value
                var channel = Channels.FirstOrDefault(c => c.BoardID == boardId && c.Name == chName);
                if (channel != null)
                {
                    channel.CurrentValue = latestValue;
                    
                    // Find the index of this channel in the selected channels list
                    var selectedChannels = Channels.Where(c => c.IsSelected).ToList();
                    var index = selectedChannels.IndexOf(channel);
                    
                    // If we have a digital meter for this channel, update it
                    if (index >= 0 && index < DigitalMeters.Count)
                    {
                        DigitalMeters[index].AddSample(latestValue);
                    }
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
            OnPropertyChanged(nameof(DigitalMeters));
        });
    }
}