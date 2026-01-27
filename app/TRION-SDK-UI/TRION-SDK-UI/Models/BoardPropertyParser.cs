using System.Xml.Linq;
using TRION_SDK_UI.POCO;
using static TRION_SDK_UI.Models.Channel;

namespace TRION_SDK_UI.Models;

public sealed class BoardPropertyParser
{
    private readonly XDocument _xmlDocument;
    private readonly XElement _rootElement;
    private readonly XElement _AcquisitionProperties;

    private string GetBoardInfoValue(string elementName) => _rootElement.Element("BoardInfo")?.Element(elementName)?.Value ?? string.Empty;

    public BoardPropertyParser(string boardPropertiesXml)
    {
        ArgumentNullException.ThrowIfNull(boardPropertiesXml, nameof(boardPropertiesXml));
        _xmlDocument = XDocument.Parse(boardPropertiesXml);
        _rootElement = _xmlDocument.Root ?? throw new InvalidOperationException("Invalid XML: Missing root element.");
        _AcquisitionProperties = _rootElement.Element("AcquisitionProperties") ?? throw new InvalidOperationException("Invalid XML: Missing AcquisitionProperties element.");
    }

    private static ChannelType GetChannelTypeFromString(string name)
    {
        if (name.StartsWith("AI", StringComparison.OrdinalIgnoreCase)) return ChannelType.Analog;
        if (name.StartsWith("Discret", StringComparison.OrdinalIgnoreCase)) return ChannelType.Digital;
        if (name.StartsWith("CNT", StringComparison.OrdinalIgnoreCase)) return ChannelType.Counter;
        if (name.StartsWith("BoardCNT", StringComparison.OrdinalIgnoreCase)) return ChannelType.BoardCounter;
        return ChannelType.Unknown;
    }

    public Board CreateBoard(int ID, string scanDescriptorXML, int bufferBlockCount)
    {
        var boardName = GetBoardName();

        return new Board
        {
            Id = ID,
            BoardProperties = this,
            ScanDescriptorXml = scanDescriptorXML,
            Name = boardName,
            Channels = GetChannels(ID, boardName),
            SamplingRate = GetDefaultIntAcqPropFromString("SampleRate"),
            ExternalTrigger = GetDefaultStringAcqPropFromString("ExtTrigger"),
            ExternalClock = GetDefaultStringAcqPropFromString("ExtClk"),
            OperationMode = GetDefaultStringAcqPropFromString("OperationMode"),
            BufferBlockCount = bufferBlockCount,
            SampleRateDivider = GetDefaultIntAcqPropFromString("SampleRateDivider"),
            ResolutionAI = "Test"
        };
    }

