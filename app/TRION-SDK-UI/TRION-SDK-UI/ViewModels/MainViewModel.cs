using System.Collections.ObjectModel;
using Trion;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<string> ChannelNames { get; } = new();
    public ObservableCollection<string> LogMessages { get; } = new();

    

    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = new ObservableCollection<Board>
        {
            //new Board { Name = "Board 1", IsActive = true },
            //new Board { Name = "Board 2", IsActive = true },
            //new Board { Name = "Board 3", IsActive = false }
        }
    };

    // ... other properties and methods ...
    public MainViewModel()
    {
        ChannelNames.Add("Channel 1");
        ChannelNames.Add("Channel 2");
        ChannelNames.Add("Channel 3");

        LogMessages.Add("App started.");

        var nNoOfBoards = Math.Abs(TrionApi.Initialize());

        var enc0_name = TrionApi.DeWeGetParamXML_String("BoardID0/boardproperties/SystemInfo/EnclosureInfo", "Name").value;
        MyEnc.Name = enc0_name;

        TrionApi.DeWeSetParam_i32(0, TrionCommand.OPEN_BOARD_ALL, 0);

        for (int i = 0; i < nNoOfBoards; ++i)
        {
            var board_name = TrionApi.DeWeGetParamStruct_String($"BoardID0{i}", "BoardName").value;
            MyEnc.Boards.Add(new Board { Name = board_name, IsActive = true });
        }
        OnPropertyChanged(nameof(MyEnc));
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);

        // Call your uninitialize function here
        TrionApi.Uninitialize();
    }
}