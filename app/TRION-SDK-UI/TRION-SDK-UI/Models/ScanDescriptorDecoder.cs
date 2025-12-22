using System.Xml.XPath;

namespace TRION_SDK_UI.Models;
public partial class ScanDescriptorDecoder
{
    public class ChannelInfo
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public uint Index { get; set; }
        public uint SamplePos { get; set; }
        public uint SampleSize { get; set; }
        public uint SampleOffset { get; set; }
        public uint SampleSizeBytes => SampleSize / 8;
    }

    public List<ChannelInfo> Channels { get; private set; } = [];

    public uint ScanSizeBytes { get; private set; }

    public ScanDescriptorDecoder(string scanDescriptorXML)
    {
        ParseScanDescriptor(scanDescriptorXML);
    }

    private void ParseScanDescriptor(string scanDescriptorXML)
    {
        var doc = new XPathDocument(new StringReader(scanDescriptorXML));
        var nav = doc.CreateNavigator();

        var scanDescNode = nav.SelectSingleNode("ScanDescriptor/*/ScanDescription")
            ?? throw new Exception("ScanDescriptor unexpected element (ScanDescription not found).");

        ScanSizeBytes = uint.Parse(scanDescNode.GetAttribute("scan_size", "")) / 8;

        var channelNodes = scanDescNode.Select("Channel");
        while (channelNodes.MoveNext())
        {
            var channel = channelNodes.Current;
            if (channel == null) continue;

            var sample = channel.SelectSingleNode("Sample");
            if (sample == null) continue;

            Channels.Add(new ChannelInfo
            {
                Name = channel.GetAttribute("name", ""),
                Type = channel.GetAttribute("type", ""),
                Index = uint.Parse(channel.GetAttribute("index", "")),
                SamplePos = uint.Parse(sample.GetAttribute("pos", "")),
                SampleSize = uint.Parse(sample.GetAttribute("size", "")),
                SampleOffset = uint.Parse(sample.GetAttribute("offset", ""))
            });
        }
    }
}