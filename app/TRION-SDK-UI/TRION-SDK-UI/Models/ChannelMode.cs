using TRION_SDK_UI.Models;

public class ChannelMode
{
    public enum UnitEnum
    {
        None = 0,
        Voltage = 1,
        MilliAmperes = 2,
        Hertz = 3,
        Ohm = 4,
    }

    public required string Name { get; set; }

    public string? Unit { get; set; }

    public required List<double> Ranges { get; set; }
    public required List<ModeOption> Options { get; set; }
    public string? DefaultValue { get; set; }
}