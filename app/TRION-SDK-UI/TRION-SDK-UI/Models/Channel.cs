using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class Channel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public enum ChannelType
        {
            Unknown = 0,
            Analog = 1,
            Digital = 2,
            Counter = 3
        }

        public List<ChannelMode> ModeList { get; set; } = [];
        public int BoardID { get; set; }
        public string? BoardName { get; set; }
        public string? Name { get; set; }
        public ChannelType Type { get; set; }

        private ChannelMode _mode = null!;
        public required ChannelMode Mode
        {
            get => _mode;
            set
            {
                if (!ReferenceEquals(_mode, value))
                {
                    _mode = value;
                    OnPropertyChanged();
                    if (!string.IsNullOrWhiteSpace(value.Unit) &&
                        !string.Equals(_unit, value.Unit, StringComparison.OrdinalIgnoreCase))
                    {
                        Unit = value.Unit;
                    }
                }
            }
        }

        private bool _used;
        public bool Used
        {
            get => _used;
            set
            {
                if (value == _used) return;
                _used = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelected;
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

        private string _unit = null!;
        public required string Unit
        {
            get => _unit;
            set
            {
                if (value == _unit) return;
                _unit = value;
                OnPropertyChanged();
            }
        }

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

        private void ActivateAnalogChannel()
        {
            string target = $"BoardID{BoardID}/{Name}";

            // Enable channel
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct(target, "Used", "True"),
                $"Failed to activate channel {Name} on board {BoardID}");

            // Set a default range derived from Mode (if available)
            var defaultRange = GetDefaultRangeParameter();
            if (!string.IsNullOrWhiteSpace(defaultRange))
            {
                Utils.CheckErrorCode(
                    TrionApi.DeWeSetParamStruct(target, "Range", defaultRange!),
                    $"Failed to set range for channel {Name} on board {BoardID}");
            }
        }

        private string? GetDefaultRangeParameter()
        {
            // Prefer provided default value if Mode supplies one (assumed already in device format)
            if (!string.IsNullOrWhiteSpace(Mode.DefaultValue))
                return Mode.DefaultValue;

            // Otherwise choose a reasonable range from available list (largest magnitude is typical default, e.g., 10 V)
            if (Mode.Ranges is { Count: > 0 })
            {
                double v = Mode.Ranges.OrderByDescending(Math.Abs).First();
                var unit = !string.IsNullOrWhiteSpace(Mode.Unit) ? Mode.Unit : Unit;
                return string.IsNullOrWhiteSpace(unit)
                    ? v.ToString("G", CultureInfo.InvariantCulture)
                    : $"{v.ToString("G", CultureInfo.InvariantCulture)} {unit}";
            }

            // No known default
            return null;
        }

        private void ActivateDigitalChannel()
        {
            string target = $"BoardID{BoardID}/{Name}";

            string[] candidateModes =
            [
                "DIO",
                "DI",
                "DO"
            ];

            if (ModeList.Count > 0)
            {
                var known = ModeList.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
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