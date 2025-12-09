namespace TRION_SDK_UI.POCO;

public record class ExternalTrigger
{
    public bool IsPresent { get; init; } = true;
    public required int DefaultIndex { get; init; }
    public required string[] Values { get; init; } = [];
}
