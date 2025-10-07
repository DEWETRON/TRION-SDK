using System.Xml.XPath;

/// <summary>
/// Parses a TRION ScanDescriptor XML (V3) and exposes
/// per-channel layout information needed to map raw scan buffer data
/// (offsets, sizes, ordering) to logical channels.
/// </summary>
/// <remarks>
/// Notes:
/// - scan_size attribute is given in bits (divided by 8 to get bytes).
/// - Sample 'size' is assumed to be in bits (kept as-is here; consumer can interpret).
/// - Sample 'offset' and 'pos' semantics are taken as provided by the device (no unit conversion).
/// Robustness:
/// - Throws on missing ScanDescription (caller should treat as fatal).
/// - Skips channels with missing Sample node instead of failing the entire parse.
/// </remarks>
public partial class ScanDescriptorDecoder
{
    /// <summary>
    /// Immutable per-channel description extracted from the scan descriptor.
    /// </summary>
    public class ChannelInfo
    {
        /// <summary>Channel name (e.g., AI0).</summary>
        public string? Name { get; set; }
        /// <summary>Channel type string as supplied by XML (e.g., analog, digital).</summary>
        public string? Type { get; set; }
        /// <summary>Zero-based channel index within the scan order.</summary>
        public uint Index { get; set; }
        /// <summary>Position (bit or byte aligned—per device spec) reported in 'pos' attribute.</summary>
        public uint SamplePos { get; set; }
        /// <summary>Sample size (typically in bits) as reported by 'size' attribute.</summary>
        public uint SampleSize { get; set; }
        /// <summary>Sample byte offset within a scan frame (as reported by 'offset').</summary>
        public uint SampleOffset { get; set; }
    }

    /// <summary>
    /// Parsed collection of channel layout entries in scan order.
    /// </summary>
    public List<ChannelInfo> Channels { get; private set; } = [];

    /// <summary>
    /// Total scan size in bytes (derived from scan_size / 8).
    /// </summary>
    public uint ScanSizeBytes { get; private set; }

    /// <summary>
    /// Constructs the decoder and immediately parses the provided ScanDescriptor XML.
    /// </summary>
    /// <param name="scanDescriptorXML">Raw ScanDescriptor XML string.</param>
    /// <exception cref="Exception">Thrown if required root/ScanDescription node is missing.</exception>
    public ScanDescriptorDecoder(string scanDescriptorXML)
    {
        ParseScanDescriptor(scanDescriptorXML);
    }

    /// <summary>
    /// Core parser: loads XML via XPath and extracts scan size + channel/sample attributes.
    /// </summary>
    /// <param name="scanDescriptorXML">Raw XML text.</param>
    private void ParseScanDescriptor(string scanDescriptorXML)
    {
        var doc = new XPathDocument(new StringReader(scanDescriptorXML));
        var nav = doc.CreateNavigator();

        // Locate ScanDescription regardless of intermediate container element.
        var scanDescNode = nav.SelectSingleNode("ScanDescriptor/*/ScanDescription")
            ?? throw new Exception("ScanDescriptor unexpected element (ScanDescription not found).");

        // scan_size (bits) -> bytes
        ScanSizeBytes = uint.Parse(scanDescNode.GetAttribute("scan_size", "")) / 8;

        // Enumerate channels
        var channelNodes = scanDescNode.Select("Channel");
        while (channelNodes.MoveNext())
        {
            var channel = channelNodes.Current;
            if (channel == null) continue;

            var sample = channel.SelectSingleNode("Sample");
            if (sample == null) continue; // Skip if malformed

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