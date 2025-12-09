namespace TRION_SDK_UI.POCO;

public record class OperationMode
{
    public bool IsPresent { get; init; } = true;
    public int DefaultIndex { get; init; }
    public string[] Modes { get; init; } = [];
}