    private static int GetDefaultIntValueFromElement(int defaultIndex, XElement element)
    {
        var valueStr = element.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals($"ID{defaultIndex}", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (int.TryParse(valueStr, out var rate))
        {
            return rate;
        }
        
        throw new InvalidOperationException($"Invalid XML: Unable to parse integer value at ID{defaultIndex} in {element.Name}.");
    }

    private static string GetDefaultStringValueFromElement(int defaultIndex, XElement element)
    {
        var value = element.Elements()
            .FirstOrDefault(e => e.Name.LocalName.Equals($"ID{defaultIndex}", StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Invalid XML: Unable to parse string value at ID{defaultIndex} in {element.Name}.");
        }
        return value;
    }

    private string GetDefaultStringAcqPropFromString(string str)
    {
        var acqProp = _AcquisitionProperties.Element("AcqProp");
        var element = acqProp?.Element(str);
        
        if (element == null) return string.Empty;

        var defaultIndex = element.GetAttrInt("Default", -1);
        if (defaultIndex < 0) return string.Empty;

        return GetDefaultStringValueFromElement(defaultIndex, element);
    }

    private int GetDefaultIntAcqPropFromString(string str)
    {
        var acqProp = _AcquisitionProperties.Element("AcqProp");
        var element = acqProp?.Element(str);

        if (element == null) return 0;

        var defaultIndex = element.GetAttrInt("Default", -1);
        if (defaultIndex < 0) return 0;

        return GetDefaultIntValueFromElement(defaultIndex, element);

    }

    private static string GetDefaultRange(ChannelMode mode)
    {
        if (int.TryParse(mode.DefaultValue, out var idx))
        {
            if (idx >= 0 && idx < mode.Ranges.Count) return mode.Ranges[idx];
        }
        return mode.Ranges.FirstOrDefault() ?? string.Empty;
    }


    public string GetBoardName() => GetBoardInfoValue("BoardName");

    public List<Channel> GetChannels(int boardId = -1, string boardName = "")
    {
        var channels = new List<Channel>(128);
        var channelProps = _rootElement.Element("ChannelProperties");

        if (channelProps == null) return channels;

        foreach (var channelElem in channelProps.Elements())
        {
            var type = GetChannelTypeFromString(channelElem.Name.LocalName);
            if (type == ChannelType.Unknown) continue;

            var modes = GetChannelModes(channelElem);
            if (modes.Count == 0) continue;

            var defaultModeName = channelElem.GetAttrString("Default");
            var currentMode = modes.FirstOrDefault(m => m.Name.Equals(defaultModeName, StringComparison.OrdinalIgnoreCase))
                              ?? modes.First();

            var defaultRange = GetDefaultRange(currentMode);

            if (ChannelType.Analog == GetChannelTypeFromString(channelElem.Name.LocalName))
            {
                channels.Add(new AnalogChannel
                {
                    BoardID = boardId,
                    BoardName = boardName,
                    Name = channelElem.Name.LocalName,
                    ModeList = modes,
                    Mode = currentMode,
                    Unit = currentMode.Unit ?? string.Empty,
                    Range = defaultRange
                });
            }
            else if (ChannelType.Digital == GetChannelTypeFromString(channelElem.Name.LocalName))
            {
                channels.Add(new DigitalChannel
                {
                    BoardID = boardId,
                    BoardName = boardName,
                    Name = channelElem.Name.LocalName,
                    ModeList = modes,
                    Mode = currentMode,
                    Unit = currentMode.Unit ?? string.Empty,
                    Range = defaultRange
                });
            }
            else if (ChannelType.Counter == GetChannelTypeFromString(channelElem.Name.LocalName))
            {
                channels.Add(new CounterChannel
                {
                    BoardID = boardId,
                    BoardName = boardName,
                    Name = channelElem.Name.LocalName,
                    ModeList = modes,
                    Mode = currentMode,
                    Unit = currentMode.Unit ?? string.Empty,
                    Range = defaultRange
                });
            }
        }
        return channels;
    }

    private XElement? AcqPropElem => _rootElement.Element("AcquisitionProperties")?.Element("AcqProp");

    public IEnumerable<string> GetAvailableValuesFromString(string str)
    {
        return AcqPropElem?.Element(str)?.Elements().Select(e => e.Value) ?? [];
    }

    public (bool IsProg, int Min, int Max, List<int> Rates) GetSampleRateCapabilities()
    {
        var elem = AcqPropElem?.Element("SampleRate");
        if (elem == null) return (false, 0, 0, []);

        var isProg = bool.TryParse(elem.Attribute("Programmable")?.Value, out var p) && p;
        int.TryParse(elem.Attribute("ProgMin")?.Value, out var min);
        int.TryParse(elem.Attribute("ProgMax")?.Value, out var max);

        var rates = elem.Elements()
            .Select(e => int.TryParse(e.Value, out var r) ? r : -1)
            .Where(r => r > 0)
            .ToList();

        return (isProg, min, max, rates);
    }

    public (int Min, int Max, List<int> Proposed) GetDividerCapabilities()
    {
        var elem = AcqPropElem?.Element("SampleRateDivider");
        if (elem == null) return (0, 0, []);

        int.TryParse(elem.Attribute("ProgMin")?.Value, out var min);
        int.TryParse(elem.Attribute("ProgMax")?.Value, out var max);

        var proposed = elem.Elements()
            .Select(e => int.TryParse(e.Value, out var r) ? r : -1)
            .Where(r => r > 0)
            .ToList();

        return (min, max, proposed);
    }

    private static List<ChannelMode> GetChannelModes(XElement channelElem)
    {
        var modes = new List<ChannelMode>();
        foreach (var modeElem in channelElem.Elements("Mode"))
        {
            var name = modeElem.GetAttrString("Mode");
            if (string.IsNullOrWhiteSpace(name)) name = modeElem.GetAttrString("Name");

            var rangeElem = modeElem.Element("Range");
            var unit = rangeElem?.GetAttrString("Unit") ?? modeElem.GetAttrString("Unit");
            var defaultVal = rangeElem?.GetAttrString("Default");

            var ranges = rangeElem?.Elements()
                .Where(e => e.Name.LocalName.StartsWith("ID"))
                .Select(e => e.Value.Trim())
                .ToList() ?? [];

            modes.Add(new ChannelMode
            {
                Name = name,
                Unit = unit,
                Ranges = ranges,
                DefaultValue = defaultVal,
                Options = []
            });
        }
        return modes;
    }

}

file static class XmlExt
{
    public static string GetAttrString(this XElement? e, string name)
        => e?.Attribute(name)?.Value ?? string.Empty;

    public static int GetAttrInt(this XElement? e, string name, int def = 0)
        => int.TryParse(e?.Attribute(name)?.Value, out var i) ? i : def;
}