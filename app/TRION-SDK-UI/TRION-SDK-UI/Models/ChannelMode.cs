public class ChannelMode
{

    public enum UnitEnum
    {
        None = 0,
        Voltage = 1,
        MiliAmperes = 2,
        Hertz = 3,
        Ohm = 4,
    }
    required public string Name { get; set; }
    public UnitEnum Unit { get; set; }
    required public List<Double> Ranges { get; set; }
}