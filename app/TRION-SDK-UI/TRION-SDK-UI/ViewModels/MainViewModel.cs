using System.Collections.ObjectModel;
using Trion;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using TrionApiUtils;
using System.Windows.Input;
using TRION_SDK_UI.Models;

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

        var enclosure0Name = TrionApi.DeWeGetParamXML_String("BoardID0/boardproperties/SystemInfo/EnclosureInfo", "Name").value;
        MyEnc.Name = enclosure0Name;

        TrionApi.DeWeSetParam_i32(0, TrionCommand.OPEN_BOARD_ALL, 0);


        for (int i = 0; i < numberOfBoards; ++i)
        {
            var boardName = TrionApi.DeWeGetParamStruct_String($"BoardID0{i}", "BoardName").value;
            var boardProperties = TrionApi.DeWeGetParamStruct_String($"BoardID0{i}", "boardproperties").value;
            var boardPropertiesModel = new BoardPropertyModel(boardProperties);
            var newBoard = new Board(i, boardName, true, boardPropertiesModel, string.Empty);

            MyEnc.Boards.Add(newBoard);

            // Restore channel names population
            foreach (string channelName in boardPropertiesModel.GetChannelNames())
            {
                GetScanDescriptor(newBoard, channelName);
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

    public void GetScanDescriptor(Board board, string channelName)
    {
        var (error, scan_descriptor) = TrionApi.DeWeGetParamStruct_String($"BoardID{board.Id}/{channelName}", "ScanDescriptor");
        LogMessages.Add(error.ToString());
        board.ScanDescriptor = scan_descriptor;
    }

    public string SelectedChannel { get; set; }

    public ICommand ChannelSelectedCommand { get; }

    private async void OnChannelSelected(string channelName)
    {
        LogMessages.Add($"Channel selected: {channelName}");
    }

    public ObservableCollection<string> ChannelData { get; } = new();
}