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

    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = []
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

    public ObservableCollection<string> ChannelData { get; } = [];
}