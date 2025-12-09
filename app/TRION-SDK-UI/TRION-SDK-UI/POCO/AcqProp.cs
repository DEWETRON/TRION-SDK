namespace TRION_SDK_UI.POCO
{
    public record class AcqProp
    {
        public required SampleRateProp SampleRateProp { get; init; }
        public required OperationMode OperationModeProp { get; init; }
        public required ExternalTrigger ExternalTriggerProp { get; init; }
        public required ExternalClockProp ExternalClockProp { get; init; }
        public SampleRateDividerProp? SampleRateDividerProp { get; init; }
    }
}
