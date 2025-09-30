using System.Collections.ObjectModel;
using System.Windows.Input;
using Trion;
using TRION_SDK_UI.Models;

/// <summary>
/// Main ViewModel for the .NET MAUI UI.
/// Initializes TRION API, discovers channels, starts/stops acquisition,
/// updates meters/logs, and notifies the View when new samples arrive.
///
/// Threading:
/// - Acquisition callbacks occur on worker threads. This VM marshals UI updates to the main thread.
/// - ObservableCollection and property changes are raised from the UI thread only.
///
/// Plotting/data flow (current design):
/// - This VM does NOT retain long plotting history. It raises SamplesAppended with only the newest batch.
/// - The View (MainPage) uses ScottPlot DataStreamer per channel to render incoming batches efficiently.
/// - If you need export/analytics or pan-back history, reintroduce a history buffer here (ring/list).
/// </summary>
public class MainViewModel : BaseViewModel, IDisposable
{
    /// <summary>
    /// Digital readout per selected channel. Updated with latest values during acquisition.
    /// The View binds to this to display per-channel "meter" widgets.
    /// </summary>
    public ObservableCollection<DigitalMeter> DigitalMeters { get; } = [];

    /// <summary>
    /// All channels discovered across all boards. Users select from this list before acquisition.
    /// </summary>
    public ObservableCollection<Channel> Channels { get; } = [];

    /// <summary>
    /// UI-visible log lines for diagnostics, status changes, and user feedback.
    /// </summary>
    public ObservableCollection<string> LogMessages { get; } = [];

    // Commands are nullable to satisfy CS8618 (assigned in constructor).

    /// <summary>Start acquisition on the currently selected Channels.</summary>
    public ICommand? StartAcquisitionCommand { get; private set; }

    /// <summary>Stop all ongoing acquisitions.</summary>
    public ICommand? StopAcquisitionCommand { get; private set; }

    /// <summary>Toggle "follow latest" behavior for the live plot (auto-scroll).</summary>
    public ICommand? LockScrollingCommand { get; private set; }

    /// <summary>Toggle application theme between Light and Dark.</summary>
    public ICommand? ToggleThemeCommand { get; private set; }

    /// <summary>Show channel properties (read-only) using TRION string-get API.</summary>
    public ICommand? ShowChannelPropertiesCommand { get; private set; }

    /// <summary>Copy "BoardIDx/ChName" target string to clipboard for diagnostics/automation.</summary>
    public ICommand? CopyChannelPathCommand { get; private set; }

    /// <summary>Unselect all channels then select only the specified one.</summary>
    public ICommand? SelectOnlyChannelCommand { get; private set; }

    /// <summary>Select all channels on the same board as the specified channel.</summary>
    public ICommand? SelectAllOnBoardCommand { get; private set; }

    /// <summary>Deselect all channels on the same board as the specified channel.</summary>
    public ICommand? DeselectAllOnBoardCommand { get; private set; }

    /// <summary>
    /// Orchestrates low-level acquisition (TRION buffer handling) for all boards in <see cref="MyEnc"/>.
    /// Created after successful TRION initialization and enclosure discovery.
    /// </summary>
    private readonly AcquisitionManager? _acquisitionManager;

    /// <summary>
    /// Backing field for FollowLatest toggle. When true, the View should keep X-axis scrolled to newest data.
    /// </summary>
    private bool _isScrollingLocked = true;

    private bool _followLatest = true;

    /// <summary>
    /// Indicates whether the plot should follow the newest samples.
    /// This is toggled by the LockScrollingCommand.
    /// </summary>
    public bool FollowLatest
    {
        get => _followLatest;
        private set { if (_followLatest != value) { _followLatest = value; OnPropertyChanged(); } }
    }

    // Default Y-axis limits. The View reads these and applies them on every redraw.
    private double _yAxisMin = -10;
    private double _yAxisMax = 10;

    /// <summary>
    /// Raised after <see cref="StartAcquisition"/> validates selection but before data begins to arrive.
    /// The View uses this moment to clear existing plot content and pre-create per-channel visuals.
    /// </summary>
    public event EventHandler<IReadOnlyList<Channel>>? AcquisitionStarting;

    /// <summary>
    /// Raised when a new batch of samples has arrived for a channel.
    /// This event carries only the newest batch (not the whole history).
    /// The View feeds the batch into its streaming plot (ScottPlot DataStreamer).
    /// </summary>
    public event EventHandler<SamplesAppendedEventArgs>? SamplesAppended;

    /// <summary>
    /// Event args carrying the channel key and newest batch of samples.
    /// ChannelKey format: "BoardID/ChannelName" (e.g., "1/AI0").
    /// </summary>
    public sealed class SamplesAppendedEventArgs : EventArgs
    {
        /// <summary>Composite key used by the view layer to route data to a per-channel plottable.</summary>
        public string ChannelKey { get; }

        /// <summary>Newest sample batch for this channel. May be empty.</summary>
        public ReadOnlyMemory<double> Samples { get; }

        /// <summary>Number of samples in this batch.</summary>
        public int Count => Samples.Length;

        public SamplesAppendedEventArgs(string channelKey, ReadOnlyMemory<double> samples)
        {
            ChannelKey = channelKey;
            Samples = samples;
        }
    }


