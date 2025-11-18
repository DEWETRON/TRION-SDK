using System.Diagnostics;
using System.Xml.XPath;
using TRION_SDK_UI.POCO;
using static TRION_SDK_UI.Models.Channel;

namespace TRION_SDK_UI.Models;

public sealed class BoardPropertyModel
{
    private readonly XPathNavigator _navigator;
    public string BoardName => GetBoardName();
    public AcqProp AcqProp => GetAcqProp();

    public BoardPropertyModel(string boardXml)
    {
        ArgumentNullException.ThrowIfNull(boardXml);

        using var sr = new StringReader(boardXml);
        var doc = new XPathDocument(sr);
        _navigator = doc.CreateNavigator();
    }

    private static ChannelType GetChannelType(string name) =>
        name.StartsWith("AI", StringComparison.OrdinalIgnoreCase) ? ChannelType.Analog :
        name.StartsWith("Di", StringComparison.OrdinalIgnoreCase) ? ChannelType.Digital :
        name.StartsWith("CNT", StringComparison.OrdinalIgnoreCase) ? ChannelType.Counter :
        ChannelType.Unknown;

    private static ChannelMode CreatePlaceholderMode() => new()
    {
        Name = "Unknown",
        Unit = string.Empty,
        Ranges = [],
        Options = [],
        DefaultValue = string.Empty
    };

    public static (bool ok, ChannelMode mode) TryGetDefaultMode(XPathNavigator channelNav)
    {
        if (channelNav is null || channelNav.NodeType != XPathNodeType.Element)
        {
            return (false, CreatePlaceholderMode());
        }

        var modes = GetChannelModes(channelNav);
        if (modes.Count == 0)
        {
            return (false, CreatePlaceholderMode());
        }

        var defaultName = channelNav.GetAttribute("Default", "");
        if (!string.IsNullOrWhiteSpace(defaultName))
        {
            var found = modes.FirstOrDefault(m => string.Equals(m.Name, defaultName, StringComparison.OrdinalIgnoreCase));
            if (found is not null)
            {
                return (true, found);
            }
        }

        return (true, modes[0]);
    }

    public List<Channel> GetChannels()
    {
        var channels = new List<Channel>(capacity: 64);

        var iterator = _navigator.Select("/Properties/ChannelProperties/*");
        while (iterator.MoveNext())
        {
            var channelNav = iterator.Current;
            if (channelNav is null || channelNav.NodeType != XPathNodeType.Element)
            {
                continue;
            }

            var allModes = GetChannelModes(channelNav);
            if (allModes.Count == 0)
            { 
                continue;
            }

            var (ok, defaultMode) = TryGetDefaultMode(channelNav);
            if (!ok) continue;

            var rangeIndexStr = defaultMode.DefaultValue ?? string.Empty;
            int idx = int.TryParse(rangeIndexStr, out var parsed) ? parsed : 0;
            string defaultRange =
                (idx >= 0 && idx < defaultMode.Ranges.Count)
                ? defaultMode.Ranges[idx]
                : (defaultMode.Ranges.FirstOrDefault() ?? string.Empty);

            channels.Add(new Channel
            {
                BoardID = GetBoardID(),
                BoardName = GetBoardName(),
                Name = channelNav.Name,
                Type = GetChannelType(channelNav.Name),
                ModeList = allModes,
                Mode = defaultMode,
                Unit = defaultMode.Unit ?? string.Empty,
                Range = defaultRange
            });
        }

        return channels;
    }

    private string GetBoardName()
    {
        var boardNameNode = _navigator.SelectSingleNode("/Properties/BoardInfo/BoardName");
        return boardNameNode?.Value ?? string.Empty;
    }

    private int GetBoardID()
    {
        var propertiesNode = _navigator.SelectSingleNode("/Properties");
        if (propertiesNode == null) return -1;
        var idStr = propertiesNode.GetAttribute("BoardID", "");
        return int.TryParse(idStr, out var id) ? id : -1;
    }

