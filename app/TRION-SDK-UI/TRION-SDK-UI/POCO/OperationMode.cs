namespace TRION_SDK_UI.POCO
{
    public record class OperationMode
    {
        public int DefaultIndex { get; init; }
        public required string[] Modes { get; init; } 
    }
}
