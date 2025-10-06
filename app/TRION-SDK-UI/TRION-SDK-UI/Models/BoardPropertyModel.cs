using System.Xml.XPath;
using TRION_SDK_UI.Models;
using static TRION_SDK_UI.Models.Channel;

/// <summary>
/// Parses a TRION board XML description (BoardProperties XML) and exposes
/// strongly-typed accessors for board meta-data, channel definitions, and
/// per-channel mode/range information.
/// </summary>
/// <remarks>
/// The constructor accepts the full XML string delivered by the TRION API
/// (typically something like DeWeGetParamStruct_String("BoardIDX", "BoardProperties")).
/// Expected (simplified) XML shape:
/// <![CDATA[
/// <Properties BoardID="0">
///   <BoardInfo>
///     <BoardName>TRION-XXXX</BoardName>
///   </BoardInfo>
///   <ChannelProperties>
///     <AI0>
///       <Mode Mode="Voltage">
///         <Range Unit="V">
///           <ID0>-10</ID0>
///           <ID1>10</ID1>
///         </Range>
///       </Mode>
///       <Mode Mode="IEPE">
///         <Range Unit="V">
///           <ID0>-5</ID0>
///           <ID1>5</ID1>
///         </Range>
///       </Mode>
///     </AI0>
///     <Di0>
///       <Mode Mode="Digital">
///         <Range Unit="None" />
///       </Mode>
///     </Di0>
///   </ChannelProperties>
/// </Properties>
/// ]]>
/// The parser is resilient to missing nodes and returns defaults (empty lists / strings / -1).
/// </remarks>
public class BoardPropertyModel
{
    private readonly XPathDocument _document;
    private readonly XPathNavigator _navigator;

    /// <summary>
    /// Loads the board properties XML into an XPath document for repeated queries.
    /// </summary>
    /// <param name="boardXML">Raw XML describing board and channel capabilities.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="boardXML"/> is null.</exception>
    public BoardPropertyModel(string boardXML)
    {
        ArgumentNullException.ThrowIfNull(boardXML);

        using var stringReader = new StringReader(boardXML);
        _document = new XPathDocument(stringReader);
        _navigator = _document.CreateNavigator();
    }

    /// <summary>
    /// Returns all channel element names found under /Properties/ChannelProperties.
    /// </summary>
    /// <returns>List of channel identifiers (e.g. AI0, AI1, Di0).</returns>
    public List<string> GetChannelNames()
    {
        var channelNames = new List<string>();
        var iterator = _navigator.Select("Properties/ChannelProperties/*");

        while (iterator.MoveNext())
        {
            var channelNav = iterator.Current;
            if (channelNav != null)
            {
                channelNames.Add(channelNav.Name);
            }
        }

        return channelNames;
    }

    /// <summary>
    /// Infers a channel type from its XML element name prefix.
    /// </summary>
    /// <param name="name">Channel element name (e.g. AI0, Di3).</param>
    /// <returns>Matching <see cref="Channel.ChannelType"/> or Unknown.</returns>
    private static ChannelType GetChannelType(string name)
    {
        if (name.StartsWith("AI")) return ChannelType.Analog;
        if (name.StartsWith("Di")) return ChannelType.Digital;
        return ChannelType.Unknown;
    }

    /// <summary>
    /// Builds a list of <see cref="Channel"/> objects from the XML definition,
    /// populating basic metadata and available modes (with ranges).
    /// </summary>
    /// <remarks>
    /// BoardID and BoardName are re-queried for each channel; if performance
    /// becomes critical, capture them before the loop.
    /// </remarks>
    /// <returns>List of channel descriptors.</returns>
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

    /// <summary>
    /// Retrieves the board name from /Properties/BoardInfo/BoardName.
    /// </summary>
    /// <returns>Board name, or empty string if not found.</returns>
    public string GetBoardName()
    {
        var boardName = _navigator.SelectSingleNode("/Properties/BoardInfo/BoardName");
        return boardName != null ? boardName.Value : string.Empty;
    }

    /// <summary>
    /// Retrieves the numeric board ID from /Properties/@BoardID.
    /// </summary>
    /// <returns>Board ID if parsable; -1 otherwise.</returns>
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

    /// <summary>
    /// Parses all Mode elements inside a channel node and converts them into
    /// <see cref="ChannelMode"/> instances including their numeric ranges.
    /// </summary>
    /// <param name="channelNav">Navigator positioned at a channel element.</param>
    /// <returns>List of channel modes (possibly empty).</returns>
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

    /// <summary>
    /// Converts raw unit text to the strongly-typed <see cref="ChannelMode.UnitEnum"/>.
    /// Returns None for unknown or missing values.
    /// </summary>
    /// <param name="unit">Raw unit string (e.g. "V").</param>
    /// <returns>Mapped <see cref="ChannelMode.UnitEnum"/>.</returns>
    private static ChannelMode.UnitEnum ParseUnit(string? unit)
    {
        return unit switch
        {
            "V" => ChannelMode.UnitEnum.Voltage,
            "mA" => ChannelMode.UnitEnum.MiliAmperes,
            "Hz" => ChannelMode.UnitEnum.Hertz,
            "Ohm" => ChannelMode.UnitEnum.Ohm,
            _ => ChannelMode.UnitEnum.None
        };
    }
}