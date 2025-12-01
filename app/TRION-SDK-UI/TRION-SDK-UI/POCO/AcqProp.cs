namespace TRION_SDK_UI.POCO
{
    public class AcqProp
    {
        public required SampleRateProp SampleRateProp { get; set; }
        public required OperationMode OperationModeProp { get; set; }
        public required ExternalTrigger ExternalTriggerProp { get; set; }
        public required ExternalClockProp ExternalClockProp { get; set; }

    }
}
