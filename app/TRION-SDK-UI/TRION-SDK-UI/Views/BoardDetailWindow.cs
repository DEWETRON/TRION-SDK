using TRION_SDK_UI.Models;

namespace TRION_SDK_UI;

public sealed class BoardDetailWindow : Window
{
    public BoardDetailWindow(Board board)
        : base(new BoardDetailPage(board))
    {
        Title = $"Board {board.Id}/{board.Name}";
        Width = 520;
        Height = 520;
        X = 140;
        Y = 140;
    }
}