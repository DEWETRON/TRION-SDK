using System.Collections.ObjectModel;
using System.Windows.Input;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI.ViewModels;

public sealed class BoardDetailViewModel : BaseViewModel
{
    public Board Board { get; }
    public string Title => $"Board {Board.Id} - {Board.Name}";

    public ObservableCollection<string> OperationModes { get; } = [];
    public ObservableCollection<string> ExternalTriggerValues { get; } = [];
    public ObservableCollection<string> ExternalClockValues { get; } = [];

    // Sample Rate - supports free entry within range + proposed values
    public ObservableCollection<int> ProposedSampleRates { get; } = [];
    public bool HasProposedSampleRates => ProposedSampleRates.Count > 0;
    public bool IsSampleRateProgrammable { get; }
    public int SampleRateMin { get; }
    public int SampleRateMax { get; }
    public string SampleRateRangeHint => IsSampleRateProgrammable ? $"Range: {SampleRateMin} - {SampleRateMax} Hz" : string.Empty;

    private string _sampleRateText = string.Empty;
    public string SampleRateText
    {
        get => _sampleRateText;
        set
        {
            if (_sampleRateText != value)
            {
                _sampleRateText = value;
                OnPropertyChanged();
                ValidateSampleRate();
            }
        }
    }

    private string? _sampleRateError;
    public string? SampleRateError
    {
        get => _sampleRateError;
        set { if (_sampleRateError != value) { _sampleRateError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSampleRateError)); } }
    }
    public bool HasSampleRateError => !string.IsNullOrEmpty(SampleRateError);

    // Sample Rate Divider - supports free entry within range + proposed values
    public ObservableCollection<int> ProposedDividerValues { get; } = [];
    public bool HasProposedDividerValues => ProposedDividerValues.Count > 0;
    public bool HasSampleRateDivider { get; }
    public int DividerMin { get; }
    public int DividerMax { get; }
    public string DividerRangeHint => HasSampleRateDivider ? $"Range: {DividerMin} - {DividerMax}" : string.Empty;

    private string _dividerText = string.Empty;
    public string DividerText
    {
        get => _dividerText;
        set
        {
            if (_dividerText != value)
            {
                _dividerText = value;
                OnPropertyChanged();
                ValidateDivider();
            }
        }
    }

    private string? _dividerError;
    public string? DividerError
    {
        get => _dividerError;
        set { if (_dividerError != value) { _dividerError = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDividerError)); } }
    }
    public bool HasDividerError => !string.IsNullOrEmpty(DividerError);

    private string? _selectedOperationMode;
    public string? SelectedOperationMode
    {
        get => _selectedOperationMode;
        set { if (_selectedOperationMode != value) { _selectedOperationMode = value; OnPropertyChanged(); } }
    }

    private string? _externalTrigger;
    public string? ExternalTrigger
    {
        get => _externalTrigger;
        set { if (_externalTrigger != value) { _externalTrigger = value; OnPropertyChanged(); } }
    }

    private string? _externalClock;
    public string? ExternalClock
    {
        get => _externalClock;
        set { if (_externalClock != value) { _externalClock = value; OnPropertyChanged(); } }
    }

    public ICommand ApplyCommand { get; }
    public ICommand SelectProposedSampleRateCommand { get; }
    public ICommand SelectProposedDividerCommand { get; }
    public event EventHandler? CloseRequested;

    public BoardDetailViewModel(Board board)
    {
        Board = board ?? throw new ArgumentNullException(nameof(board));

        var acqProp = board.BoardProperties?.AcqProp;

        if (acqProp?.OperationModeProp is { IsPresent: true, Modes.Length: > 0 } opMode)
        {
            foreach (var mode in opMode.Modes)
                OperationModes.Add(mode);
        }
        SelectedOperationMode = board.OperationMode;

        var sampleRateProp = acqProp?.SampleRateProp;
        if (sampleRateProp is { IsPresent: true })
        {
            IsSampleRateProgrammable = sampleRateProp.Programmable;
            SampleRateMin = sampleRateProp.ProgMin;
            SampleRateMax = sampleRateProp.ProgMax;

            foreach (var rate in sampleRateProp.AvailableRates)
            {
                if (int.TryParse(rate, out var rateValue))
                    ProposedSampleRates.Add(rateValue);
            }
        }
        SampleRateText = board.SamplingRate.ToString();

        var dividerProp = acqProp?.SampleRateDividerProp;
        HasSampleRateDivider = dividerProp is not null;
        if (dividerProp is not null)
        {
            DividerMin = dividerProp.ProgMin;
            DividerMax = dividerProp.ProgMax;

            foreach (var val in dividerProp.ProposedValues)
                ProposedDividerValues.Add(val);
        }
        DividerText = board.SampleRateDivider.ToString();

        if (acqProp?.ExternalTriggerProp is { IsPresent: true })
        {
            foreach (var val in acqProp.ExternalTriggerProp.Values)
                ExternalTriggerValues.Add(val);
        }
        ExternalTrigger = board.ExternalTrigger;

        if (acqProp?.ExternalClockProp is { IsPresent: true })
        {
            foreach (var val in acqProp.ExternalClockProp.Values)
                ExternalClockValues.Add(val);
        }
        ExternalClock = board.ExternalClock;

        ApplyCommand = new Command(async () => await ApplyAsync(), () => !HasSampleRateError && !HasDividerError);
        SelectProposedSampleRateCommand = new Command<int>(rate => SampleRateText = rate.ToString());
        SelectProposedDividerCommand = new Command<int>(val => DividerText = val.ToString());
    }

    private void ValidateSampleRate()
    {
        if (!int.TryParse(SampleRateText, out var value))
        {
            SampleRateError = "Must be a valid number";
            return;
        }

        if (IsSampleRateProgrammable && (value < SampleRateMin || value > SampleRateMax))
        {
            SampleRateError = $"Must be between {SampleRateMin} and {SampleRateMax}";
            return;
        }

        SampleRateError = null;
    }

    private void ValidateDivider()
    {
        if (!HasSampleRateDivider)
        {
            DividerError = null;
            return;
        }

        if (!int.TryParse(DividerText, out var value))
        {
            DividerError = "Must be a valid number";
            return;
        }

        if (value < DividerMin || value > DividerMax)
        {
            DividerError = $"Must be between {DividerMin} and {DividerMax}";
            return;
        }

        DividerError = null;
    }

    private async Task ApplyAsync()
    {
        if (Board.IsAcquiring)
        {
            await ShowAlertAsync("Apply Failed", "Cannot apply settings while acquisition is running.");
            return;
        }

        if (HasSampleRateError || HasDividerError)
        {
            await ShowAlertAsync("Validation Error", "Please fix validation errors before applying.");
            return;
        }

        try
        {
            Board.OperationMode = SelectedOperationMode ?? Board.OperationMode;
            Board.SamplingRate = int.TryParse(SampleRateText, out var rate) ? rate : Board.SamplingRate;
            Board.ExternalTrigger = ExternalTrigger ?? Board.ExternalTrigger;
            Board.ExternalClock = ExternalClock ?? Board.ExternalClock;

            if (HasSampleRateDivider && int.TryParse(DividerText, out var divider))
                Board.SampleRateDivider = divider;

            Board.UpdateAcquisitionProperties();
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Apply Failed", ex.Message);
        }
    }
}