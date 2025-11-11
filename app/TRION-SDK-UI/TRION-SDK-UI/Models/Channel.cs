using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        private ChannelMode _mode = null!;
        public ChannelMode Mode
        {
            get => _mode;
            set
            {
                if (ReferenceEquals(_mode, value)) return;
                _mode = value;
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

            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct(target, "Used", "True"),
                $"Failed to activate channel {Name} on board {BoardID}");

            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct(target, "Mode", _mode.Name),
                $"Failed to set mode {_mode.Name} for channel {Name} on board {BoardID}");

            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct(target, "Range", $"{_range} {_unit}"),
                $"Failed to set range for channel {Name} on board {BoardID}");
        }

        private void ActivateDigitalChannel()
        {
            string target = $"BoardID{BoardID}/{Name}";

            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct(target, "Used", "True"),
                $"Failed to activate channel {Name} on board {BoardID}");

            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct(target, "Mode", _mode.Name),
                $"Failed to set mode {_mode.Name} for channel {Name} on board {BoardID}");
        }
    }
}