namespace TRION_SDK_UI.POCO
{
    public class SampleRateProp
    {
        public string? Unit { get; set; }
        public string? Count { get; set; }
        public int Default { get; set; }
        public bool Programmable { get; set; }
        public int ProgMax { get; set; }
        public int ProgMin { get; set; }
        public string? ProgRes { get; set; }
        public string[]? AvailableRates { get; set; }
    }
}
