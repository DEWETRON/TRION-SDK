using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;
using System.ComponentModel;

public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<DigitalMeter> DigitalMeters { get; } = [];
    public ObservableCollection<Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];

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

    private const int MaxSelectableChannels = 8;
    private bool _suppressSelectionGuard = false;

    public event EventHandler<IReadOnlyList<Channel>>? AcquisitionStarting;

    public event EventHandler<SamplesBatchAppendedEventArgs>? SamplesBatchAppended;

    public sealed class SamplesBatchAppendedEventArgs(IReadOnlyDictionary<string, Sample[]> batches) : EventArgs
    {
        public IReadOnlyDictionary<string, Sample[]> Batches { get; } = batches;
    }

    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);
        TrionApi.Uninitialize();
    }

    public Enclosure MyEnc { get; } = new Enclosure { Name = "MyEnc", Boards = [] };

    private readonly Dictionary<string, Channel> _channelByKey = [];
    private readonly Dictionary<string, DigitalMeter> _meterByKey = [];

    public MainViewModel()
    {
        Debug.WriteLine("Started");
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
            foreach (var channel in board.Channels.Where(c => c.Type is Channel.ChannelType.Analog or Channel.ChannelType.Digital))
            {
                Channels.Add(channel);
                channel.PropertyChanged += OnChannelPropertyChanged;
            }
        }

        _acquisitionManager = new AcquisitionManager(MyEnc);
        OnPropertyChanged(nameof(Channels));

        StartAcquisitionCommand      = new Command(async () => await StartAcquisition());
        StopAcquisitionCommand       = new Command(async () => await StopAcquisition());
        LockScrollingCommand         = new Command(LockScrolling);
        ToggleThemeCommand           = new Command(ToggleTheme);
        ShowChannelPropertiesCommand = new Command<Channel>(async ch => await ShowChannelPropertiesAsync(ch));
        CopyChannelPathCommand       = new Command<Channel>(async ch => await CopyChannelPathAsync(ch));
        SelectOnlyChannelCommand     = new Command<Channel>(SelectOnlyChannel);
        SelectAllOnBoardCommand      = new Command<Channel>(SelectAllOnBoard);
        DeselectAllOnBoardCommand    = new Command<Channel>(DeselectAllOnBoard);
    }

    // NEW: guard to revert selection when over limit
    private void OnChannelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSelectionGuard) return;
        if (sender is not Channel ch) return;
        if (e.PropertyName != nameof(Channel.IsSelected)) return;

        if (ch.IsSelected)
        {
            int selected = Channels.Count(c => c.IsSelected);
            if (selected > MaxSelectableChannels)
            {
                // revert this selection
                _suppressSelectionGuard = true;
                ch.IsSelected = false;
                _suppressSelectionGuard = false;
                LogMessages.Add($"You can select up to {MaxSelectableChannels} channels.");
            }
        }
    }

    private async Task StartAcquisition()
    {
        Debug.WriteLine("Starting acquisition...");
        LogMessages.Add("Starting acquisition...");

        var selectedChannels = Channels.Where(c => c.IsSelected).ToList();

        if (selectedChannels.Count > MaxSelectableChannels)
        {
            foreach (var extra in selectedChannels.Skip(MaxSelectableChannels))
                extra.IsSelected = false;

            selectedChannels = selectedChannels.Take(MaxSelectableChannels).ToList();
            LogMessages.Add($"Selection limited to {MaxSelectableChannels} channels.");
        }

        if (selectedChannels.Count == 0)
        {
            LogMessages.Add("No channels selected. Please select at least one channel.");
            await ShowAlertAsync("No channels selected", "Please select at least one channel and try again.");
            return;
        }

        PrepareUIForAcquisition(selectedChannels);
        AcquisitionStarting?.Invoke(this, selectedChannels);

        await _acquisitionManager!.StartAcquisitionAsync(selectedChannels);
        StartUiDrainTimer();
    }

    private void PrepareUIForAcquisition(List<Channel> selectedChannels)
    {
        DigitalMeters.Clear();
        _channelByKey.Clear();
        _meterByKey.Clear();

        foreach (var channel in selectedChannels)
        {
            var key = $"{channel.BoardID}/{channel.Name}";
            _channelByKey[key] = channel;

            var meter = new DigitalMeter
            {
                Label = key,
                Unit = channel.Unit
            };
            _meterByKey[key] = meter;
            DigitalMeters.Add(meter);
        }

        OnPropertyChanged(nameof(DigitalMeters));
    }

    private async Task StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
        StopUiDrainTimer();
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

    // Dispatcher timer
    private IDispatcherTimer? _uiDrainTimer;
    private EventHandler? _drainTickHandler;

    private void StartUiDrainTimer()
    {
        StopUiDrainTimer();
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        _uiDrainTimer = dispatcher.CreateTimer();
        _uiDrainTimer.Interval = TimeSpan.FromMilliseconds(33.3); // ~30 Hz (tune)
        _uiDrainTimer.IsRepeating = true;

        _drainTickHandler = (_, _) => DrainAndPublish();
        _uiDrainTimer.Tick += _drainTickHandler;
        _uiDrainTimer.Start();
    }

    private void StopUiDrainTimer()
    {
        if (_uiDrainTimer is null)
        {
            return;
        }
        if (_drainTickHandler is not null)
        {
            _uiDrainTimer.Tick -= _drainTickHandler;
        }

        _uiDrainTimer.Stop();
        _uiDrainTimer = null;
        _drainTickHandler = null;
    }

    private readonly TimeSpan _meterUpdatePeriod = TimeSpan.FromMilliseconds(33.3); // 30 Hz
    private DateTime _lastMeterUpdateUtc = DateTime.MinValue;

    private void DrainAndPublish()
    {
        // get a batch of samples per channel (up to maxPerChannel)
        var batches = _acquisitionManager!.DrainSamples(maxPerChannel: 1000);
        if (0 == batches.Count)
        {
            return;
        }

        // Single UI block per tick
        var now = DateTime.UtcNow;
        bool updateMeters = (now - _lastMeterUpdateUtc) >= _meterUpdatePeriod;
        if (updateMeters) _lastMeterUpdateUtc = now;

        foreach (var (channelKey, samples) in batches)
        {
            // Update meters less frequently
            if (updateMeters)
            {
                var latestValue = samples.Length > 0 ? samples[^1].Value : 0;
           
                if (_meterByKey.TryGetValue(channelKey, out var meter))
                {
                    meter.AddSample(latestValue);
                }
            }
        }

        SamplesBatchAppended?.Invoke(this, new SamplesBatchAppendedEventArgs(batches));
    }

    private async Task ShowChannelPropertiesAsync(Channel? ch)
    {
        if (ch is null)
        {
            return;
        }

        string target = $"BoardID{ch.BoardID}/{ch.Name}";
        var props = new List<(string Key, string? Val)>();

        foreach (var key in GetKeysForChannelType(ch.Type))
        {
            var (ok, val) = TryGetParam(target, key);
            if (ok && !string.IsNullOrWhiteSpace(val))
            {
                props.Add((key, val));
            }
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
        {
            sb.AppendLine($"{k}: {v}");
        }

        LogMessages.Add($"Shown properties for {target}");
        await ShowAlertAsync("Channel Properties", sb.ToString());
    }
    private static IEnumerable<string> GetKeysForChannelType(Channel.ChannelType type)
    {
        if (type == Channel.ChannelType.Analog)
        {
            return
            [
                "Used", "Mode", "Range", 
                "InputOffset", 
                "LPFilter_Val", 
                "HPFilter_Val",
                "LPFilter_Order",
                "HPFilter_Order",
                "LPFilter_Type",
                "HPFilter_Type",
                "InputType",
                "Excitation",
                "ChannelFeatures",
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

    /// <summary>
    /// Deselect all channels and select only the provided one. Useful for quick isolation and testing.
    /// </summary>
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

        int selected = Channels.Count(x => x.IsSelected);
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID))
        {
            if (!c.IsSelected)
            {
                if (selected >= MaxSelectableChannels) break;
                c.IsSelected = true;
                selected++;
            }
        }

        OnPropertyChanged(nameof(Channels));
        if (selected >= MaxSelectableChannels)
            LogMessages.Add($"Selection limited to {MaxSelectableChannels} channels.");
        else
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