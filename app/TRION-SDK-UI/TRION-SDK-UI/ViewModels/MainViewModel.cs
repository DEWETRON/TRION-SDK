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
        StartAcquisitionCommand = new Command(StartAcquisition);
        StopAcquisitionCommand = new Command(StopAcquisition);
        LockScrollingCommand = new Command(LockScrolling);
        UpdateYAxes();

    }

    private readonly AcquisitionManager _acquisitionManager;
    private bool _isScrollingLocked = true;
    private double _yAxisMin = -10;
    private void StartAcquisition()
    {
        LogMessages.Add("Starting acquisition...");

        var selectedChannels = Channels.Where(c => c.IsSelected).ToList();

        _acquisitionManager.StartAcquisition(selectedChannels, OnSamplesReceived);

        MeasurementSeries = [.. selectedChannels.Select(ch => new LineSeries<double>
        {
            Values = Recorder.GetWindow(ch.Name),
            Name = ch.Name,
            AnimationsSpeed = TimeSpan.Zero,
            GeometrySize = 0
        })];
        OnPropertyChanged(nameof(MeasurementSeries));

        ChannelSeries.Clear();
        foreach (var ch in Channels.Where(c => c.IsSelected))
        {
            var window = Recorder.GetWindow(ch.Name);
            Debug.WriteLine($"TEST: Channel: {ch.Name}, Window HashCode: {window.GetHashCode()}, Count: {window.Count}");

            var series = new LineSeries<double>
            {
                Values = Recorder.GetWindow(ch.Name),
                Name = ch.Name,
                AnimationsSpeed = TimeSpan.Zero,
                GeometrySize = 0
            };
            ChannelSeries.Add([series]);
        }
        OnPropertyChanged(nameof(ChannelSeries));
    }
    private void StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
        _acquisitionManager.StopAcquisition();
        MeasurementSeries = [];
        ChannelSeries.Clear();
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
            //Debug.WriteLine($"got sample {channelName} {samples.Count()}");
            Recorder.AddSamples(channelName, samples);
            if (_isScrollingLocked)
            {
                Recorder.AutoScroll();
                OnPropertyChanged(nameof(ScrollIndex));
            }
            OnPropertyChanged(nameof(MaxScrollIndex));
            OnPropertyChanged(nameof(MeasurementSeries));
            OnPropertyChanged(nameof(ChannelSeries));
        });
    }
}