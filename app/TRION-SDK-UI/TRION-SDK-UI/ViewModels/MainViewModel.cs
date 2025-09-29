using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Trion;
using TRION_SDK_UI.Models;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<DigitalMeter> DigitalMeters { get; } = new();
    public ObservableCollection<Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    public ICommand StartAcquisitionCommand { get; private set; }
    public ICommand StopAcquisitionCommand { get; private set; }
    public ICommand LockScrollingCommand { get; private set; }
    public ICommand ToggleThemeCommand { get; private set; }
    public ICommand ShowChannelPropertiesCommand { get; private set; }
    public ICommand CopyChannelPathCommand { get; private set; }
    public ICommand SelectOnlyChannelCommand { get; private set; }
    public ICommand SelectAllOnBoardCommand { get; private set; }
    public ICommand DeselectAllOnBoardCommand { get; private set; }

    private readonly AcquisitionManager _acquisitionManager;
    private bool _isScrollingLocked = true;

    private bool _followLatest = true;
    public bool FollowLatest
    {
        get => _followLatest;
        private set
        {
            if (_followLatest != value)
            {
                _followLatest = value;
                OnPropertyChanged();
            }
        }
    }

    private double _yAxisMin = -10;
    private double _yAxisMax = 10;

    // events for the View
    public event EventHandler<IReadOnlyList<Channel>>? AcquisitionStarting;
    public event EventHandler<SamplesAppendedEventArgs>? SamplesAppended;

    // EventArgs
    public sealed class SamplesAppendedEventArgs(string channelKey, int count) : EventArgs
    {
        public string ChannelKey { get; } = channelKey;
        public int Count { get; } = count;
    }

    public double YAxisMax
    {
        get => _yAxisMax;
        set
        {
            if (_yAxisMax == value)
            {
                return;
            }
            _yAxisMax = value;
            if (_yAxisMax <= _yAxisMin)
            {      
                _yAxisMax = _yAxisMin + 1;
                LogMessages.Add("Invalid Y limits. Max adjusted above Min.");
                _ = ShowAlertAsync("Invalid Y limits", "Y Max must be greater than Y Min.");
            }      
            OnPropertyChanged();
        }
    }
    public double YAxisMin
    {
        get => _yAxisMin;
        set
        {
            if (_yAxisMin == value)
            {
                return;
            }
            _yAxisMin = value;
            if (_yAxisMin >= _yAxisMax)
            {
                _yAxisMin = _yAxisMax - 1;
                LogMessages.Add("Invalid Y limits. Min adjusted below Max.");
                _ = ShowAlertAsync("Invalid Y limits", "Y Min must be less than Y Max.");
            }
            OnPropertyChanged();
        }
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);
        TrionApi.Uninitialize();
    }

    public Enclosure MyEnc { get; } = new Enclosure
    {
        Name = "MyEnc",
        Boards = []
    };

    // plotting buffer (per channel), unlimited history when capacity == 0
    private sealed class ChannelSeriesBuffer
    {
        // All samples for this channel in chronological order
        public List<double> Samples { get; } = new(capacity: 8192);

        // Global index of Samples[0] (advances when oldest samples are trimmed)
        public long FirstSampleIndex { get; private set; } = 0;

        // Global index to assign to the next appended sample
        public long NextSampleIndex { get; private set; } = 0;

        // 0 = unlimited; if > 0 keep at most this many newest samples
        public int MaxSamples { get; }

        public ChannelSeriesBuffer(int maxSamples) => MaxSamples = maxSamples;

        public void Append(IEnumerable<double> samples)
        {
            foreach (var v in samples)
            {
                Samples.Add(v);
                NextSampleIndex++;

                if (MaxSamples > 0 && Samples.Count > MaxSamples)
                {
                    int remove = Samples.Count - MaxSamples;
                    Samples.RemoveRange(0, remove);
                    FirstSampleIndex += remove;
                }
            }
        }

        // Return full series as X (indices) and Y (values)
        public (double[] X, double[] Y) GetSeries()
        {
            int count = Samples.Count;
            if (count == 0) return (Array.Empty<double>(), Array.Empty<double>());

            var ys = Samples.ToArray();
            var xs = new double[count];
            double x = FirstSampleIndex;
            for (int i = 0; i < count; i++, x++)
                xs[i] = x;

            return (xs, ys);
        }
    }

    private readonly Dictionary<string, ChannelSeriesBuffer> _buffersByChannel = new();
    private const int SampleHistoryCapacity = 5_000; // 0 = unlimited history; set >0 to cap memory

    public MainViewModel()
    {
        LogMessages.Add("App started.");

        var numberOfBoards = TrionApi.Initialize();
        if (numberOfBoards < 0)
        {
            LogMessages.Add($"Number of simulated Boards found: {Math.Abs(numberOfBoards)}");
        }
        else if (numberOfBoards > 0)
        {
            LogMessages.Add($"Number of real Boards found: {numberOfBoards}");
        }
        else
        {
            LogMessages.Add("No Trion Boards found.");
            _ = ShowAlertAsync("No TRION boards", "No TRION boards were detected. Configure a system and try again.");
            return;
        }

        numberOfBoards = Math.Abs(numberOfBoards);

        MyEnc.Init(numberOfBoards);
        OnPropertyChanged(nameof(MyEnc));

        foreach (var board in MyEnc.Boards)
        {
            LogMessages.Add($"Board: {board.Name} (ID: {board.Id})");

            foreach (var channel in board.BoardProperties.GetChannels())
            {
                if (channel.Type is Channel.ChannelType.Analog or Channel.ChannelType.Digital)
                {
                    Channels.Add(channel);
                }
            }
        }

        _acquisitionManager = new AcquisitionManager(MyEnc);
        OnPropertyChanged(nameof(Channels));

        StartAcquisitionCommand = new Command(async () => await StartAcquisition());
        StopAcquisitionCommand = new Command(async () => await StopAcquisition());
        LockScrollingCommand = new Command(LockScrolling);
        ToggleThemeCommand = new Command(ToggleTheme);
        ShowChannelPropertiesCommand = new Command<Channel>(async ch => await ShowChannelPropertiesAsync(ch));
        CopyChannelPathCommand = new Command<Channel>(async ch => await CopyChannelPathAsync(ch));
        SelectOnlyChannelCommand = new Command<Channel>(SelectOnlyChannel);
        SelectAllOnBoardCommand = new Command<Channel>(SelectAllOnBoard);
        DeselectAllOnBoardCommand = new Command<Channel>(DeselectAllOnBoard);
    }

    private async Task StartAcquisition()
    {
        LogMessages.Add("Starting acquisition...");

        var selectedChannels = Channels.Where(c => c.IsSelected).ToList();
        if (selectedChannels.Count == 0)
        {
            LogMessages.Add("No channels selected. Please select at least one channel.");
            await ShowAlertAsync("No channels selected",
                "Please select at least one channel and try again.");
            return;
        }

        PrepareUIForAcquisition(selectedChannels);

        // notify view early so it can clear plottables
        AcquisitionStarting?.Invoke(this, selectedChannels);

        await _acquisitionManager.StartAcquisitionAsync(selectedChannels, OnSamplesReceived);
    }

    private void PrepareUIForAcquisition(List<Channel> selectedChannels)
    {
        DigitalMeters.Clear();

        foreach (var channel in selectedChannels)
        {
            var meter = new DigitalMeter
            {
                Label = $"{channel.BoardID}/{channel.Name}",
                Value = channel.CurrentValue,
                Unit = channel.Unit
            };
            DigitalMeters.Add(meter);
        }

        // clear plot buffers for selected channels
        foreach (var ch in selectedChannels)
        {
            var key = $"{ch.BoardID}/{ch.Name}";
            _buffersByChannel[key] = new ChannelSeriesBuffer(SampleHistoryCapacity);
        }

        OnPropertyChanged(nameof(DigitalMeters));
    }

    private async Task StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
        await _acquisitionManager.StopAcquisitionAsync();
    }

    private void LockScrolling()
    {
        _isScrollingLocked = !_isScrollingLocked;
        FollowLatest = _isScrollingLocked;
        LogMessages.Add(_isScrollingLocked ? "Scrolling locked." : "Scrolling unlocked.");
    }

    private void ToggleTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            LogMessages.Add($"Theme changed to {Application.Current.UserAppTheme}.");
        }
    }

    private void OnSamplesReceived(string channelName, IEnumerable<double> samples)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var latestValue = samples.LastOrDefault();

            // update meter value
            var parts = channelName.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[0], out var boardId))
            {
                var chName = parts[1];
                var channel = Channels.FirstOrDefault(c => c.BoardID == boardId && c.Name == chName);
                if (channel is not null)
                {
                    channel.CurrentValue = latestValue;
                    var meter = DigitalMeters.FirstOrDefault(m => m.Label == channelName);
                    meter?.AddSample(latestValue);
                }
            }

            // plot buffer (full history)
            if (!_buffersByChannel.TryGetValue(channelName, out var buf))
            {
                buf = new ChannelSeriesBuffer(SampleHistoryCapacity);
                _buffersByChannel[channelName] = buf;
            }
            buf.Append(samples);

            SamplesAppended?.Invoke(this, new SamplesAppendedEventArgs(channelName, samples.Count()));
        });
    }

    // ScottPlot: full history accessor
    public (IReadOnlyList<double> X, IReadOnlyList<double> Y) GetFullSeries(string channelKey)
    {
        if (_buffersByChannel.TryGetValue(channelKey, out var buf))
        {
            var (x, y) = buf.GetSeries();
            return (x, y);
        }
        return (Array.Empty<double>(), Array.Empty<double>());
    }


    private async Task ShowChannelPropertiesAsync(Channel? ch)
    {
        if (ch is null) return;
        string target = $"BoardID{ch.BoardID}/{ch.Name}";
        var props = new List<(string Key, string? Val)>();

        // Try common keys; failures are silently ignored
        foreach (var key in GetKeysForChannelType(ch.Type))
        {
            var (ok, val) = TryGetParam(target, key);
            if (ok && !string.IsNullOrWhiteSpace(val))
                props.Add((key, val));
        }

        if (props.Count == 0)
        {
            await ShowAlertAsync("Channel Properties", $"No readable properties for {target}.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Channel: {target}");
        sb.AppendLine($"Type: {ch.Type}");
        foreach (var (k, v) in props)
            sb.AppendLine($"{k}: {v}");

        LogMessages.Add($"Shown properties for {target}");
        await ShowAlertAsync("Channel Properties", sb.ToString());
    }

    private static IEnumerable<string> GetKeysForChannelType(Channel.ChannelType type)
    {
        // Safe, read‑only keys; adjust as needed for your hardware
        if (type == Channel.ChannelType.Analog)
        {
            return
            [
                "Used", "Mode", "Range", "Excitation", "InputType",
                "LPFilter_Val", "HPFilter_Val", "BridgeRes", "ShuntType", "ShuntResistance"
            ];
        }
        if (type == Channel.ChannelType.Digital)
        {
            return
            [
                "Used", "Mode"
            ];
        }
        return ["Used", "Mode"];
    }

    private static (bool ok, string value) TryGetParam(string target, string key)
    {
        var (err, val) = TrionApi.DeWeGetParamStruct_String(target, key);
        return (err == TrionError.NONE, val);
    }

    private async Task CopyChannelPathAsync(Channel? ch)
    {
        if (ch is null) return;
        string channelPath = $"BoardID{ch.BoardID}/{ch.Name}";
        await Clipboard.SetTextAsync(channelPath);
        LogMessages.Add($"Copied: {channelPath}");
    }

    private void SelectOnlyChannel(Channel? ch)
    {
        if (ch is null) return;
        foreach (var c in Channels)
        {
            c.IsSelected = false;
        }
        ch.IsSelected = true;
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Selected only {ch.BoardID}/{ch.Name}");
    }

    private void SelectAllOnBoard(Channel? ch)
    {
        if (ch is null) return;
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID))
        {
            c.IsSelected = true;
        }
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Selected all channels on Board {ch.BoardID}");
    }

    private void DeselectAllOnBoard(Channel? ch)
    {
        if (ch is null) return;
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID))
        {
            c.IsSelected = false;
        }
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Deselected all channels on Board {ch.BoardID}");
    }
}

public readonly record struct ChannelId(int BoardId, string Name)
{
    public override string ToString() => $"{BoardId}/{Name}";
}