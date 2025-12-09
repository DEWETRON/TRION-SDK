namespace TRION_SDK_UI.POCO
{
    public record class SampleRateDividerProp
    {
        public required int ProgMin { get; init; }
        public required int ProgMax { get; init; }
        public required int Default { get; init; }
        public required List<int> ProposedValues { get; init; }
    }
}
