namespace TRION_SDK_UI.POCO;

public readonly record struct ChannelRangeStats(
    string ChannelKey,
    double Min,
    double Max,
    double Average,
    int SampleCount);