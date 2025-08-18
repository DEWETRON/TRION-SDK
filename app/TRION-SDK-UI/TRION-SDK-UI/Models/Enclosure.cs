using System.Collections.ObjectModel;
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

        var boardProperties = TrionApi.DeWeGetParamStruct_String($"BoardID0{boardId}", "boardproperties").value;
        var boardPropertiesModel = new BoardPropertyModel(boardProperties);
        var newBoard = new Board(boardPropertiesModel);
        newBoard.SetBoardProperties();
        Boards.Add(newBoard);
    }
}
