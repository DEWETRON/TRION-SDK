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

    public ObservableCollection<double> ChartWindowData => Recorder.Window;
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
    public int MaxScrollIndex => Recorder.MaxScrollIndex;

    public ObservableCollection<Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    private bool _isScrollingLocked = true;

    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = []
    };

    private CancellationTokenSource? _cts;
    private Task? _acquisitionTask;
    private double _yAxisMin = -10;
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
    private void StartAcquisition()
    {
        LogMessages.Add("Starting acquisition...");
    }

    private void StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
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

    public Axis[]? YAxes { get; set; }
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

    public ICommand ChannelSelectedCommand { get; private set; }
    public ICommand StartAcquisitionCommand { get; private set; }
    public ICommand StopAcquisitionCommand { get; private set; }
    public ICommand LockScrollingCommand { get; private set; }
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
        ChannelSelectedCommand = new Command<Channel>(OnChannelSelected);
        StartAcquisitionCommand = new Command(StartAcquisition);
        StopAcquisitionCommand = new Command(StopAcquisition);
        LockScrollingCommand = new Command(LockScrolling);
        UpdateYAxes();
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);

        // Call your uninitialize function here
        TrionApi.Uninitialize();
    }

    private void OnChannelSelected(Channel selectedChannel)
    {
        _cts?.Cancel();
        _acquisitionTask?.Wait();
        Recorder.Data.Clear();
        Recorder.Window.Clear();

        MeasurementSeries = [
            new LineSeries<double>
            {
                Values = ChartWindowData,
                Name = $"{selectedChannel.Name}",
                AnimationsSpeed = TimeSpan.Zero,
                GeometrySize = 0
            }];
        OnPropertyChanged(nameof(MeasurementSeries));

        _cts = new CancellationTokenSource();
        _acquisitionTask = Task.Run(() => AcquireDataLoop(selectedChannel), _cts.Token);
    }

    private void AcquireDataLoop(Channel selectedChannel)
    {
        var board_id = selectedChannel.BoardID;
        var channel_name = selectedChannel.Name;

        Debug.WriteLine($"Board ID: {board_id}    channel name: {channel_name}  ");

        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Used", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/{channel_name}", "Used", "True");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/{channel_name}", "Range", "10 V");

        MyEnc.Boards[board_id].SetAcquisitionProperties(sampleRate: "2000", buffer_block_size: 200, buffer_block_count: 50);
        MyEnc.Boards[board_id].UpdateBoard();
        // --- Scan Descriptor Integration ---
        var scanDescriptorResult = TrionApi.DeWeGetParamStruct_String($"BoardID{board_id}", "ScanDescriptor_V3");
        string scanDescriptorXml = scanDescriptorResult.value;
        var decoder = new ScanDescriptorDecoder(scanDescriptorXml);
        uint scanSizeBytes = decoder.ScanSizeBytes;
        var channelInfo = decoder.Channels.FirstOrDefault(c => c.Name == channel_name);

        Debug.WriteLine( "#-------------------------------------------------");
        Debug.WriteLine($"#Board: {MyEnc.Boards[board_id]} {channel_name}   ");
        Debug.WriteLine($"#XML {scanDescriptorXml}                          ");
        Debug.WriteLine($"#Channel Name {channelInfo.Name}                  ");
        Debug.WriteLine($"#Channel Type {channelInfo.Type}                  ");
        Debug.WriteLine($"#Channel Index {channelInfo.Index}                ");
        Debug.WriteLine($"#Scan Size {decoder.ScanSizeBytes}                ");
        Debug.WriteLine($"#SamplePos {channelInfo.SamplePos}                ");
        Debug.WriteLine($"#SampleSize {channelInfo.SampleSize}              ");
        Debug.WriteLine($"#SampleOffset {channelInfo.SampleOffset}          ");
        Debug.WriteLine( "#-------------------------------------------------");

        var (adcDelayError, adc_delay) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BOARD_ADC_DELAY);
        TrionApi.DeWeSetParam_i32(board_id, TrionCommand.START_ACQUISITION, 0);
        CircularBuffer buffer = new(board_id);

        while (_cts != null && !_cts.IsCancellationRequested)
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

                read_pos += sizeof(uint);
            }
            TrionApi.DeWeSetParam_i32(board_id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Recorder.AddSamples(tempValues);

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