    /// <summary>
    /// Dispose pattern: close all boards and uninitialize the TRION API.
    /// The app calls this when shutting down to free native resources deterministically.
    /// </summary>
    public void Dispose()
    {
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);
        TrionApi.Uninitialize();
    }

    /// <summary>
    /// Enclosure abstraction which discovers and stores the opened boards and their properties/channels.
    /// </summary>
    public Enclosure MyEnc { get; } = new Enclosure { Name = "MyEnc", Boards = [] };

    /// <summary>
    /// Constructor: initializes TRION API, builds enclosure/board model, populates Channels,
    /// and wires all UI commands.
    /// Logs whether simulation or real hardware is found.
    /// </summary>
    public MainViewModel()
    {
        LogMessages.Add("App started.");

        // Initialize the TRION API:
        // - Positive return: number of real boards
        // - Negative return: number of simulated boards (absolute value)
        // - Zero: nothing found
        var numberOfBoards = TrionApi.Initialize();
        if (numberOfBoards < 0)
            LogMessages.Add($"Number of simulated Boards found: {Math.Abs(numberOfBoards)}");
        else if (numberOfBoards > 0)
            LogMessages.Add($"Number of real Boards found: {numberOfBoards}");
        else
        {
            // Inform the user and stop early; keep the app responsive for reconfiguration.
            LogMessages.Add("No Trion Boards found.");
            _ = ShowAlertAsync("No TRION boards", "No TRION boards were detected. Configure a system and try again.");
            return;
        }

        // Initialize enclosure and load board properties for each detected board ID.
        numberOfBoards = Math.Abs(numberOfBoards);
        MyEnc.Init(numberOfBoards);
        OnPropertyChanged(nameof(MyEnc));

        // Flatten all boards' eligible channels (Analog/Digital) into a single list for selection.
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

        // Wire up UI commands
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

    /// <summary>
    /// Validate selection and start acquisition on selected channels.
    /// Raises <see cref="AcquisitionStarting"/> so the View can clear and pre-allocate visuals.
    /// </summary>
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

        // Allow the View to reset plot state before data begins to flow.
        AcquisitionStarting?.Invoke(this, selectedChannels);

        // Start low-level acquisition; samples will arrive via OnSamplesReceived callback.
        await _acquisitionManager!.StartAcquisitionAsync(selectedChannels, OnSamplesReceived);
    }

    /// <summary>
    /// Prepare UI state for a new acquisition:
    /// - Clear and recreate the DigitalMeters (one per selected channel).
    /// </summary>
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

    /// <summary>
    /// Stop acquisition by signaling the AcquisitionManager and ending the board loops.
    /// </summary>
    private async Task StopAcquisition()
    {
        LogMessages.Add("Stopping acquisition...");
        await _acquisitionManager!.StopAcquisitionAsync();
    }

    /// <summary>
    /// Toggle FollowLatest (auto-follow plotting). When locked, the plot stays at the newest data.
    /// When unlocked, the user can pan without being pulled forward by incoming data.
    /// </summary>
    private void LockScrolling()
    {
        _isScrollingLocked = !_isScrollingLocked;
        FollowLatest = _isScrollingLocked;
        LogMessages.Add(_isScrollingLocked ? "Scrolling locked." : "Scrolling unlocked.");
    }

    /// <summary>
    /// Toggle between Light and Dark themes at runtime and log the change.
    /// </summary>
    private void ToggleTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.UserAppTheme = Application.Current.UserAppTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
            LogMessages.Add($"Theme changed to {Application.Current.UserAppTheme}.");
        }
    }

    /// <summary>
    /// Acquisition callback (invoked by AcquisitionManager) with the newest batch for a channel.
    /// Marshals to the UI thread to:
    /// - Update the DigitalMeter and channel's CurrentValue,
    /// - Raise <see cref="SamplesAppended"/> with the batch for the View's plot layer.
    /// </summary>
    private void OnSamplesReceived(string channelName, IEnumerable<double> samples)
    {
        // Ensure all UI-bound changes happen on the main thread.
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Materialize the batch once. Using it for meters and event args.
            var batch = samples as double[] ?? [.. samples];
            var latestValue = batch.Length > 0 ? batch[^1] : 0;

            // Update the channel's latest value and corresponding meter.
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

            // Notify the View; it will push this batch into the DataStreamer for drawing.
            SamplesAppended?.Invoke(this, new SamplesAppendedEventArgs(channelName, batch));
        });
    }

    /// <summary>
    /// Read and display a small set of commonly useful TRION properties for a channel.
    /// Uses safe "string-get" calls which fail gracefully if a property is unsupported on the hardware.
    /// </summary>
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

    /// <summary>
    /// Curated read-only property keys to query for the channel type.
    /// Avoids write-only or complex interdependent properties.
    /// </summary>
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

    /// <summary>
    /// Wrapper around TRION string-get API to obtain a (ok,value) tuple without throwing.
    /// </summary>
    private static (bool ok, string value) TryGetParam(string target, string key)
    {
        var (err, val) = TrionApi.DeWeGetParamStruct_String(target, key);
        return (err == TrionError.NONE, val);
    }

    /// <summary>
    /// Copy a TRION channel path like "BoardID1/AI0" to the clipboard and log the action.
    /// </summary>
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

    /// <summary>
    /// Select every channel on the same board as the provided channel. Handy for board-level operations.
    /// </summary>
    private void SelectAllOnBoard(Channel? ch)
    {
        if (ch is null) return;
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID)) c.IsSelected = true;
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Selected all channels on Board {ch.BoardID}");
    }

    /// <summary>
    /// Deselect every channel on the same board as the provided channel.
    /// </summary>
    private void DeselectAllOnBoard(Channel? ch)
    {
        if (ch is null) return;
        foreach (var c in Channels.Where(x => x.BoardID == ch.BoardID)) c.IsSelected = false;
        OnPropertyChanged(nameof(Channels));
        LogMessages.Add($"Deselected all channels on Board {ch.BoardID}");
    }
}

/// <summary>
/// Lightweight identifier for a channel (useful for keys, logs, and bindings).
/// </summary>
public readonly record struct ChannelId(int BoardId, string Name)
{
    public override string ToString() => $"{BoardId}/{Name}";
}