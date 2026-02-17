using System.Collections.ObjectModel;
using System.Windows.Input;
using TRION_SDK_UI.Models;
using TrionApiUtils;

namespace TRION_SDK_UI.ViewModels;

public sealed class BoardDetailViewModel : BaseViewModel
{
    public Board Board { get; }
    public string Title => $"Board {Board.Id} - {Board.Name}";

    public ObservableCollection<string> ResolutionAIValues { get; } = [];
    public ObservableCollection<string> OperationModes { get; } = [];
    public ObservableCollection<string> ExternalTriggerValues { get; } = [];
    public ObservableCollection<string> ExternalClockValues { get; } = [];
    public ObservableCollection<int> ProposedSampleRates { get; } = [];
    public ObservableCollection<int> ProposedDividerValues { get; } = [];

    public bool HasProposedSampleRates => ProposedSampleRates.Count > 0;
    public bool IsSampleRateProgrammable { get; }
    public int SampleRateMin { get; }
    public int SampleRateMax { get; }
    public string SampleRateRangeHint => IsSampleRateProgrammable ? $"Range: {SampleRateMin} - {SampleRateMax} Hz" : string.Empty;

    public bool HasProposedDividerValues => ProposedDividerValues.Count > 0;
    public bool HasSampleRateDivider { get; }
    public int DividerMin { get; }
    public int DividerMax { get; }
    public string DividerRangeHint => HasSampleRateDivider ? $"Range: {DividerMin} - {DividerMax}" : string.Empty;

    private bool _suppressSync;

    private string _sampleRateText = string.Empty;
    public string SampleRateText
    {
        get => _sampleRateText;
        set
        {
            if (_sampleRateText == value) return;
            _sampleRateText = value;
            OnPropertyChanged();
            ValidateSampleRate();

            if (!HasSampleRateError)
            {
                CommitPropertyChange(() =>
                {
                    if (int.TryParse(value, out var rate))
                    {
                        Board.SamplingRate = rate;
                        Board.UpdateBuffer(true);
                    }
                });
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

    private string _dividerText = string.Empty;
    public string DividerText
    {
        get => _dividerText;
        set
        {
            if (_dividerText == value) return;
            _dividerText = value;
            OnPropertyChanged();
            ValidateDivider();

            if (!HasDividerError)
            {
                CommitPropertyChange(() =>
                {
                    if (int.TryParse(value, out var div))
                    {
                        Board.SetAcqProp("SampleRateDivider", div.ToString());
                        Board.SampleRateDivider = div;
                    }
                });
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
        set
        {
            if (_selectedOperationMode == value) return;
            _selectedOperationMode = value;
            OnPropertyChanged();

            if (value != null)
            {
                CommitPropertyChange(() =>
                {
                    Board.SetAcqProp("OperationMode", value);
                    Board.OperationMode = value;
                });
            }
        }
    }

    private string? _externalTrigger;
    public string? ExternalTrigger
    {
        get => _externalTrigger;
        set
        {
            if (_externalTrigger == value) return;
            _externalTrigger = value;
            OnPropertyChanged();

            if (value != null)
            {
                CommitPropertyChange(() =>
                {
                    Board.SetAcqProp("ExtTrigger", value);
                    Board.ExternalTrigger = value;
                });
            }
        }
    }

    private string? _externalClock;
    public string? ExternalClock
    {
        get => _externalClock;
        set
        {
            if (_externalClock == value) return;
            _externalClock = value;
            OnPropertyChanged();

            if (value != null)
            {
                CommitPropertyChange(() =>
                {
                    Board.SetAcqProp("ExtClk", value);
                    Board.ExternalClock = value;
                });
            }
        }
    }

    private string? _resolutionAI;
    public string? ResolutionAI
    {
        get => _resolutionAI;
        set
        {
            if (_resolutionAI == value) return;
            _resolutionAI = value;
            OnPropertyChanged();

            if (value != null)
            {
                CommitPropertyChange(() =>
                {
                    Board.SetAcqProp("ResolutionAI", value);
                    Board.ResolutionAI = value;

                    var (error, currentValue) = TrionApi.DeWeGetParamStruct_String($"BoardID{Board.Id}/AcqProp", "ResolutionAI");
                    Utils.CheckErrorCode(error, $"Failed to get ResolutionAI for board {Board.Id}");
                    if (currentValue != value)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                            _ = ShowAlertAsync("ResolutionAI Error", "ResolutionAI not set correctly."));
                    }
                });
            }
        }
    }

    public ICommand SelectProposedSampleRateCommand { get; }
    public ICommand SelectProposedDividerCommand { get; }
    public ICommand CloseCommand { get; }
    public event EventHandler? CloseRequested;

    public BoardDetailViewModel(Board board)
    {
        Board = board ?? throw new ArgumentNullException(nameof(board));
        
        var parser = board.BoardProperties;

        foreach (var mode in parser.GetAvailableValuesFromString("OperationMode"))
        {
            OperationModes.Add(mode);
        }

        foreach (var trig in parser.GetAvailableValuesFromString("ExtTrigger"))
        {
            ExternalTriggerValues.Add(trig);
        }

        foreach (var clk in parser.GetAvailableValuesFromString("ExtClk"))
        {
            ExternalClockValues.Add(clk);
        }
        
        foreach (var res in parser.GetAvailableValuesFromString("ResolutionAI"))
        {
            ResolutionAIValues.Add(res);
        }

        var (srProg, srMin, srMax, srRates) = parser.GetSampleRateCapabilities();
        IsSampleRateProgrammable = srProg;
        SampleRateMin = srMin;
        SampleRateMax = srMax;
        
        foreach (var rate in srRates)
        {
            ProposedSampleRates.Add(rate);
        }

        var (divMin, divMax, divProposed) = parser.GetDividerCapabilities();
        HasSampleRateDivider = divMax > 0;
        DividerMin = divMin;
        DividerMax = divMax;
        
        foreach (var val in divProposed)
        {
            ProposedDividerValues.Add(val);
        }

        SelectProposedSampleRateCommand = new Command<int>(rate => SampleRateText = rate.ToString());
        SelectProposedDividerCommand = new Command<int>(val => DividerText = val.ToString());
        CloseCommand = new Command(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        RefreshBoardState();
    }

    private void CommitPropertyChange(Action hardwareUpdateAction)
    {
        if (_suppressSync) return;

        _ = CommitPropertyChangeInternal(hardwareUpdateAction);
    }

    private async Task CommitPropertyChangeInternal(Action hardwareUpdateAction)
    {
        if (Board.IsAcquiring)
        {
            RefreshBoardState();
            await ShowAlertAsync("Action Failed", "Cannot change settings while acquisition is running.");
            return;
        }

        try
        {
            await Task.Run(hardwareUpdateAction);

            RefreshBoardState();
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Update Failed", ex.Message);
            RefreshBoardState();
        }
    }

    private void RefreshBoardState()
    {
        _suppressSync = true;
        try
        {
            SelectedOperationMode = Board.OperationMode;
            SampleRateText = Board.SamplingRate.ToString();
            DividerText = Board.SampleRateDivider.ToString();
            ExternalTrigger = Board.ExternalTrigger;
            ExternalClock = Board.ExternalClock;
            ResolutionAI = Board.ResolutionAI;
        }
        finally
        {
            _suppressSync = false;
        }
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
}