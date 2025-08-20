using System.Reflection.Metadata;
using System.Xml.XPath;
using TRION_SDK_UI.Models;

public class BoardPropertyModel
{
    private XPathDocument _document;
    private XPathNavigator _navigator;

    public BoardPropertyModel(string boardXML)
    {
        using var stringReader = new StringReader(boardXML);
        _document = new XPathDocument(stringReader);
        _navigator = _document.CreateNavigator();
    }

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
                    Name = channelNav.Name
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
                return id;
        }
        return -1;
    }
}
