namespace TRION_SDK_UI;

public sealed class ChannelDetailWindow : Window
{
    public ChannelDetailWindow(TRION_SDK_UI.Models.Channel channel)
        : base(new ChannelDetailPage(channel))
    {
        Title = $"Channel {channel.BoardID}/{channel.Name}";
        // Set desired size (supported on Windows & MacCatalyst)
        Width = 600;
        Height = 600;

        // Optional initial position (ignored on some platforms)
        X = 120;
        Y = 120;
    }
}