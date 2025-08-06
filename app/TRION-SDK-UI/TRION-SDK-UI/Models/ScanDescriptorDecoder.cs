using System;
using System.Collections.Generic;
using System.Xml.XPath;
using TRION_SDK_UI.Models;

public partial class ScanDescriptorDecoder
{
    public List<Channel> Channels = new();
    public uint ScanSizeBytes;

    public ScanDescriptorDecoder(string scanDescriptorXML)
    {
        ParseScanDescriptor(scanDescriptorXML);
    }

    private void ParseScanDescriptor(string scanDescriptorXML)
    {
        var doc = new XPathDocument(new System.IO.StringReader(scanDescriptorXML));
        var nav = doc.CreateNavigator();

        var scanDescNode = nav.SelectSingleNode("ScanDescriptor/*/ScanDescription");
        if (scanDescNode == null)
        {
            throw new Exception("ScanDescriptor unexpected element");
        }

        ScanSizeBytes = uint.Parse(scanDescNode.GetAttribute("scan_size", "")) / 8;

        var channelNodes = scanDescNode.Select("Channel");
        while (channelNodes.MoveNext())
        {
            var channel = channelNodes.Current;
            var sample = channel.SelectSingleNode("Sample");
            Channels.Add(new Channel
            {
                Name = channel.GetAttribute("name", ""),
                ChannelType = channel.GetAttribute("type", ""),
                Index = uint.Parse(channel.GetAttribute("index", "")),
                SampleSize = uint.Parse(sample.GetAttribute("size", "")),
                SampleOffset = uint.Parse(sample.GetAttribute("offset", ""))
            });
        }
    }
}