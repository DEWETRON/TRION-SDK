namespace TRION_SDK_UI.POCO
{
    public record class ExternalTrigger
    {
        public required int DefaultIndex { get; init; }
        public required string[] Values { get; init; }
    }
}
