using System.Collections.ObjectModel;
using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models;
    public class Enclosure
{
    public string? Name { get; set; }

    public ObservableCollection<Board> Boards { get; set; } = [];

    public void AddBoard(int boardId)
    {
        var error = TrionApi.DeWeSetParam_i32(boardId, TrionCommand.OPEN_BOARD, 0);
        Utils.CheckErrorCode(error, "Failed to open board");

        var boardPropertiesXml = TrionApi.DeWeGetParamStruct_String($"BoardID{boardId}", "boardproperties").value;
        var boardPropertiesModel = new BoardPropertyModel(boardPropertiesXml);

        var newBoard = new Board
        {
            Id = boardId,
            Name = boardPropertiesModel.BoardName,
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