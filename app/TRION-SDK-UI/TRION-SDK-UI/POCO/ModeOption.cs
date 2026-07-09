namespace TRION_SDK_UI.POCO
{
    public class ModeOption
    {
        public double Default { get; set; }
        public required string Name { get; set; }
        public required List<string> Values { get; set; }
        public string? Unit { get; set; }
        public string? Programmable { get; set; }
        public double ProgMax { get; set; }
        public double ProgMin { get; set; }
        public double ProgRes { get; set; }
    }
}
