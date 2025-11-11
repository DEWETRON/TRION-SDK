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
        set
        {
            if (_selectedMode == value) return;
            _selectedMode = value;
            OnPropertyChanged();
            OnSelectedModeChanged(); // update ranges immediately when mode changes in the picker
        }
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
            {
                Modes.Add(m.Name);
            }

            SelectedMode = Channel.Mode?.Name;

            foreach (var range in Channel.Mode.Ranges)
            {
                if (Ranges.Contains(range))
                {
                    continue;
                }
                Ranges.Add(range);
            }
        }

            IsSelected = Channel.IsSelected;

        Channel.PropertyChanged += ChannelOnPropertyChanged;

        ApplyCommand = new Command(async () => await ApplyAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());

        _ = RefreshAsync(); 
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
                    foreach (var r in Channel.Mode.Ranges)
                    {
                        Ranges.Add(r);
                    }
                }
                break;
            case nameof(Channel.Unit):
                break;
        }
    }
    private async Task RefreshAsync()
    {
        _suppressSync = true;
        try
        {
            SelectedMode = Channel.Mode?.Name;

            Ranges.Clear();
            if (Channel.Mode?.Ranges is { Count: > 0 })
            {
                foreach (var r in Channel.Mode.Ranges)
                {
                    Ranges.Add(r);
                }
            }

            string? rangeToSelect = null;

            if (!string.IsNullOrWhiteSpace(Channel.Range) && Ranges.Contains(Channel.Range))
            {
                rangeToSelect = Channel.Range;
            }
            else if (!string.IsNullOrWhiteSpace(Channel.Mode?.DefaultValue)
                     && int.TryParse(Channel.Mode.DefaultValue, out var idx)
                     && idx >= 0
                     && Channel.Mode.Ranges.Count > idx)
            {
                rangeToSelect = Channel.Mode.Ranges[idx];
            }
            else if (Channel.Mode?.Ranges.Count > 0)
            {
                rangeToSelect = Channel.Mode.Ranges[0];
            }

            SelectedRange = rangeToSelect;

            if (!string.IsNullOrWhiteSpace(SelectedMode))
            {
                var newMode = Channel.ModeList.FirstOrDefault(m => string.Equals(m.Name, SelectedMode, StringComparison.OrdinalIgnoreCase));
                if (newMode is not null && !ReferenceEquals(newMode, Channel.Mode))
                {
                    Channel.Mode = newMode;
                }
            }
        }
        finally
        {
            _suppressSync = false;
        }
    }

    private void OnSelectedModeChanged()
    {
        var mode = Channel.ModeList.FirstOrDefault(m => string.Equals(m.Name, _selectedMode, StringComparison.OrdinalIgnoreCase));

        Ranges.Clear();
        if (mode?.Ranges is { Count: > 0 })
        {
            foreach (var r in mode.Ranges)
            {
                if (string.IsNullOrWhiteSpace(r))
                {
                    continue;
                }
                Ranges.Add(r);
            }
        }

        if (!string.IsNullOrWhiteSpace(SelectedRange) && Ranges.Contains(SelectedRange))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Channel.Range) && Ranges.Contains(Channel.Range))
        {
            SelectedRange = Channel.Range;
            return;
        }

        if (mode != null && int.TryParse(mode.DefaultValue, out var idx) && idx >= 0 && idx < Ranges.Count)
        {
            SelectedRange = Ranges[idx];
        }
        else
        {
            SelectedRange = Ranges.FirstOrDefault();
        }
    }

    private async Task ApplyAsync()
    {
        try
        {
            _suppressSync = true;
            try
            {
                if (!string.IsNullOrWhiteSpace(SelectedMode))
                {
                    var newMode = Channel.ModeList.FirstOrDefault(m => string.Equals(m.Name, SelectedMode, StringComparison.OrdinalIgnoreCase));

                    if (newMode is not null && !ReferenceEquals(newMode, Channel.Mode))
                    {
                        Channel.Mode = newMode;
                        if (!string.IsNullOrWhiteSpace(newMode.Unit))
                        {
                            Channel.Unit = newMode.Unit!;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(SelectedRange))
                {
                    if (Channel.Mode?.Ranges?.Contains(SelectedRange) == true)
                    {
                        Channel.Range = SelectedRange;
                    }
                    else if (Channel.Mode?.Ranges?.Count > 0)
                    {
                        Channel.Range = Channel.Mode.Ranges[0];
                    }
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
}