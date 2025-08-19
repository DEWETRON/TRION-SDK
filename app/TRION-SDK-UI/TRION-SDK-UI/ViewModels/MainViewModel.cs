using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.Threading.Channels;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;
using System.Runtime.InteropServices;

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
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);

        // Call your uninitialize function here
        TrionApi.Uninitialize();
    }

    private void OnChannelSelected(TRION_SDK_UI.Models.Channel selectedChannel)
    {
        var message = $"Board {selectedChannel.BoardID} - {selectedChannel.Name}";
        LogMessages.Add(message);
        ChannelMeasurementData.Clear();
        GetMeasurementData(selectedChannel);

    }

    private void GetMeasurementData(TRION_SDK_UI.Models.Channel selectedChannel)
    {
        var board_id = selectedChannel.BoardID;
        var channel_name = selectedChannel.Name;

        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Used", "False");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/{channel_name}", "Used", "True");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Range", "10 V");
        TrionApi.DeWeSetParamStruct($"BoardID{board_id}/{channel_name}", "Range", "10 V");

        MyEnc.Boards[board_id].SetAcquisitionProperties();
        MyEnc.Boards[board_id].UpdateBoard();

        var (adcDelayError, adc_delay) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BOARD_ADC_DELAY);
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);


        CircularBuffer buffer = new(board_id);

        { //while (true)
            var (available_samples_error, available_samples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                // continue;
            }
            var (read_pos_error, read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            read_pos += adc_delay * sizeof(UInt32);
            for (int i = 0; i < available_samples; ++i)
            {
                if (read_pos >= buffer.EndPosition)
                {
                    read_pos -= buffer.Size;
                }

                float value = Marshal.ReadInt32((IntPtr)read_pos);
                value = (float)((float)value / 0x7FFFFF00 * 10.0);

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
            TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);
        }
        TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);

        MeasurementSeries =
        [
        new LineSeries<double>
        {
            Values = ChannelMeasurementData,
            Name = $"{channel_name}",
            AnimationsSpeed = TimeSpan.Zero
        }
        ];
        OnPropertyChanged(nameof(MeasurementSeries));
    }
}