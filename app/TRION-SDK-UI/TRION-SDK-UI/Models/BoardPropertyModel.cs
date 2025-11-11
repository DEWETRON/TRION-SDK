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

    public string GetDefaultMode(string name)
    {
        var defaultNode = _navigator.SelectSingleNode($"Properties/ChannelProperties/{name}");
        if (defaultNode != null)
        {
            return defaultNode.GetAttribute("Default", "");
        }
        return string.Empty;
    }

    public string GetUnit(string name)
    {
        var unitNode = _navigator.SelectSingleNode($"Properties/ChannelProperties/{name}/Mode[@Mode='{GetDefaultMode(name)}']/Range");
        if (unitNode != null)
        {
            return unitNode.GetAttribute("Unit", "");
        }
        return string.Empty;
    }

    public List<Channel> GetChannels()
    {
        var channels = new List<Channel>();
        var iterator = _navigator.Select("Properties/ChannelProperties/*");
        while (iterator.MoveNext())
        {
            var channelNav = iterator.Current;
            if (channelNav == null) continue;

            var channel = new Channel()
            {
                BoardID = GetBoardID(),
                BoardName = GetBoardName(),
                Name = channelNav.Name,
                Type = GetChannelType(channelNav.Name),
                ModeList = GetChannelModes(channelNav),
                Mode = GetChannelModes(channelNav)
                    .FirstOrDefault(m => m.Name == GetDefaultMode(channelNav.Name))!,
                Unit = GetUnit(channelNav.Name)
            };
            channels.Add(channel);
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

    private static List<ModeOption> GetModeOptions(XPathNavigator modeNav)
    {
        var options = new List<ModeOption>();
        var optionIterator = modeNav.SelectChildren("", "");
        while (optionIterator.MoveNext())
        {
            var optionNav = optionIterator.Current;
            if (optionNav == null) continue;
            
            var option = new ModeOption
            {
                Name = optionNav.Name,
                Default = double.TryParse(optionNav.GetAttribute("Default", ""), out var def) ? def : 0,
                Unit = optionNav.GetAttribute("Unit", ""),
                Programmable = optionNav.GetAttribute("Programmable", ""),
                ProgMax = double.TryParse(optionNav.GetAttribute("ProgMax", ""), out var pmax) ? pmax : 0,
                ProgMin = double.TryParse(optionNav.GetAttribute("ProgMin", ""), out var pmin) ? pmin : 0,
                ProgRes = double.TryParse(optionNav.GetAttribute("ProgRes", ""), out var pres) ? pres : 0,
                Values = optionNav?
                    .SelectChildren(XPathNodeType.Element)
                    .Cast<XPathNavigator>()
                    .Where(static e => !string.IsNullOrEmpty(e.Value))
                    .Select(e => (e.Value))
                    .ToList() ?? [],
            };
            options.Add(option);
        }
        return options;
    }

    public static List<ChannelMode> GetChannelModes(XPathNavigator channelNav)
    {
        var modes = new List<ChannelMode>();
        var modeIterator = channelNav.SelectChildren("Mode", "");
        while (modeIterator.MoveNext())
        {
            var modeNav = modeIterator.Current;
            if (modeNav == null) continue;

            var rangeNav = modeNav.SelectSingleNode("Range");
            var mode = new ChannelMode
            {
                Name = modeNav.GetAttribute("Mode", ""),
                Unit = ParseUnit(rangeNav?.GetAttribute("Unit", "")),
                Ranges = rangeNav?
                    .SelectChildren(XPathNodeType.Element)
                    .Cast<XPathNavigator>()
                    .Where(e => e.Name.StartsWith("ID"))
                    .Select(e => double.TryParse(e.Value, out var v) ? v : 0)
                    .ToList() ?? [],
                Options = GetModeOptions(modeNav)
            };
            modes.Add(mode);
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