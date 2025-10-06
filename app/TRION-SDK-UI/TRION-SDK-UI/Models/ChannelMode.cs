/// <summary>
/// Describes an operational mode for a hardware channel, including
/// its engineering unit and list of supported numeric range values.
/// Instances are typically produced while parsing board XML metadata
/// (see BoardPropertyModel.GetChannelModes).
/// </summary>
public class ChannelMode
{
    /// <summary>
    /// Engineering unit associated with this mode's ranges.
    /// </summary>
    /// <remarks>
    /// Naming note: 'MilliAmperes' keeps original spelling to avoid breaking changes.
    /// If a rename to 'MilliAmperes' is desired, introduce a new enum member and map old values.
    /// </remarks>
    public enum UnitEnum
    {
        /// <summary>No unit / dimensionless.</summary>
        None = 0,
        /// <summary>Volts.</summary>
        Voltage = 1,
        /// <summary>Milliamperes (current).</summary>
        MilliAmperes = 2,
        /// <summary>Hertz (frequency).</summary>
        Hertz = 3,
        /// <summary>Ohms (resistance).</summary>
        Ohm = 4,
    }

    /// <summary>
    /// Mode name as supplied by metadata (e.g., "Voltage", "IEPE", "Resistance").
    /// This is often used as a display label or lookup key.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Engineering unit for interpreting values acquired under this mode.
    /// </summary>
    public UnitEnum Unit { get; set; }

    /// <summary>
    /// Ordered collection of numeric range limits or identifiers parsed from XML.
    /// Interpretation depends on the mode (e.g., min/max pairs, selectable full-scale ranges, etc.).
    /// Empty list indicates no discrete ranges were provided.
    /// </summary>
    public required List<double> Ranges { get; set; }
}