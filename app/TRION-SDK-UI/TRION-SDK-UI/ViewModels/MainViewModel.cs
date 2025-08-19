using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Threading.Channels;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<TRION_SDK_UI.Models.Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    public ISeries[] MeasurementSeries { get; set; } = Array.Empty<ISeries>();
    public ObservableCollection<double> ChannelMeasurementData { get; } = [];
    public ICommand ChannelSelectedCommand { get; }

    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = []
    };

    private CancellationTokenSource _cts;
    private Task _acquisitionTask;

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

    public Axis[] YAxes { get; set; }

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
            foreach (var channel in board.Channels)
            {
                if (channel.Name != null)
                {
                    Channels.Add(channel);
                }
            }
        }

        OnPropertyChanged(nameof(Channels));
        ChannelSelectedCommand = new Command<TRION_SDK_UI.Models.Channel>(OnChannelSelected);

        UpdateYAxes();
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);

        // Call your uninitialize function here
        TrionApi.Uninitialize();
    }

    private void OnChannelSelected(TRION_SDK_UI.Models.Channel selectedChannel)
    {
        _cts?.Cancel();
        _acquisitionTask?.Wait();
        ChannelMeasurementData.Clear();

        MeasurementSeries = [
        new LineSeries<double>
        {
            Values = ChannelMeasurementData,
            Name = $"{selectedChannel.Name}",
            AnimationsSpeed = TimeSpan.Zero,
            GeometrySize = 0 // <-- This removes the dots
        }];
        OnPropertyChanged(nameof(MeasurementSeries));

        _cts = new CancellationTokenSource();
        _acquisitionTask = Task.Run(() => AcquireDataLoop(selectedChannel), _cts.Token);
    }

    private void AcquireDataLoop(TRION_SDK_UI.Models.Channel selectedChannel)
    {
        var board_id = selectedChannel.BoardID;
        var channel_name = selectedChannel.Name;

        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Used", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/{channel_name}", "Used", "True");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/{channel_name}", "Range", "10 V");

        MyEnc.Boards[board_id].SetAcquisitionProperties(sampleRate: "2000", buffer_block_size: 200, buffer_block_count: 50);
        MyEnc.Boards[board_id].UpdateBoard();

        var (adcDelayError, adc_delay) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BOARD_ADC_DELAY);
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);
        CircularBuffer buffer = new(board_id);

        while (!_cts.IsCancellationRequested)
        {
            var (available_samples_error, available_samples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                Thread.Sleep(10);
                continue;
            }
            var (read_pos_error, read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            read_pos += adc_delay * sizeof(UInt32);
            List<double> tempValues = [.. new double[available_samples]];
            Debug.WriteLine($"Acquiring {available_samples} samples from board {board_id} at position {read_pos}.");
            for (int i = 0; i < available_samples; ++i)
            {
                if (read_pos >= buffer.EndPosition)
                {
                    read_pos -= buffer.Size;
                }

                float value = Marshal.ReadInt32((IntPtr)read_pos);
                value = (float)((float)value / 0x7FFFFF00 * 10.0);
                tempValues[i] = value;

                read_pos += sizeof(UInt32);
            }
            TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);

            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                int i = 0;
                foreach (var v in tempValues)
                {
                    if (i < ChannelMeasurementData.Count)
                    {
                        ChannelMeasurementData[i] = v; // Overwrite existing value
                    }
                    else
                    {
                        ChannelMeasurementData.Add(v); // Add new value if needed
                    }
                    i++;
                }
            });
        }
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
    }
}