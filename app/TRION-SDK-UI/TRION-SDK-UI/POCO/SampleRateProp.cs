namespace TRION_SDK_UI.POCO
{
    public record class SampleRateProp
    {
        public string? Unit { get; init; }
        public string? Count { get; init; }
        public int Default { get; init; }
        public bool Programmable { get; init; }
        public int ProgMax { get; init; }
        public int ProgMin { get; init; }
        public string? ProgRes { get; init; }
        public string[]? AvailableRates { get; init; }
    }
}
