namespace TRION_SDK_UI;

public partial class ChannelDetailPage : ContentPage
{
    public ChannelDetailPage(TRION_SDK_UI.Models.Channel ch)
    {
        InitializeComponent();
        BindingContext = ch;
    }
}