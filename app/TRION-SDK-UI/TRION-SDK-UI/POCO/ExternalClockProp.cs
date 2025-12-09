namespace TRION_SDK_UI.POCO;

public record class ExternalClockProp
{
    public bool IsPresent { get; init; } = true;
    public int DefaultIndex { get; init; }
    public string[] Values { get; init; } = [];
}
