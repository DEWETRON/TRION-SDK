using System.Collections.ObjectModel;


public class Board
{
    public string Name { get; set; }
    public bool IsActive { get; set; }

    public BoardPropertyModel BoardProperties { get; set; }
}

public class Enclosure
{
    public string Name { get; set; }
    public ObservableCollection<Board> Boards { get; set; } = new();
}
