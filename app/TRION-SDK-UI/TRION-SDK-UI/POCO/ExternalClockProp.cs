namespace TRION_SDK_UI.POCO
{
    public record class ExternalClockProp
    {
        public required int DefaultIndex { get; set; }
        public required string[] Values { get; set; }
    }
}
