using TRION_SDK_UI.Models;
using TRION_SDK_UI.ViewModels;

namespace TRION_SDK_UI;

public partial class ChannelDetailPage : ContentPage
{
    public ChannelDetailPage(Channel ch)
    {
        InitializeComponent();

        var vm = new ChannelDetailViewModel(ch);
        vm.CloseRequested += (_, __) =>
        {
            var window = this.Window;
            if (window is not null)
                Application.Current?.CloseWindow(window);
        };
        BindingContext = vm;
    }
}