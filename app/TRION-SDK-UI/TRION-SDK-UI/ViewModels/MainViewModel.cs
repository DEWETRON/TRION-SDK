using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

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

        OnPropertyChanged(nameof(Channels));
        StartAcquisitionCommand = new Command(StartAcquisition);
        StopAcquisitionCommand = new Command(StopAcquisition);
        LockScrollingCommand = new Command(LockScrolling);
        UpdateYAxes();

    }

    private readonly AcquisitionManager _acquisitionManager;
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
        TrionError error;

        // Reset boards
        foreach (var selected_board in selectedBoards)
        {
            selected_board.ResetBoard();
            selected_board.SetAcquisitionProperties();
            error = TrionApi.DeWeSetParamStruct($"BoardID{selected_board.Id}/AIAll", "Used", "False");
            Utils.CheckErrorCode(error, "Failed to reset board");
            Debug.WriteLine($"TEST: Board: {selected_board.Name} Reset");
        }
        // enable selected channels
        foreach (var selected_channel in selectedChannels)
        {
            error = TrionApi.DeWeSetParamStruct($"BoardID{selected_channel.BoardID}/{selected_channel.Name}", "Used", "True");
            Utils.CheckErrorCode(error, $"Failed to set channel used {selected_channel.Name}");
            error = TrionApi.DeWeSetParamStruct($"BoardID{selected_channel.BoardID}/{selected_channel.Name}", "Range", "10 V");
            Utils.CheckErrorCode(error, $"Failed to set channel range {selected_channel.Name}");
            Debug.WriteLine($"TEST: Channel: {selected_channel.Name} enabled");
        }
        // update parameters
        foreach (var selected_board in selectedBoards)
        {
            selected_board.UpdateBoard();
            Debug.WriteLine($"TEST: Board: {selected_board.Name} Updated");
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
            (error, board.ScanDescriptorXml) = TrionApi.DeWeGetParamStruct_String($"BoardID{board.Id}", "ScanDescriptor_V3");
            Utils.CheckErrorCode(error, $"Failed to get scan descriptor {board.Id}");
            board.ScanDescriptorDecoder = new ScanDescriptorDecoder(board.ScanDescriptorXml);
            board.ScanSizeBytes = board.ScanDescriptorDecoder.ScanSizeBytes;
            //Debug.WriteLine($"TEST XML: {board.ScanDescriptorXml}");
        }

        foreach (var channel in selectedChannels)
        {
            //Debug.WriteLine($"Channel selected: {channel.Name}");
            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);
            var task = Task.Run(() => AcquireDataLoop(channel, cts.Token), cts.Token);
            _acquisitionTasks.Add(task);
        }
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
        var board = MyEnc.Boards.First(b => b.Id == selectedChannel.BoardID);
        var scanSize = (int)board.ScanSizeBytes;
        var scanDescriptor = board.ScanDescriptorDecoder;
        var channelInfo = scanDescriptor.Channels.FirstOrDefault(c => c.Name == selectedChannel.Name);
        var polling_interval = (int)(board.BufferBlockSize / (double)board.SamplingRate * 1000);

        TrionError error;
        if (channelInfo == null) return;

        var board_id = selectedChannel.BoardID;
        (error, var adc_delay) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BOARD_ADC_DELAY);
        Utils.CheckErrorCode(error, $"Failed to get ADC Delay {board_id}");

        error = TrionApi.DeWeSetParam_i32(board_id, TrionCommand.START_ACQUISITION, 0);
        Utils.CheckErrorCode(error, $"Failed start acquisition {board_id}");

        CircularBuffer buffer = new(board_id);

        Debug.WriteLine($"AcquireDataLoop started for channel: {selectedChannel.Name}");
        Debug.WriteLine($"Sample Size {(int)channelInfo.SampleSize}, Sample Offset {(int)channelInfo.SampleOffset / 8}");

        while (!token.IsCancellationRequested)
        {
            (error, var available_samples) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
            Utils.CheckErrorCode(error, $"Failed to get available samples {board_id}, {available_samples}");
            if (available_samples <= 0)
            {
                Thread.Sleep(polling_interval);
            }

            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                Thread.Sleep(10);
                continue;
            }
            (error, var read_pos) = TrionApi.DeWeGetParam_i64(board_id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            Utils.CheckErrorCode(error, $"Failed to get actual sample position {board_id}");

            read_pos += adc_delay * scanSize;

            List<double> tempValues = new(available_samples);
            // loop over available samples
            for (int i = 0; i < available_samples; ++i)
            {
                // calculate the position of the sample in memory
                var offset_bytes = (int)channelInfo.SampleOffset / 8;
                var samplePos = read_pos + offset_bytes;

                // read the raw data
                int raw = Marshal.ReadInt32((IntPtr)samplePos);

                // extract the actual sample bits
                int sampleSize = (int)channelInfo.SampleSize;
                int bitmask = (1 << sampleSize) - 1; // = 0xFFFFFF
                raw &= bitmask; // keeps only the lower 24 bits

                // general sign extension for N-bit signed value
                int signBit = 1 << (sampleSize - 1);
                if ((raw & signBit) != 0)
                    raw |= ~bitmask;

                // scale to engineering units (i guess the range needs to be adjustable)
                double value = (double)raw / (double)(signBit - 1) * 10.0;

                // store the result
                tempValues.Add(value);

                // move to the next sample in the buffer
                read_pos += scanSize;
                if (read_pos >= buffer.EndPosition)
                {
                    read_pos -= buffer.Size;
                }
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
        error = TrionApi.DeWeSetParam_i32(board_id, TrionCommand.STOP_ACQUISITION, 0);
        Utils.CheckErrorCode(error, $"Failed stop acquisition {board_id}");
        error = TrionApi.DeWeSetParam_i32(board_id, TrionCommand.CLOSE_BOARD, 0);
        Utils.CheckErrorCode(error, $"Failed close board {board_id}");
    }
}