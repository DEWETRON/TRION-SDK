using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Threading.Channels;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<string> ChannelNames { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    public ISeries[] MeasurementSeries { get; set; }
    public ObservableCollection<double> ChannelMeasurementData { get; } = [];


    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = []
    };


    private static Int32 GetDataAtPos(Int64 read_pos)
    {
        // Get the sample value at the read pointer of the circular buffer
        // The sample value is 24Bit (little endian, encoded in 32bit).
        unsafe
        {
            return *(Int32*)read_pos;
        }
    }

    private static int GetPollingIntervalMs(int block_size, int sample_rate)
    {
        // Returns interval in milliseconds
        return (int)(block_size / (double)sample_rate * 1000);
    }

    // ... other properties and methods ...
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

        MyEnc.Name = TrionApi.DeWeGetParamXML_String("BoardID0/boardproperties/SystemInfo/EnclosureInfo", "Name").value;

        for (int i = 0; i < numberOfBoards; ++i)
        {
            MyEnc.AddBoard(i);
            // Restore channel names population
            foreach (string channelName in MyEnc.Boards.Last().BoardProperties.GetChannelNames())
            {
                ChannelNames.Add(channelName);
            }
        }
        OnPropertyChanged(nameof(MyEnc));

        foreach (var board in MyEnc.Boards)
        {
            LogMessages.Add($"Board: {board.Name} (ID: {board.Id})");
        }
        OnPropertyChanged(nameof(ChannelNames));

        // Example measurement data
        var values = new ObservableCollection<double> { 1, 3, 2, 5, 4, 6, 3, 7 };
        MeasurementSeries =
        [
            new LineSeries<double>
            {
                Values = values,
                Name = "Channel 1"
            }
        ];
        var board_id = 1;
        var SAMPLE_RATE = 2000;
        var BLOCK_SIZE = 200;
        var BLOCK_COUNT = 50;

        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "OperationMode", "Slave");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtTrigger", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtClk", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "SampleRate", "2000");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Used", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AI0", "Used", "True");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Range", "10 V");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AI0", "Range", "10 V");

        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, BLOCK_SIZE);
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, BLOCK_COUNT);

        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);

        var (adc_delay_error, adc_delay) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BOARD_ADC_DELAY);

        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);

        int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);

        var (buffer_start_pos_error, buffer_start_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_START_POINTER);
        var (buffer_end_pos_error, buffer_end_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER);
        var (buffer_size_error, buffer_size) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE);

        var (available_samples_error, available_samples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
        var (read_pos_error, read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);


        AddDataPoint(available_samples, read_pos, buffer_end_pos, buffer_size);




        ChannelSelectedCommand = new Command<string>(OnChannelSelected);
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);

        // Call your uninitialize function here
        TrionApi.Uninitialize();
    }

    public string? SelectedChannel { get; set; }

    public ICommand ChannelSelectedCommand { get; }

    private void OnChannelSelected(string channelName)
    {
        LogMessages.Add($"Channel selected: {channelName}");
    }


    public void AddDataPoint(int available_samples, long read_pos, long buffer_end_pos, int buffer_size)
    {
        for (int i = 0; i < available_samples; ++i)
        {
            if (read_pos >= buffer_end_pos)
            {
                read_pos -= buffer_size;
            }

            Int32 raw_data = GetDataAtPos(read_pos);
            float value = (float)((float)raw_data / 0x7FFFFF00 * 10.0);

            var dispatcher = Dispatcher.GetForCurrentThread();
            if (dispatcher != null)
            {
                dispatcher.Dispatch(() => ChannelMeasurementData.Add(value));
            }
            else
            {
                ChannelMeasurementData.Add(value);
            }

            read_pos += sizeof(UInt32);
        }
        MeasurementSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = ChannelMeasurementData,
                Name = "Channel 1"
            }
        };
        OnPropertyChanged(nameof(MeasurementSeries));
    }
}