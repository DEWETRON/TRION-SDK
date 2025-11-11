using System.Collections.ObjectModel;
using System.Windows.Input;
using Trion;
using TrionApiUtils;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI.ViewModels;

public sealed class ChannelDetailViewModel : BaseViewModel
{
    private readonly Channel _channel;
    public Channel Channel => _channel;

    public string Title => $"Channel {_channel.BoardID}/{_channel.Name}";

    public ObservableCollection<string> Modes { get; } = [];
    public ObservableCollection<string> Ranges { get; } = [];

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

    private bool _used;
    public bool Used
    {
        get => _used;
        set { if (_used != value) { _used = value; OnPropertyChanged(); } }
    }

    public ICommand ApplyCommand { get; }
    public ICommand RefreshCommand { get; }

    public ChannelDetailViewModel(Channel channel)
    {
        _channel = channel;

        // Populate pickers from channel metadata
        if (_channel.ModeList is { Count: > 0 })
        {
            foreach (var m in _channel.ModeList)
                Modes.Add(m.Name);
            SelectedMode = _channel.Mode?.Name;
            // Use first mode's ranges (or aggregate unique ranges)
            foreach (var range in _channel.Mode.Ranges.Select(r => r.ToString("G")))
                if (!Ranges.Contains(range))
                    Ranges.Add(range);
        }

        ApplyCommand = new Command(async () => await ApplyAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());

        _ = RefreshAsync(); // initial load of live values
    }

    private string TargetPath => $"BoardID{_channel.BoardID}/{_channel.Name}";

    private async Task RefreshAsync()
    {
        // Read live values from hardware
        Used = ReadBool("Used");
        SelectedMode ??= ReadString("Mode");
        SelectedRange ??= ReadString("Range");

        // If hardware reports a range not present yet, add it
        if (!string.IsNullOrWhiteSpace(SelectedRange) && !Ranges.Contains(SelectedRange))
            Ranges.Add(SelectedRange);
    }

    private bool ReadBool(string key)
    {
        var (err, val) = TrionApi.DeWeGetParamStruct_String(TargetPath, key);
        return err == TrionError.NONE && bool.TryParse(val, out var b) && b;
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
            // Apply in a safe order: Mode -> Range -> Used
            if (!string.IsNullOrWhiteSpace(SelectedMode))
                TrySet("Mode", SelectedMode);

            if (!string.IsNullOrWhiteSpace(SelectedRange))
            {
                // Normalize: append unit if user selected only numeric part and channel unit is known
                var rangeVal = SelectedRange;
                if (!_channel.Unit.StartsWith("V", StringComparison.OrdinalIgnoreCase) &&
                    rangeVal.EndsWith(" V", StringComparison.OrdinalIgnoreCase) == false &&
                    _channel.Unit is { Length: > 0 })
                {
                    // Keep as-is (hardware may expect "10 V" style). If numeric only and unit present, append.
                }
                TrySet("Range", rangeVal);
            }

            TrySet("Used", Used ? "True" : "False");

            // Optionally refresh scan descriptor if analog channel & active
            if (_channel.Type == Channel.ChannelType.Analog)
            {
                // The board instance is not directly accessible here; caller could trigger RefreshScanDescriptor.
                // For now just notify user.
            }

            await ShowAlertAsync("Channel Updated", "Changes applied successfully.");
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