    public AcqProp GetAcqProp()
    {
        var acqPropNav = _navigator.SelectSingleNode("/Properties/AcquisitionProperties/AcqProp");
        if (acqPropNav == null)
            return new AcqProp();
        return new AcqProp
        {
            SampleRateProp = GetSampleRateProp(),
        };
    }

    public SampleRateProp GetSampleRateProp()
    {
        var sampleRateNav = _navigator.SelectSingleNode("/Properties/AcquisitionProperties/AcqProp/SampleRate");

        if (sampleRateNav == null)
            return new SampleRateProp();

        return new SampleRateProp
        {
            Unit = sampleRateNav.GetAttribute("Unit", ""),
            Count = sampleRateNav.GetAttribute("Count", ""),
            Default = int.TryParse(sampleRateNav.GetAttribute("Default", ""), out var def) ? def : 0,
            Programmable = bool.TryParse(sampleRateNav.GetAttribute("Programmable", ""), out var prog) && prog,
            ProgMax = int.TryParse(sampleRateNav.GetAttribute("ProgMax", ""), out var progMax) ? progMax : 0,
            ProgMin = int.TryParse(sampleRateNav.GetAttribute("ProgMin", ""), out var progMin) ? progMin : 0,
            ProgRes = sampleRateNav.GetAttribute("ProgRes", ""),
            AvailableRates = [.. sampleRateNav
                .SelectChildren(XPathNodeType.Element)
                .Cast<XPathNavigator>()
                .Select(v => v.Value?.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)]
        };
    }

    private static List<ModeOption> GetModeOptions(XPathNavigator modeNav)
    {
        var list = new List<ModeOption>();
        var iterator = modeNav.SelectChildren(XPathNodeType.Element);
        while (iterator.MoveNext())
        {
            var optionNav = iterator.Current;
            if (optionNav is null) continue;

            var values = optionNav
                .SelectChildren(XPathNodeType.Element)
                .Cast<XPathNavigator>()
                .Select(v => v.Value?.Trim())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .ToList();

            list.Add(new ModeOption
            {
                Name = optionNav.Name,
                Default = double.TryParse(optionNav.GetAttribute("Default", ""), out var def) ? def : 0,
                Unit = optionNav.GetAttribute("Unit", ""),
                Programmable = optionNav.GetAttribute("Programmable", ""),
                ProgMax = double.TryParse(optionNav.GetAttribute("ProgMax", ""), out var programmingMax) ? programmingMax : 0,
                ProgMin = double.TryParse(optionNav.GetAttribute("ProgMin", ""), out var programmingMin) ? programmingMin : 0,
                ProgRes = double.TryParse(optionNav.GetAttribute("ProgRes", ""), out var ProgrammingRes) ? ProgrammingRes : 0,
                Values = values
            });
        }
        return list;
    }

    public static List<ChannelMode> GetChannelModes(XPathNavigator channelNav)
    {
        var modes = new List<ChannelMode>();
        if (channelNav is null || channelNav.NodeType != XPathNodeType.Element)
        {
            return modes;
        }

        var iterator = channelNav.SelectChildren("Mode", "");
        while (iterator.MoveNext())
        {
            var modeNav = iterator.Current;
            if (modeNav is null) continue;

            var rangeNav = modeNav.SelectSingleNode("Range");

            var name = modeNav.GetAttribute("Mode", "");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = modeNav.GetAttribute("Name", "");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                name = modeNav.Name;
            }

            var unit = rangeNav?.GetAttribute("Unit", "");
            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = modeNav.GetAttribute("Unit", "");
            }
            unit ??= string.Empty;

            var ranges = rangeNav?
                .SelectChildren(XPathNodeType.Element)
                .Cast<XPathNavigator>()
                .Where(n => n.Name.StartsWith("ID", StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Value?.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToList() ?? [];

            var defaultValue = rangeNav?.GetAttribute("Default", "") ?? string.Empty;

            modes.Add(new ChannelMode
            {
                Name = name,
                Unit = unit,
                Ranges = ranges,
                Options = GetModeOptions(modeNav),
                DefaultValue = defaultValue
            });
        }

        return modes;
    }
}