using System.Collections.ObjectModel;
using System.Diagnostics;
using Trion;
using TRION_SDK_UI.Models;


public class Enclosure
{
    public string? Name { get; set; }
    public ObservableCollection<Board> Boards { get; set; } = [];
    public void AddBoard(int boardId)
    {
        var error = TrionApi.DeWeSetParam_i32(boardId, TrionCommand.OPEN_BOARD, 0);
        if (error != TrionError.NONE)
        {
            System.Diagnostics.Debug.WriteLine($"TRION_API: OpenBoard failed for board {boardId}");
            return;
        }

        var boardPropertiesXml = TrionApi.DeWeGetParamStruct_String($"BoardID{boardId}", "boardproperties").value;
        var boardPropertiesModel = new BoardPropertyModel(boardPropertiesXml);
        var test = TrionApi.DeWeGetParamStruct_String($"BoardID{boardId}", "ScanDescriptor").value;
        Debug.WriteLine($"This is a test: {test}");

        var newBoard = new Board()
        {
            Id = boardId,
            Name = boardPropertiesModel.GetBoardName(),
            IsActive = true,
            BoardProperties = boardPropertiesModel,
            Channels = boardPropertiesModel.GetChannels(),
            ScanDescriptorXml = TrionApi.DeWeGetParamStruct_String($"BoardID{boardId}", "ScanDescriptor_V3").value
        };

        Boards.Add(newBoard);
    }

    public void Init(int numberOfBoards)
    {
        Name = TrionApi.DeWeGetParamXML_String("BoardID0/boardproperties/SystemInfo/EnclosureInfo", "Name").value;

        for (int i = 0; i < numberOfBoards; ++i)
        {
            AddBoard(i);
        }
    }
}
