using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TRION_SDK_UI.Models;
using System.ComponentModel;
using TRION_SDK_UI.Services;
using TRION_SDK_UI.POCO;

namespace TRION_SDK_UI.ViewModels;
public class MainViewModel : BaseViewModel, IDisposable
{
    public ObservableCollection<DigitalMeter> DigitalMeters { get; } = [];
    public ObservableCollection<Channel> Channels { get; } = [];
    public ObservableCollection<string> LogMessages { get; } = [];
    public ICommand? StartAcquisitionCommand { get; private set; }
    public ICommand? StopAcquisitionCommand { get; private set; }
    public ICommand? LockScrollingCommand { get; private set; }
    public ICommand? ToggleThemeCommand { get; private set; }
    public ICommand? CopyChannelPathCommand { get; private set; }
    public ICommand? SelectOnlyChannelCommand { get; private set; }
    public ICommand? SelectAllOnBoardCommand { get; private set; }
    public ICommand? DeselectAllOnBoardCommand { get; private set; }
    public ICommand? OpenChannelWindowCommand { get; private set; }
    public ICommand? OpenBoardWindowCommand { get; private set; }
    public ICommand? MaxCalcCommand { get; private set; }
    public ICommand? PlaceMarkerCommand { get; private set; }
    public ICommand? ClearMarkersCommand { get; private set; }
    public bool IsAcquiring
    {
        get => _isAcquiring;
        set
        {
            if (_isAcquiring != value)
            {
                _isAcquiring = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotAcquiring));
                OnPropertyChanged(nameof(CalcEnabled));
            }
        }
    }

    public bool IsNotAcquiring => !IsAcquiring;
    public bool CalcEnabled => IsAcquiring && IsScrollLocked;
    public event EventHandler<IReadOnlyList<Channel>>? AcquisitionStarting;
    public event EventHandler<SamplesBatchAppendedEventArgs>? SamplesBatchAppended;

    public event EventHandler? PlaceMarkerRequested;
    public event EventHandler? ClearMarkersRequested;
    public event EventHandler? RangeStatsRequested;

    public bool IsScrollLocked
    {
        get => _isScrollLocked;
        private set
        {
            if (_isScrollLocked != value)
            {
                _isScrollLocked = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CalcEnabled));
            }
        }
    }

    public bool FollowLatest => !IsScrollLocked;

    public Enclosure MyEnc => _hardwareService.Enclosure;

    private readonly Dictionary<string, DigitalMeter> _meterByKey = [];
    private IDispatcherTimer? _uiDrainTimer;
    private EventHandler? _drainTickHandler;
    private readonly TimeSpan _meterUpdatePeriod = TimeSpan.FromMilliseconds(33.3); // 30 Hz
    private DateTime _lastMeterUpdateUtc = DateTime.MinValue;
    private bool _isAcquiring;
    private readonly HardwareService _hardwareService = new();
    private readonly AcquisitionManager? _acquisitionManager;
    private bool _isScrollLocked;
    private const int MaxSelectableChannels = 8;
    private bool _suppressSelectionGuard = false;

    public sealed class SamplesBatchAppendedEventArgs(IReadOnlyDictionary<string, Sample[]> batches) : EventArgs
    {
        public IReadOnlyDictionary<string, Sample[]> Batches { get; } = batches;
    }
    public MainViewModel()
    {
        IsAcquiring = false;
        Debug.WriteLine("Started");
        LogMessages.Add("App started.");

        var initResult = _hardwareService.Initialize();

        if (initResult.BoardCount == 0)
        {
            LogMessages.Add("No Trion Boards found.");
            _ = ShowAlertAsync("No TRION boards", "No TRION boards were detected. Configure a system and try again.");
            return;
        }

        LogMessages.Add(initResult.IsSimulated
            ? $"Number of simulated Boards found: {initResult.BoardCount}"
            : $"Number of real Boards found: {initResult.BoardCount}");

        OnPropertyChanged(nameof(MyEnc));

        foreach (var board in MyEnc.Boards)
        {
            LogMessages.Add($"Board: {board.Name} (ID: {board.Id})");
        }

        var channels = _hardwareService.GetChannels(
            Channel.ChannelType.Analog,
            Channel.ChannelType.Digital,
            Channel.ChannelType.Counter);

        foreach (var channel in channels)
        {
            Channels.Add(channel);
            channel.PropertyChanged += OnChannelPropertyChanged;
        }

        _acquisitionManager = new AcquisitionManager(MyEnc);
        OnPropertyChanged(nameof(Channels));

        StartAcquisitionCommand      = new Command(async () => await StartAcquisition());
        StopAcquisitionCommand       = new Command(async () => await StopAcquisition());
        LockScrollingCommand         = new Command(LockScrolling);
        ToggleThemeCommand           = new Command(ToggleTheme);
        CopyChannelPathCommand       = new Command<Channel>(async ch => await CopyChannelPathAsync(ch));
        SelectOnlyChannelCommand     = new Command<Channel>(SelectOnlyChannel);
        SelectAllOnBoardCommand      = new Command<Channel>(SelectAllOnBoard);
        DeselectAllOnBoardCommand    = new Command<Channel>(DeselectAllOnBoard);
        OpenChannelWindowCommand     = new Command<Channel>(OpenChannelWindow);
        OpenBoardWindowCommand       = new Command<Board>(OpenBoardWindow);
        MaxCalcCommand               = new Command(MaxCalc);
        PlaceMarkerCommand           = new Command(PlaceMarker);
        ClearMarkersCommand          = new Command(ClearMarkers);
    }
    private void OpenChannelWindow(Channel? ch)
    {
        if (ch is null)
        {
            return;
        }

        var window = new ChannelDetailWindow(ch);
        Application.Current?.OpenWindow(window);

        LogMessages.Add($"Opened window for {ch.BoardID}/{ch.Name} ({window.Width}x{window.Height})");
    }
    private void OpenBoardWindow(Board? board)
    {
        if (board is null)
        {
            return;
        }

        var window = new BoardDetailWindow(board);
        Application.Current?.OpenWindow(window);

        LogMessages.Add($"Opened board window for {board.Name} (ID: {board.Id})");
    }

    private void PlaceMarker()
    {
        if (!CalcEnabled) return;
        PlaceMarkerRequested?.Invoke(this, EventArgs.Empty);
        LogMessages.Add("Range marker placed.");
    }

    private void ClearMarkers()
    {
        ClearMarkersRequested?.Invoke(this, EventArgs.Empty);
        LogMessages.Add("Range markers cleared.");
    }

    private void MaxCalc()
    {
        if (!CalcEnabled)
        {
            LogMessages.Add("Lock scrolling during acquisition to compute range stats.");
            return;
        }

        RangeStatsRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ReceiveRangeStats(List<ChannelRangeStats> stats)
    {
        if (stats.Count == 0)
        {
            LogMessages.Add("No data in selected range. Place two markers first.");
            return;
        }

        LogMessages.Add("───── Statistics ─────");
        foreach (var s in stats)
        {
            LogMessages.Add($"  {s.ChannelKey}:  Min={s.Min:F4}  Max={s.Max:F4}  Avg={s.Average:F4}  ({s.SampleCount} samples)");
        }
        LogMessages.Add("──────────────────────");
    }

    private void OnChannelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSelectionGuard || sender is not Channel ch || e.PropertyName != nameof(Channel.IsSelected) || !ch.IsSelected)
        {
            return;
        }

        var selected = Channels.Count(c => c.IsSelected);
        if (selected > MaxSelectableChannels)
        {
            _suppressSelectionGuard = true;
            ch.IsSelected = false;
            _suppressSelectionGuard = false;
            LogMessages.Add($"You can select up to {MaxSelectableChannels} channels.");
        }
    }
    private async Task StartAcquisition()
    {
        var selectedChannels = Channels.Where(c => c.IsSelected).ToList();

        if (selectedChannels.Count > MaxSelectableChannels)
        {
            foreach (var extra in selectedChannels.Skip(MaxSelectableChannels))
            {
                extra.IsSelected = false;
            }

            selectedChannels = [.. selectedChannels.Take(MaxSelectableChannels)];
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

        IsAcquiring = true;
        Debug.WriteLine("Starting acquisition...");
        LogMessages.Add("Starting acquisition...");

        await _acquisitionManager!.StartAcquisitionAsync(selectedChannels);
        StartUiDrainTimer();
    }
    private void PrepareUIForAcquisition(List<Channel> selectedChannels)
    {
        DigitalMeters.Clear();
        _meterByKey.Clear();

        foreach (var channel in selectedChannels)
        {
            var key = $"{channel.BoardID}/{channel.Name}";

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
        if (!IsAcquiring)
        {
            return;
        }

        LogMessages.Add("Stopping acquisition...");
        StopUiDrainTimer();
        IsAcquiring = false;
        await _acquisitionManager!.StopAcquisitionAsync();
        LogMessages.Add("Acquisition stopped.");
    }
    private void LockScrolling()
    {
        IsScrollLocked = !IsScrollLocked;
        LogMessages.Add(IsScrollLocked ? "Scrolling locked." : "Scrolling unlocked.");
    }
    private void ToggleTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            LogMessages.Add($"Theme changed to {Application.Current.UserAppTheme}.");
        }
    }
    private void StartUiDrainTimer()
    {
        StopUiDrainTimer();
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return;
        }

        _uiDrainTimer = dispatcher.CreateTimer();
        _uiDrainTimer.Interval = TimeSpan.FromMilliseconds(33.3); // ~30 Hz
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
    private void DrainAndPublish()
    {
        var batches = _acquisitionManager!.DrainSamples(maxPerChannel: 10_000);
        if (0 == batches.Count)
        {
            return;
        }

        var now = DateTime.UtcNow;
        bool updateMeters = (now - _lastMeterUpdateUtc) >= _meterUpdatePeriod;
        if (updateMeters) _lastMeterUpdateUtc = now;

        foreach (var (channelKey, samples) in batches)
        {
            if (updateMeters && samples.Length > 0)
            {
                var latestValue = samples[^1].Value;
                if (_meterByKey.TryGetValue(channelKey, out var meter))
                {
                    meter.Value = latestValue;
                }
            }
        }

        SamplesBatchAppended?.Invoke(this, new SamplesBatchAppendedEventArgs(batches));
    }
    private async Task CopyChannelPathAsync(Channel? ch)
    {
        if (ch is null)
        {
            return;
        }

        string channelPath = $"BoardID{ch.BoardID}/{ch.Name}";
        await Clipboard.SetTextAsync(channelPath);
        LogMessages.Add($"Copied: {channelPath}");
    }
    private void SelectOnlyChannel(Channel? ch)
    {
        if (ch is null)
        {
            return;
        }

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
        if (ch is null)
        {
            return;
        }

        int selected = Channels.Count(x => x.IsSelected);
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID))
        {
            if (c.IsSelected)
            {
                continue;
            }
            if (selected >= MaxSelectableChannels)
            {
                break;
            }
            c.IsSelected = true;
            selected++;
        }

        OnPropertyChanged(nameof(Channels));
        if (selected >= MaxSelectableChannels)
        {
            LogMessages.Add($"Selection limited to {MaxSelectableChannels} channels.");
        }
        else
        {
            LogMessages.Add($"Selected all channels on Board {ch.BoardID}");
        }
    }
    private void DeselectAllOnBoard(Channel? ch)
    {
        if (ch is null)
        {
            return;
        }
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID))
        {
            c.IsSelected = false;
        }
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Deselected all channels on Board {ch.BoardID}");
    }
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _hardwareService.Dispose();
    }
}