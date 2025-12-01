using System.Collections.ObjectModel;
using System.Windows.Input;
using TRION_SDK_UI.Models;

namespace TRION_SDK_UI.ViewModels;

public sealed class BoardDetailViewModel : BaseViewModel
{
    public Board Board { get; }
    public string Title => $"Board {Board.Id} - {Board.Name}";

    public ObservableCollection<string> OperationModes { get; } = [];
    public ObservableCollection<string> SampleRates { get; } = [];

    private string? _selectedOperationMode;
    public string? SelectedOperationMode
    {
        get => _selectedOperationMode;
        set { if (_selectedOperationMode != value) { _selectedOperationMode = value; OnPropertyChanged(); } }
    }

    private string? _selectedSampleRate;
    public string? SelectedSampleRate
    {
        get => _selectedSampleRate;
        set { if (_selectedSampleRate != value) { _selectedSampleRate = value; OnPropertyChanged(); } }
    }

    private string _externalTrigger;
    public string ExternalTrigger
    {
        get => _externalTrigger;
        set { if (_externalTrigger != value) { _externalTrigger = value; OnPropertyChanged(); } }
    }

    private string _externalClock;
    public string ExternalClock
    {
        get => _externalClock;
        set { if (_externalClock != value) { _externalClock = value; OnPropertyChanged(); } }
    }

    public ICommand ApplyCommand { get; }
    public ICommand RefreshCommand { get; }
    public event EventHandler? CloseRequested;

    public BoardDetailViewModel(Board board)
    {
        Board = board;

        // Basic operation modes (tweak if your hardware supports more)
        OperationModes = new ObservableCollection<string>(board.BoardProperties.AcqProp.OperationModeProp.Modes);
        SelectedOperationMode = board.OperationMode;

        SampleRates = new ObservableCollection<string>(board.BoardProperties.AcqProp.SampleRateProp.AvailableRates);
        SelectedSampleRate = board.SamplingRate.ToString();
        ExternalTrigger = board.ExternalTrigger;
        ExternalClock = board.ExternalClock;

        ApplyCommand = new Command(async () => await ApplyAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    private async Task RefreshAsync()
    {
        return;
    }

    private async Task ApplyAsync()
    {
        try
        {
            Board.OperationMode = SelectedOperationMode ?? Board.OperationMode;
            Board.SamplingRate = int.TryParse(SelectedSampleRate, out var parsedRate) ? parsedRate : Board.SamplingRate;
            Board.ExternalTrigger = ExternalTrigger;
            Board.ExternalClock = ExternalClock;
            Board.UpdateAcquisitionProperties();

            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Apply Failed", ex.Message);
        }
    }
}