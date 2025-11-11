using System.ComponentModel;
using System.Runtime.CompilerServices;

public class DigitalMeter : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private double _value;

    public double Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            OnPropertyChanged();
        }
    }

    public string? Unit { get; set; }

    public string? Label { get; set; }

    public void AddSample(double sample) => Value = sample;
}