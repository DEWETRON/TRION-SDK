using System.ComponentModel;
using System.Runtime.CompilerServices;
using Trion; // added
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    /// <summary>
    /// Represents a single hardware channel on a TRION board.
    /// Handles activation/deactivation through the TRION API and exposes
    /// observable state for UI binding (e.g., selection and live value).
    /// </summary>
    /// <remarks>
    /// The class implements <see cref="INotifyPropertyChanged"/> so it can be bound
    /// directly in .NET MAUI view models / UI. Only mutable runtime state (selection, value)
    /// triggers notifications.
    /// </remarks>
    public class Channel : INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when a property value changes (used by data binding).
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Invokes <see cref="PropertyChanged"/> using the caller member name automatically.
        /// </summary>
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// High-level classification of the physical channel.
        /// </summary>
        public enum ChannelType
        {
            Unknown = 0,
            Analog = 1,
            Digital = 2,
            Counter = 3
        }

        /// <summary>
        /// Known analog operating modes (subset of what hardware may support).
        /// These may map to API strings or configuration sets elsewhere.
        /// </summary>
        public enum ChannelModeAnalog
        {
            Calibration = 0,
            Voltage = 1,
            Resistance = 2,
            IEPE = 3,
            Bridge = 4,
            ExcCurrentMonitor = 5,
            ExcVoltMonitor = 6
        }

        /// <summary>
        /// Available modes (and their ranges) parsed from board XML (<see cref="BoardPropertyModel"/>).
        /// May be empty if metadata was not loaded.
        /// </summary>
        public List<ChannelMode> Modes { get; set; } = [];

        // Immutable (per-run) identification / layout properties:
        /// <summary>
        /// Owning board numeric identifier.
        /// </summary>
        public int BoardID { get; set; }

        /// <summary>
        /// Owning board display name.
        /// </summary>
        public string? BoardName { get; set; }

        /// <summary>
        /// Channel name (e.g., "AI0", "Di3") as exposed by the TRION API.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Channel functional category (analog, digital, etc.).
        /// </summary>
        public ChannelType Type { get; set; }

        // Observable mutable state:

        private bool _isSelected;
        /// <summary>
        /// UI selection state (e.g., picked by user for activation or display).
        /// Raises <see cref="PropertyChanged"/> when modified.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        private double _currentValue;
        /// <summary>
        /// Latest decoded engineering value (e.g., volts) for display.
        /// Updated externally by acquisition pipeline; raises change notification.
        /// </summary>
        public double CurrentValue
        {
            get => _currentValue;
            set
            {
                if (value.Equals(_currentValue)) return;
                _currentValue = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Convenience engineering unit string. Currently returns "V" for analog channels,
        /// otherwise empty. Can be extended to leverage <see cref="Modes"/>.
        /// </summary>
        public string Unit => Type == ChannelType.Analog ? "V" : "";

        /// <summary>
        /// Activates the channel using type-specific logic.
        /// Throws if the channel type is not supported.
        /// </summary>
        public void Activate()
        {
            switch (Type)
            {
                case ChannelType.Analog:
                    ActivateAnalogChannel();
                    break;
                case ChannelType.Digital:
                    ActivateDigitalChannel();
                    break;
                case ChannelType.Counter:
                case ChannelType.Unknown:
                default:
                    throw new NotSupportedException($"Channel type {Type} is not supported.");
            }
        }

        /// <summary>
        /// Performs analog activation steps:
        /// Applies a default range ("10 V").
        /// </summary>
        /// <remarks>
        /// The hard-coded range can be replaced later with dynamic selection
        /// from <see cref="Modes"/> or user configuration.
        /// </remarks>
        private void ActivateAnalogChannel()
        {
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "True"),
                $"Failed to activate channel {Name}  on board  {BoardID}");
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Range", "10 V"),
                $"Failed to set range for channel {Name}  on board  {BoardID}");
        }

        /// <summary>
        /// Performs digital activation steps:
        /// Tries Mode=DIO, then falls back to DI or DO if not supported.
        /// Always attempts to set Used=True. Never throws; logs on failure.
        /// </summary>
        private void ActivateDigitalChannel()
        {
            string target = $"BoardID{BoardID}/{Name}";

            // Try preferred mode first, then safe fallbacks.
            string[] candidateModes =
            [
                "DIO",
                "DI",
                "DO"
            ];

            // If metadata is present, prioritize modes that actually exist for this channel.
            if (Modes.Count > 0)
            {
                var known = Modes.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                candidateModes =
                [
                    .. candidateModes.Where(m => known.Contains(m)),
                    .. candidateModes.Where(m => !known.Contains(m)),
                ];
            }

            TrionError lastErr = TrionError.NONE;
            bool modeSet = false;
            foreach (var mode in candidateModes)
            {
                var err = TrionApi.DeWeSetParamStruct(target, "Mode", mode);
                if (err == TrionError.NONE)
                {
                    modeSet = true;
                    break;
                }
                lastErr = err;
            }

            if (!modeSet)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] Could not set mode to DIO/DI/DO for {target}. Last error={lastErr}");
            }

            var usedErr = TrionApi.DeWeSetParamStruct(target, "Used", "True");
            if (usedErr != TrionError.NONE)
            {
                System.Diagnostics.Debug.WriteLine($"[WARN] Could not set Used=True for {target}. Error={usedErr}");
            }
        }
    }
}