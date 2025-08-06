using System.Xml.XPath;

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
            string channel = channelNav.Name;
            channelNames.Add(channel);
        }

        return channelNames;
    }
}
