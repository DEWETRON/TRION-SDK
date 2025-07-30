using System.Xml.XPath;

public class BoardPropertyModel
{
    private XPathDocument _document;
    private XPathNavigator _navigator;

    public BoardPropertyModel(string board_xml)
    {
        using var stringReader = new StringReader(board_xml);
        _document = new XPathDocument(stringReader);
        _navigator = _document.CreateNavigator();
    }


    public List<string> getChannelNames()
    {
        var channel_names = new List<string>();
        var iterator = _navigator.Select("Properties/ChannelProperties/*");

        while (iterator.MoveNext())
        {
            var channelNav = iterator.Current;
            string channel = channelNav.Name;
            channel_names.Add(channel);
        }

        return channel_names;
    }
}
