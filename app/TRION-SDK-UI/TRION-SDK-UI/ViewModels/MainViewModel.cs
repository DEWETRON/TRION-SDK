using System.Collections.ObjectModel;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;

/// <summary>
/// Main ViewModel for the .NET MAUI UI.
/// Initializes TRION API, discovers channels, starts/stops acquisition,
/// updates meters/logs, and notifies the View when new samples arrive.
/// </summary>
public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<DigitalMeter> DigitalMeters { get; } = [];
    public ObservableCollection<Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

    // Commands (nullable to satisfy CS8618 at construction time)
    public ICommand? StartAcquisitionCommand { get; private set; }
    public ICommand? StopAcquisitionCommand { get; private set; }
    public ICommand? LockScrollingCommand { get; private set; }
    public ICommand? ToggleThemeCommand { get; private set; }
    public ICommand? ShowChannelPropertiesCommand { get; private set; }
    public ICommand? CopyChannelPathCommand { get; private set; }
    public ICommand? SelectOnlyChannelCommand { get; private set; }
    public ICommand? SelectAllOnBoardCommand { get; private set; }
    public ICommand? DeselectAllOnBoardCommand { get; private set; }

    private readonly AcquisitionManager? _acquisitionManager;

    private bool _isScrollingLocked = true;
    private bool _followLatest = true;
    public bool FollowLatest
    {
        get => _followLatest;
        private set { if (_followLatest != value) { _followLatest = value; OnPropertyChanged(); } }
    }

    private double _yAxisMin = -10;
    private double _yAxisMax = 10;

    public event EventHandler<IReadOnlyList<Channel>>? AcquisitionStarting;

    // EVENT NOW CARRIES THE NEWEST BATCH
    public event EventHandler<SamplesAppendedEventArgs>? SamplesAppended;

    /// <summary>Event args carrying the channel key and newest batch of samples.</summary>
    public sealed class SamplesAppendedEventArgs : EventArgs
    {
        public string ChannelKey { get; }
        public ReadOnlyMemory<double> Samples { get; }
        public int Count => Samples.Length;
        public SamplesAppendedEventArgs(string channelKey, ReadOnlyMemory<double> samples)
        {
            ChannelKey = channelKey;
            Samples = samples;
        }
    }

    public double YAxisMax
    {
        get => _yAxisMax;
        set
        {
            if (_yAxisMax == value) return;
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
            if (_yAxisMin == value) return;
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

    public Enclosure MyEnc { get; } = new Enclosure { Name = "MyEnc", Boards = [] };

    public MainViewModel()
    {
        LogMessages.Add("App started.");

        var numberOfBoards = TrionApi.Initialize();
        if (numberOfBoards < 0)
            LogMessages.Add($"Number of simulated Boards found: {Math.Abs(numberOfBoards)}");
        else if (numberOfBoards > 0)
            LogMessages.Add($"Number of real Boards found: {numberOfBoards}");
        else
        {
            LogMessages.Add("No Trion Boards found.");
            _ = ShowAlertAsync("No TRION boards", "No TRION boards were detected. Configure a system and try again.");
            return;
        }

        numberOfBoards = Math.Abs(numberOfBoards);
        MyEnc.Init(numberOfBoards);
        OnPropertyChanged(nameof(MyEnc));

        // Discover channels
        foreach (var board in MyEnc.Boards)
        {
            LogMessages.Add($"Board: {board.Name} (ID: {board.Id})");
            foreach (var channel in board.BoardProperties.GetChannels())
            {
                if (channel.Type is Channel.ChannelType.Analog or Channel.ChannelType.Digital)
                    Channels.Add(channel);
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
            await ShowAlertAsync("No channels selected", "Please select at least one channel and try again.");
            return;
        }

        PrepareUIForAcquisition(selectedChannels);
        AcquisitionStarting?.Invoke(this, selectedChannels);

        await _acquisitionManager!.StartAcquisitionAsync(selectedChannels, OnSamplesReceived);
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

        OnPropertyChanged(nameof(DigitalMeters));
    }

    private async Task StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
        await _acquisitionManager!.StopAcquisitionAsync();
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

    /// <summary>
    /// Acquisition callback: update meters and raise event with the newest batch.
    /// </summary>
    private void OnSamplesReceived(string channelName, IEnumerable<double> samples)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var batch = samples as double[] ?? [.. samples];
            var latestValue = batch.Length > 0 ? batch[^1] : 0;

            // Update per-channel latest value and the DigitalMeter
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

            // Notify the View with this batch for plotting
            SamplesAppended?.Invoke(this, new SamplesAppendedEventArgs(channelName, batch));
        });
    }

    private async Task ShowChannelPropertiesAsync(Channel? ch)
    {
        if (ch is null) return;

        string target = $"BoardID{ch.BoardID}/{ch.Name}";
        var props = new List<(string Key, string? Val)>();

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
            return ["Used", "Mode"];
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
        foreach (var c in Channels) c.IsSelected = false;
        ch.IsSelected = true;
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Selected only {ch.BoardID}/{ch.Name}");
    }

    private void SelectAllOnBoard(Channel? ch)
    {
        if (ch is null) return;
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID)) c.IsSelected = true;
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Selected all channels on Board {ch.BoardID}");
    }

    private void DeselectAllOnBoard(Channel? ch)
    {
        if (ch is null) return;
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID)) c.IsSelected = false;
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Deselected all channels on Board {ch.BoardID}");
    }
}

/// <summary>Lightweight identifier for a channel used in keys, logs, and bindings.</summary>
public readonly record struct ChannelId(int BoardId, string Name)
{
    public override string ToString() => $"{BoardId}/{Name}";
}