using System.Collections.ObjectModel;
using Trion;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using TrionApiUtils;
using System.Windows.Input;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<string> ChannelNames { get; } = new();
    public ObservableCollection<string> LogMessages { get; } = new();

    public ISeries[] MeasurementSeries { get; set; }

    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = new ObservableCollection<Board>{}
    };

    // ... other properties and methods ...
    public MainViewModel()
    {
        LogMessages.Add("App started.");

        var nNoOfBoards = Math.Abs(TrionApi.Initialize());

        var enc0_name = TrionApi.DeWeGetParamXML_String("BoardID0/boardproperties/SystemInfo/EnclosureInfo", "Name").value;
        MyEnc.Name = enc0_name;

        TrionApi.DeWeSetParam_i32(0, TrionCommand.OPEN_BOARD_ALL, 0);


        for (int i = 0; i < nNoOfBoards; ++i)
        {
            var board_name = TrionApi.DeWeGetParamStruct_String($"BoardID0{i}", "BoardName").value;
            var board_properties = TrionApi.DeWeGetParamStruct_String($"BoardID0{i}", "boardproperties").value;
            var board_property_model = new BoardPropertyModel(board_properties);

            MyEnc.Boards.Add(new Board { Name = board_name, IsActive = true, BoardProperties = board_property_model });


            // channels:
            foreach (string item in board_property_model.getChannelNames())
            {
                ChannelNames.Add(item);
            }


        }
        OnPropertyChanged(nameof(MyEnc));


        // Example measurement data
        var values = new ObservableCollection<double> { 1, 3, 2, 5, 4, 6, 3, 7 };
        MeasurementSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = values,
                Name = "Channel 1"
            }
        };

        ChannelSelectedCommand = new Command<string>(OnChannelSelected);
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);

        // Call your uninitialize function here
        TrionApi.Uninitialize();
    }

    // starts the board of the chosen id and enables the first channel
    public string GetScanDescriptor(int board_id, string channel_name)
    {
        LogMessages.Add($"Using Board number {board_id}");

        //open the board
        var error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.OPEN_BOARD, 0);
        (error, var scan_descriptor) = TrionApi.DeWeGetParamStruct_String($"BoardID{board_id}/{channel_name}", "ScanDescriptor");
        LogMessages.Add(error.ToString());
        return scan_descriptor;

    }

    public string SelectedChannel { get; set; }

    public ICommand ChannelSelectedCommand { get; }

    private async void OnChannelSelected(string channelName)
    {
        LogMessages.Add($"Channel selected: {channelName}");
        ChannelData.Clear();

        // Find the board containing the channel
        var board = MyEnc.Boards.FirstOrDefault(b => b.BoardProperties.getChannelNames().Contains(channelName));
        if (board == null)
        {
            LogMessages.Add("Board not found for selected channel.");
            return;
        }
        int boardId = MyEnc.Boards.IndexOf(board);

        var data = await AcquireChannelDataAsync(boardId, channelName);
        foreach (var line in data)
            ChannelData.Add(line);

        OnPropertyChanged(nameof(ChannelData));
    }

    public ObservableCollection<string> ChannelData { get; } = new();

    // Add this method to MainViewModel
    public async Task<List<string>> AcquireChannelDataAsync(int board_id, string channel_name)
    {
        var output = new List<string>();
        const int SAMPLE_RATE = 2000;
        const int BLOCK_SIZE = 200;
        const int BLOCK_COUNT = 50;

        var error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.OPEN_BOARD, 0);
        error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.RESET_BOARD, 0);

        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "OperationMode", "Slave");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtTrigger", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtClk", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "SampleRate", SAMPLE_RATE.ToString());
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Used", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AI0", "Used", "True");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Range", "10 V");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AI0", "Range", "10 V");

        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, BLOCK_SIZE);
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, BLOCK_COUNT);
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);

        var (adcDelayError, adc_delay) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BOARD_ADC_DELAY);

        _ = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);

        int polling_interval_ms = (int)(BLOCK_SIZE / (double)SAMPLE_RATE * 1000);

        var (bufferStartError, buffer_start_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_START_POINTER);
        var (bufferEndError, buffer_end_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER);
        var (bufferSizeError, buffer_size) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE);

        int available_samples = 0;
        bool stop = false;
        int maxLoops = 10; // Limit for demo purposes

        for (int loop = 0; loop < maxLoops && !stop; loop++)
        {
            await Task.Delay(polling_interval_ms);
            var (availError, availSamples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
            available_samples = availSamples - adc_delay;

            if (available_samples <= 0)
                continue;

            var (readPosError, read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            read_pos += adc_delay * sizeof(UInt32);

            for (int i = 0; i < available_samples; ++i)
            {
                if (read_pos >= buffer_end_pos)
                    read_pos -= buffer_size;

                unsafe
                {
                    Int32 raw_data = *(Int32*)read_pos;
                    float value = (float)((float)raw_data / 0x7FFFFF00 * 10.0);
                    output.Add($"Raw {raw_data,12} {value,17:#.000000000000}");
                }
                read_pos += sizeof(UInt32);
            }

            TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);
        }

        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);

        return output;
    }
}