using System.Collections.ObjectModel;
using Trion;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;


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
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);

        // Call your uninitialize function here
        TrionApi.Uninitialize();
    }
}