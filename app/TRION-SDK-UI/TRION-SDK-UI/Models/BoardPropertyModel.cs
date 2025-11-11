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

    private static ChannelMode CreatePlaceholderMode() => new()
    {
        Name = "Unknown",
        Unit = string.Empty,
        Ranges = [],
        Options = [],
        DefaultValue = string.Empty
    };

    /// <summary>
    /// Attempts to get the default mode for a channel node.
    /// Returns false if the node does not represent a real channel (no modes found).
    /// Never returns a null ChannelMode.
    /// </summary>
    public bool TryGetDefaultMode(XPathNavigator channelNav, out ChannelMode mode)
    {
        mode = CreatePlaceholderMode();

        if (channelNav is null) return false;
        if (channelNav.NodeType != XPathNodeType.Element) return false;

        var modes = GetChannelModes(channelNav);
        if (modes.Count == 0)
        {
            // Not a channel node (likely metadata) -> signal caller to skip
            return false;
        }

        var defaultName = channelNav.GetAttribute("Default", "");
        if (!string.IsNullOrWhiteSpace(defaultName))
        {
            var match = modes.FirstOrDefault(m => string.Equals(m.Name, defaultName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                mode = match;
                return true;
            }
        }

        // Fallback to first available mode
        mode = modes[0];
        return true;
    }

    public List<Channel> GetChannels()
    {
        var channels = new List<Channel>();

        var boardId = GetBoardID();
        var boardName = GetBoardName();

        var iterator = _navigator.Select("Properties/ChannelProperties/*");
        while (iterator.MoveNext())
        {
            var channelNav = iterator.Current;
            if (channelNav == null) continue;

            // Skip non-element or metadata nodes early
            if (channelNav.NodeType != XPathNodeType.Element) continue;

            // Determine if this node yields any modes; skip if not
            var allModes = GetChannelModes(channelNav);
            if (allModes.Count == 0)
                continue;

            if (!TryGetDefaultMode(channelNav, out var defaultMode))
                continue; // Defensive; should already be covered by modes count

            var unit = defaultMode.Unit ?? string.Empty;
            var range = defaultMode.DefaultValue ?? string.Empty;

            var channel = new Channel
            {
                BoardID = boardId,
                BoardName = boardName,
                Name = channelNav.Name,
                Type = GetChannelType(channelNav.Name),
                ModeList = allModes,
                Mode = defaultMode,
                Unit = unit,
                Range = range
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
                    .Select(e => e.Value)
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
                Unit = rangeNav?.GetAttribute("Unit", "") ?? string.Empty,
                Ranges = rangeNav?
                    .SelectChildren(XPathNodeType.Element)
                    .Cast<XPathNavigator>()
                    .Where(e => e.Name.StartsWith("ID"))
                    .Select(e => double.TryParse(e.Value, out var v) ? v : 0)
                    .ToList() ?? [],
                Options = GetModeOptions(modeNav),
                DefaultValue = rangeNav?.GetAttribute("Default", "") ?? string.Empty
            };
            modes.Add(mode);
        }
        return modes;
    }
}