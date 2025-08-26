using System.Collections.ObjectModel;

public class ChartRecorder
{
    // Store all data per channel
    private readonly Dictionary<string, List<double>> _data = [];
    // Store windowed data per channel for UI binding
    private readonly Dictionary<string, ObservableCollection<double>> _windows = [];
    private int _windowSize = 800;
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

    // Get the window for a specific channel (for binding)
    public ObservableCollection<double> GetWindow(string channel)
    {
        if (!_windows.ContainsKey(channel))
        {
            _windows[channel] = [];
            //System.Diagnostics.Debug.WriteLine($"Created new window for channel: {channel} (HashCode: {_windows[channel].GetHashCode()})");
        }
        else
        {
            //System.Diagnostics.Debug.WriteLine($"Returning existing window for channel: {channel} (HashCode: {_windows[channel].GetHashCode()})");
        }
        return _windows[channel];
    }

    // Add samples to a specific channel
    public void AddSamples(string channel, IEnumerable<double> samples)
    {
        System.Diagnostics.Debug.WriteLine($"AddSamples called for channel: {channel}, sample count: {samples.Count()}");
        if (!_data.ContainsKey(channel))
        {
            _data[channel] = [];
        }
        _data[channel].AddRange(samples);
        UpdateWindow(channel);
    }

    // Update the window for a specific channel
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

    // Update all windows (e.g., when window size or scroll index changes)
    public void UpdateAllWindows()
    {
        foreach (var channel in _data.Keys)
        {
            UpdateWindow(channel);
        }
    }

    // Auto-scroll all channels
    public void AutoScroll()
    {
        ScrollIndex = MaxScrollIndex;
        UpdateAllWindows();
    }

    // Get max scroll index for a specific channel
    public int MaxScrollIndex => _data.Values.Select(d => Math.Max(0, d.Count - WindowSize)).DefaultIfEmpty(0).Max();
}