using TRION_SDK_UI.Models;
using TRION_SDK_UI.ViewModels;

namespace TRION_SDK_UI;

public partial class BoardDetailPage : ContentPage
{
    public BoardDetailPage(Board board)
    {
        InitializeComponent();

        var vm = new BoardDetailViewModel(board);
        vm.CloseRequested += (_, __) =>
        {
            var window = this.Window;
            if (window is not null)
                Application.Current?.CloseWindow(window);
        };
        BindingContext = vm;
    }
}