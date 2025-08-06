using System.Collections.ObjectModel;
using TRION_SDK_UI.Models;


public class Enclosure
{
    public string Name { get; set; }
    public ObservableCollection<Board> Boards { get; set; } = new();
}
