using TRION_SDK_UI.Models;
using TRION_SDK_UI.ViewModels;

namespace TRION_SDK_UI;

public partial class ChannelDetailPage : ContentPage
{
    public ChannelDetailPage(Channel ch)
    {
        InitializeComponent();
        BindingContext = new ChannelDetailViewModel(ch);
    }
}