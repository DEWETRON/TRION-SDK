using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Trion;
using TrionApiUtils;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI.ViewModels;

public sealed class ChannelDetailViewModel : BaseViewModel
{
    public Channel Channel { get; }

    public string Title => $"Channel {Channel.BoardID}/{Channel.Name}";

    public ObservableCollection<string> Modes { get; } = [];
    public ObservableCollection<string> Ranges { get; } = [];

    // Event the view listens to in order to close the window
    public event EventHandler? CloseRequested;

    private string? _selectedMode;
    public string? SelectedMode
    {
        get => _selectedMode;
        set { if (_selectedMode != value) { _selectedMode = value; OnPropertyChanged(); } }
    }

    private string? _selectedRange;
    public string? SelectedRange
    {
        get => _selectedRange;
        set { if (_selectedRange != value) { _selectedRange = value; OnPropertyChanged(); } }
    }

    // Track UI selection (Channel.IsSelected) instead of hardware "Used"
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    public ICommand ApplyCommand { get; }
    public ICommand RefreshCommand { get; }

    private bool _suppressSync;

    public ChannelDetailViewModel(Channel channel)
    {
        Channel = channel;

        // Populate pickers from channel metadata
        if (Channel.ModeList is { Count: > 0 })
        {
            foreach (var m in Channel.ModeList)
                Modes.Add(m.Name);
            SelectedMode = Channel.Mode?.Name;

            foreach (var range in Channel.Mode.Ranges.Select(r => r.ToString("G")))
                if (!Ranges.Contains(range))
                    Ranges.Add(range);
        }

        // Initialize from Channel selection state
        IsSelected = Channel.IsSelected;

        Channel.PropertyChanged += ChannelOnPropertyChanged;

        ApplyCommand = new Command(async () => await ApplyAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());

        _ = RefreshAsync(); // initial load of live values (mode/range from hardware)
    }

    private void ChannelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSync) return;

        switch (e.PropertyName)
        {
            case nameof(Channel.IsSelected):
                IsSelected = Channel.IsSelected;
                break;
            case nameof(Channel.Mode):
                if (!string.Equals(SelectedMode, Channel.Mode?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedMode = Channel.Mode?.Name;
                    Ranges.Clear();
                    foreach (var r in Channel.Mode.Ranges.Select(x => x.ToString("G")))
                        Ranges.Add(r);
                }
                break;
            case nameof(Channel.Unit):
                // Unit is shown via Channel.Mode.Unit; no VM property needed
                break;
        }
    }

    private string TargetPath => $"BoardID{Channel.BoardID}/{Channel.Name}";

    private async Task RefreshAsync()
    {
        // Read live values from hardware for mode/range
        SelectedMode ??= ReadString("Mode");
        SelectedRange ??= ReadString("Range");

        if (!string.IsNullOrWhiteSpace(SelectedRange) && !Ranges.Contains(SelectedRange))
            Ranges.Add(SelectedRange);

        // Keep Channel in-sync for Mode only (no Channel.Range anymore)
        _suppressSync = true;
        try
        {
            if (!string.IsNullOrWhiteSpace(SelectedMode))
            {
                var newMode = Channel.ModeList
                    .FirstOrDefault(m => string.Equals(m.Name, SelectedMode, StringComparison.OrdinalIgnoreCase));
                if (newMode is not null && !ReferenceEquals(newMode, Channel.Mode))
                    Channel.Mode = newMode;
            }
            // Selection stays app-level only
        }
        finally
        {
            _suppressSync = false;
        }
    }

    private string ReadString(string key)
    {
        var (err, val) = TrionApi.DeWeGetParamStruct_String(TargetPath, key);
        return err == TrionError.NONE ? val : string.Empty;
    }

    private async Task ApplyAsync()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(SelectedMode))
                TrySet("Mode", SelectedMode);

            if (!string.IsNullOrWhiteSpace(SelectedRange))
                TrySet("Range", SelectedRange);

            // Mirror Mode to Channel; do not send or store selection in hardware
            _suppressSync = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(SelectedMode))
                {
                    var newMode = Channel.ModeList
                        .FirstOrDefault(m => string.Equals(m.Name, SelectedMode, StringComparison.OrdinalIgnoreCase));
                    if (newMode is not null && !ReferenceEquals(newMode, Channel.Mode))
                        Channel.Mode = newMode;
                }

                Channel.IsSelected = IsSelected;
            }
            finally
            {
                _suppressSync = false;
            }

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Apply Failed", ex.Message);
        }
    }

    private void TrySet(string key, string value)
    {
        var err = TrionApi.DeWeSetParamStruct(TargetPath, key, value);
        Utils.CheckErrorCode(err, $"Failed to set {key}={value} for {TargetPath}");
    }
}