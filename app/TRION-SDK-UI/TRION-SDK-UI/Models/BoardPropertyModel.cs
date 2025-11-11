using ScottPlot.Interactivity;
using System.Xml.XPath;
using Trion;
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

    public bool TryGetDefaultMode(XPathNavigator channelNav, out ChannelMode mode)
    {
        mode = CreatePlaceholderMode();

        if (channelNav is null) return false;
        if (channelNav.NodeType != XPathNodeType.Element) return false;

        var modes = GetChannelModes(channelNav);
        if (modes.Count == 0)
            return false;

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
            if (channelNav.NodeType != XPathNodeType.Element) continue;

            var allModes = GetChannelModes(channelNav);
            if (allModes.Count == 0)
                continue;

            if (!TryGetDefaultMode(channelNav, out var defaultMode))
                continue;

            var unit = defaultMode.Unit ?? string.Empty;
            var rangeIndexStr = defaultMode.DefaultValue ?? string.Empty;
            int rangeIndex = int.TryParse(rangeIndexStr, out var dint) ? dint : 0;
            string defaultRange = (rangeIndex >= 0 && rangeIndex < defaultMode.Ranges.Count)
                ? defaultMode.Ranges[rangeIndex]
                : defaultMode.Ranges.FirstOrDefault() ?? string.Empty;

            string targetPath = $"BoardID{boardId}/{channelNav.Name}";

            var (err, val) = TrionApi.DeWeGetParamStruct_String(targetPath, "Mode");
            string resultMode = err == TrionError.NONE ? val : string.Empty;
            (err, val) = TrionApi.DeWeGetParamStruct_String(targetPath, "Range");
            string resultRange = err == TrionError.NONE ? val : string.Empty;

            var channel = new Channel
            {
                BoardID = boardId,
                BoardName = boardName,
                Name = channelNav.Name,
                Type = GetChannelType(channelNav.Name),
                ModeList = allModes,
                Mode = defaultMode,
                Unit = unit,
                Range = defaultRange
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
                return id;
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
        if (channelNav is null || channelNav.NodeType != XPathNodeType.Element)
            return modes;

        var modeIterator = channelNav.SelectChildren("Mode", "");
        while (modeIterator.MoveNext())
        {
            var modeNav = modeIterator.Current;
            if (modeNav == null) continue;

            var rangeNav = modeNav.SelectSingleNode("Range");

            // Name attribute may be "Mode" or "Name"; fallback to node name
            var name = modeNav.GetAttribute("Mode", "");
            if (string.IsNullOrWhiteSpace(name))
                name = modeNav.GetAttribute("Name", "");
            if (string.IsNullOrWhiteSpace(name))
                name = modeNav.Name;

            // Unit can reside on Range or Mode level
            var unit = rangeNav?.GetAttribute("Unit", "");
            if (string.IsNullOrWhiteSpace(unit))
                unit = modeNav.GetAttribute("Unit", "");
            unit ??= string.Empty;

            // Collect textual ranges (IDs like ID0, ID1...). Preserve as strings.
            var ranges = rangeNav?
                .SelectChildren(XPathNodeType.Element)
                .Cast<XPathNavigator>()
                .Where(e => e.Name.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Value?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList() ?? [];

            var defaultValue = rangeNav?.GetAttribute("Default", "") ?? string.Empty;

            var mode = new ChannelMode
            {
                Name = name,
                Unit = unit,
                Ranges = ranges,
                Options = GetModeOptions(modeNav),
                DefaultValue = defaultValue
            };

            modes.Add(mode);
        }

        return modes;
    }
}