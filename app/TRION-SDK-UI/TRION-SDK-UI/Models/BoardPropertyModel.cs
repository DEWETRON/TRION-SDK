using System.Xml.XPath;
using TRION_SDK_UI.Models;
using static TRION_SDK_UI.Models.Channel;

public class BoardPropertyModel
{
    private readonly XPathDocument _document;
    private readonly XPathNavigator _navigator;

    public BoardPropertyModel(string boardXML)
    {
        ArgumentNullException.ThrowIfNull(boardXML);

        using var stringReader = new StringReader(boardXML);
        _document = new XPathDocument(stringReader);
        _navigator = _document.CreateNavigator();
    }

    private static ChannelType GetChannelType(string name)
    {
        if (name.StartsWith("AI")) return ChannelType.Analog;
        if (name.StartsWith("Di")) return ChannelType.Digital;
        return ChannelType.Unknown;
    }

    public List<Channel> GetChannels()
    {
        var channels = new List<Channel>();
        var iterator = _navigator.Select("Properties/ChannelProperties/*");
        while (iterator.MoveNext())
        {
            var channelNav = iterator.Current;
            if (channelNav != null)
            {
                var channel = new Channel()
                {
                    BoardID = GetBoardID(),
                    BoardName = GetBoardName(),
                    Name = channelNav.Name,
                    Type = GetChannelType(channelNav.Name),
                    Modes = GetChannelModes(channelNav),
                };
                channels.Add(channel);
            }
        }
        return channels;
    }

    public string GetBoardName()
    {
        var boardName = _navigator.SelectSingleNode("/Properties/BoardInfo/BoardName");
        return boardName != null ? boardName.Value : string.Empty;
    }

    public int GetBoardID()
    {
        var propertiesNode = _navigator.SelectSingleNode("/Properties");
        if (propertiesNode != null)
        {
            var idStr = propertiesNode.GetAttribute("BoardID", "");
            if (int.TryParse(idStr, out int id))
            {
                return id;
            }
        }
        return -1;
    }

    public List<ChannelMode> GetChannelModes(XPathNavigator channelNav)
    {
        var modes = new List<ChannelMode>();
        var modeIterator = channelNav.SelectChildren("Mode", "");
        while (modeIterator.MoveNext())
        {
            var modeNav = modeIterator.Current;
            if (modeNav != null)
            {
                var rangeNav = modeNav.SelectSingleNode("Range");
                var mode = new ChannelMode
                {
                    Name = modeNav.GetAttribute("Mode", ""),
                    Unit = ParseUnit(rangeNav?.GetAttribute("Unit", "")),
                    Ranges = rangeNav?
                        .SelectChildren(XPathNodeType.Element)
                        .Cast<XPathNavigator>()
                        .Where(e => e.Name.StartsWith("ID")) // Accepts ID0, ID1, ID2...
                        .Select(e => double.TryParse(e.Value, out var v) ? v : 0)
                        .ToList() ?? []
                };
                modes.Add(mode);
            }
        }
        return modes;
    }
    private static ChannelMode.UnitEnum ParseUnit(string? unit)
    {
        return unit switch
        {
            "V" => ChannelMode.UnitEnum.Voltage,
            "mA" => ChannelMode.UnitEnum.MilliAmperes,
            "Hz" => ChannelMode.UnitEnum.Hertz,
            "Ohm" => ChannelMode.UnitEnum.Ohm,
            _ => ChannelMode.UnitEnum.None
        };
    }
}