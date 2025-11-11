using System.ComponentModel;
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

        // Mode now raises notifications and updates Unit if needed
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
                    // If the mode implies a unit, keep Unit in sync
                    if (!string.IsNullOrWhiteSpace(value.Unit) &&
                        !string.Equals(_unit, value.Unit, StringComparison.OrdinalIgnoreCase))
                    {
                        Unit = value.Unit;
                    }
                }
            }
        }

        // New: reflect hardware "Used"
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

        // New: reflect hardware "Range" (string like "10 V")
        private string? _range;
        public string? Range
        {
            get => _range;
            set
            {
                if (value == _range) return;
                _range = value;
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

        // Unit now raises notifications
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
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "True"),
                $"Failed to activate channel {Name}  on board  {BoardID}");
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Range", "10 V"),
                $"Failed to set range for channel {Name}  on board  {BoardID}");
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