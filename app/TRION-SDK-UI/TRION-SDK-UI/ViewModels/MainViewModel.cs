using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
            LogMessages.Add($"Number of simulated Boards found: {Math.Abs(numberOfBoards)}");
        }
        else if (numberOfBoards > 0)
        {
            LogMessages.Add($"Number of real Boards found: {numberOfBoards}");
        }
        else
        {
            LogMessages.Add("No Trion Boards found.");
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

        OnPropertyChanged(nameof(Channels));
        StartAcquisitionCommand = new Command(StartAcquisition);
        StopAcquisitionCommand = new Command(StopAcquisition);
        LockScrollingCommand = new Command(LockScrolling);
        UpdateYAxes();
    }

    private readonly CancellationTokenSource? _cts;
    private readonly Task? _acquisitionTask;
    private List<Task> _acquisitionTasks = [];
    private readonly List<CancellationTokenSource> _ctsList = [];
    private bool _isScrollingLocked = true;
    private double _yAxisMin = -10;
    private void StartAcquisition()
    {
        LogMessages.Add("Starting acquisition...");

        StopAcquisition();

        _acquisitionTasks.Clear();
        _ctsList.Clear();

        var selectedChannels = Channels.Where(c => c.IsSelected).ToList();
        var selectedBoardIds = selectedChannels.Select(c => c.BoardID).Distinct();
        var selectedBoards = MyEnc.Boards.Where(b => selectedBoardIds.Contains(b.Id)).ToList();

        foreach (var ch in selectedChannels)
        {
            Debug.WriteLine($"TEST selected channel: {ch.Name}");
        }
        foreach (var bid in selectedBoardIds)
        {
            Debug.WriteLine($"TEST selected board ids: {bid}");
        }
        foreach (var selb in selectedBoards)
        {
            Debug.WriteLine($"TEST selected boads: {selb.Name}");
        }

        // Reset boards
        foreach (var selected_board in selectedBoards)
        {
            selected_board.ResetBoard();
        }
        // set board properties
        // set buffer properties
        foreach (var selected_board in selectedBoards)
        {
            selected_board.SetAcquisitionProperties();
        }
        // disable all channels
        foreach (var selected_board in selectedBoards)
        {
            TrionApi.DeWeSetParamStruct($"BoardID{selected_board.Id}/AIAll", "Used", "False");
        }
        // enable selected channels
        foreach (var selected_channel in selectedChannels)
        {
            TrionApi.DeWeSetParamStruct($"BoardID{selected_channel.BoardID}/{selected_channel.Name}", "Used", "True");
            TrionApi.DeWeSetParamStruct($"BoardID{selected_channel.BoardID}/{selected_channel.Name}", "Range", "10 V");
        }
        // update parameters
        foreach (var selected_board in selectedBoards)
        {
            selected_board.UpdateBoard();
        }




        MeasurementSeries = selectedChannels.Select(ch => new LineSeries<double>
        {
            Values = Recorder.GetWindow(ch.Name),
            Name = ch.Name,
            AnimationsSpeed = TimeSpan.Zero,
            GeometrySize = 0
        }).ToArray();
        OnPropertyChanged(nameof(MeasurementSeries));

        foreach (var board in MyEnc.Boards)
        {
            board.ScanDescriptorXml = TrionApi.DeWeGetParamStruct_String($"BoardID{board.Id}", "ScanDescriptor_V3").value;
            board.ScanDescriptorDecoder = new ScanDescriptorDecoder(board.ScanDescriptorXml);
            board.ScanSizeBytes = board.ScanDescriptorDecoder.ScanSizeBytes;
            Debug.WriteLine($"TEST XML: {board.ScanDescriptorXml}");
        }

        foreach (var channel in selectedChannels)
        {
            Debug.WriteLine($"Channel selected: {channel.Name}");
            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);
            var task = Task.Run(() => AcquireDataLoop(channel, cts.Token), cts.Token);
            _acquisitionTasks.Add(task);
        }
        ChannelSeries.Clear();
        foreach (var ch in Channels.Where(c => c.IsSelected))
        {
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
        foreach (var cts in _ctsList)
        {
            cts.Cancel();
        }
        Task.WaitAll(_acquisitionTasks.ToArray(), 1000);
        _acquisitionTasks.Clear();
        _ctsList.Clear();
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
    private void AcquireDataLoop(Channel selectedChannel, CancellationToken token)
    {
        var board_id = selectedChannel.BoardID;
        var channel_name = selectedChannel.Name;

        var (adcDelayError, adc_delay) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BOARD_ADC_DELAY);
        TrionApi.DeWeSetParam_i32(board_id, TrionCommand.START_ACQUISITION, 0);
        CircularBuffer buffer = new(board_id);

        while (!token.IsCancellationRequested)
        {
            var (available_samples_error, available_samples) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                Thread.Sleep(10);
                continue;
            }
            var (read_pos_error, read_pos) = TrionApi.DeWeGetParam_i64(board_id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            read_pos += adc_delay * sizeof(uint);
            List<double> tempValues = [.. new double[available_samples]];
            for (int i = 0; i < available_samples; ++i)
            {
                if (read_pos >= buffer.EndPosition)
                {
                    read_pos -= buffer.Size;
                }

                float value = Marshal.ReadInt32((IntPtr)read_pos);
                value = (float)((float)value / 0x7FFFFF00 * 10.0);
                tempValues[i] = value;
                //Debug.WriteLine($"TEST: Value {value}");

                read_pos += sizeof(uint);
            }
            TrionApi.DeWeSetParam_i32(board_id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Recorder.AddSamples(selectedChannel.Name, tempValues);

                if (_isScrollingLocked)
                {
                    Recorder.AutoScroll();
                    OnPropertyChanged(nameof(ScrollIndex));
                }
                OnPropertyChanged(nameof(MaxScrollIndex));
            });
        }
        TrionApi.DeWeSetParam_i32(board_id, TrionCommand.STOP_ACQUISITION, 0);
    }
}