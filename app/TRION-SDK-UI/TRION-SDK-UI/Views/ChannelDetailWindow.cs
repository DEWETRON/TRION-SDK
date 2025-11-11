namespace TRION_SDK_UI;

public sealed class ChannelDetailWindow : Window
{
    public ChannelDetailWindow(TRION_SDK_UI.Models.Channel channel)
        : base(new ChannelDetailPage(channel))
    {
        Title = $"Channel {channel.BoardID}/{channel.Name}";
        Width = 600;
        Height = 600;

        X = 120;
        Y = 120;
    }
}