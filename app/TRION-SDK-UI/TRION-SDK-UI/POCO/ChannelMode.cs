namespace TRION_SDK_UI.POCO;
public class ChannelMode
{
    public required string Name { get; set; }

    public string? Unit { get; set; }

    public required List<string> Ranges { get; set; }
    public required List<ModeOption> Options { get; set; }
    public string? DefaultValue { get; set; }
}