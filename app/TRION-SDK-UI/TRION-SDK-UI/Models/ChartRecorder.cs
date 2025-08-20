using System.Collections.ObjectModel;

public class ChartRecorder
{
    public List<double> Data { get; } = [];
    public ObservableCollection<double> Window { get; } = [];

    private int _windowSize = 800;
    public int WindowSize
    {
        get => _windowSize;
        set
        {
            if (_windowSize != value)
            {
                _windowSize = value;
                UpdateWindow();
            }
        }
    }

    private int _scrollIndex;
    public int ScrollIndex
    {
        get => _scrollIndex;
        set
        {
            if (_scrollIndex != value)
            {
                _scrollIndex = value;
                UpdateWindow();
            }
        }
    }

    public int MaxScrollIndex => Math.Max(0, Data.Count - WindowSize);

    public void AddSamples(IEnumerable<double> samples)
    {
        Data.AddRange(samples);
        UpdateWindow();
    }

    public void UpdateWindow()
    {
        Window.Clear();
        foreach (var v in Data.Skip(ScrollIndex).Take(WindowSize))
            Window.Add(v);
    }

    public void AutoScroll()
    {
        ScrollIndex = MaxScrollIndex;
        UpdateWindow();
    }
}