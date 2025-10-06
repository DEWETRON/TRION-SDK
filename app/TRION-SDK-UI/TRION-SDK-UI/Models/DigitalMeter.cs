using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Simple observable numeric readout (digital meter) suitable for binding in a .NET MAUI UI.
/// Exposes a single <see cref="Value"/> plus optional <see cref="Unit"/> and <see cref="Label"/> metadata.
/// </summary>
/// <remarks>
/// Implements <see cref="INotifyPropertyChanged"/> so that UI elements update automatically when
/// <see cref="Value"/> changes. Designed for push-style updates where the latest sample overwrites
/// the previous one (no internal buffering or aggregation).
/// </remarks>
public class DigitalMeter : INotifyPropertyChanged
{
    /// <summary>
    /// Raised whenever a property value changes (currently only <see cref="Value"/> is mutable).
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Helper to raise <see cref="PropertyChanged"/> using the caller member name.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private double _value;

    /// <summary>
    /// Latest numeric reading to display. Setting triggers a change notification if the value differs.
    /// </summary>
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

    /// <summary>
    /// Optional engineering unit label (e.g., "V", "Hz", "°C").
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Optional descriptive label (e.g., channel name or metric caption).
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Updates the meter with a new sample (alias for setting <see cref="Value"/>).
    /// </summary>
    /// <param name="sample">Newest measurement value.</param>
    public void AddSample(double sample) => Value = sample;
}