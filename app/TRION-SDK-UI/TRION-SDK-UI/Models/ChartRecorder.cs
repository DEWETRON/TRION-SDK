using System.Collections.ObjectModel;

public class ChartRecorder
{
    private readonly Dictionary<string, List<double>> _data = [];
    private readonly Dictionary<string, ObservableCollection<double>> _windows = [];
    private int _windowSize = 600;
    public int WindowSize
    {
        get => _windowSize;
        set
        {
            if (_windowSize != value)
            {
                _windowSize = value;
                UpdateAllWindows();
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
                UpdateAllWindows();
            }
        }
    }

    // NEW: cap the number of samples kept per channel
    public int MaxHistorySamples { get; set; } = 200_000;

    public ObservableCollection<double> GetWindow(string channel)
    {
        if (!_windows.ContainsKey(channel))
        {
            _windows[channel] = [];
        }
        return _windows[channel];
    }

    public void AddSamples(string channel, IEnumerable<double> samples)
    {
        if (!_data.TryGetValue(channel, out List<double>? value))
        {
            value = [];
            _data[channel] = value;
        }

        value.AddRange(samples);

        // NEW: trim oldest data to avoid unbounded growth
        if (value.Count > MaxHistorySamples)
        {
            int remove = value.Count - MaxHistorySamples;
            value.RemoveRange(0, remove);
            // adjust scroll so window stays aligned with trimmed data
            if (ScrollIndex > 0)
                ScrollIndex = Math.Max(0, ScrollIndex - remove);
        }

        UpdateWindow(channel);
    }

    private void UpdateWindow(string channel)
    {
        var data = _data[channel];
        var window = GetWindow(channel);
        window.Clear();
        foreach (var v in data.Skip(ScrollIndex).Take(WindowSize))
        {
            window.Add(v);
        }
    }

    public void UpdateAllWindows()
    {
        foreach (var channel in _data.Keys)
        {
            UpdateWindow(channel);
        }
    }

    public void AutoScroll()
    {
        ScrollIndex = MaxScrollIndex;
        UpdateAllWindows();
    }

    public int MaxScrollIndex => _data.Values.Select(d => Math.Max(0, d.Count - WindowSize)).DefaultIfEmpty(0).Max();
}