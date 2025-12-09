namespace TRION_SDK_UI.POCO;

public record class SampleRateProp
{
    public bool IsPresent { get; init; } = true;
    public string Unit { get; init; } = string.Empty;
    public string Count { get; init; } = "0";
    public int Default { get; init; }
    public bool Programmable { get; init; }
    public int ProgMax { get; init; }
    public int ProgMin { get; init; }
    public string ProgRes { get; init; } = string.Empty;
    public string[] AvailableRates { get; init; } = [];
}
