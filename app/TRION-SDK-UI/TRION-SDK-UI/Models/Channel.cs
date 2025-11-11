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
        public required ChannelMode Mode { get; set; } 
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

        public required string Unit { get; set; }

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