using System.ComponentModel;
using System.Runtime.CompilerServices;
using Trion;
using TRION_SDK_UI.POCO;
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

        public required List<ChannelMode> ModeList { get; set; } = [];
        public required int BoardID { get; set; }
        public required string BoardName { get; set; }
        public required string Name { get; set; }
        public required ChannelType Type { get; set; }
        private string? _range;
        public required string? Range
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
        public required ChannelMode Mode
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
                    ActivateCounterChannel();
                    break;
                case ChannelType.Unknown:
                default:
                    throw new NotSupportedException($"Channel type {Type} is not implemented.");
            }
        }

        private void ActivateCounterChannel()
        {
            var target = $"BoardID{BoardID}/{Name}";
            TrionError error;
            error = TrionApi.DeWeSetParamStruct(target, "Used", "True");
            Utils.CheckErrorCode(error, $"Failed to activate channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Source_A", "Acq_Clk");
            Utils.CheckErrorCode(error, $"Failed to set Source_A for channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Mode", _mode.Name);
            Utils.CheckErrorCode(error, $"Failed to set mode {_mode.Name} for channel {Name} on board {BoardID}");
        }

        private void ActivateAnalogChannel()
        {
            string target = $"BoardID{BoardID}/{Name}";
            TrionError error;

            error = TrionApi.DeWeSetParamStruct(target, "Used", "True");
            Utils.CheckErrorCode(error, $"Failed to activate channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Mode", _mode.Name);
            Utils.CheckErrorCode(error, $"Failed to set mode {_mode.Name} for channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Range", $"{_range} {_unit}");
            Utils.CheckErrorCode(error, $"Failed to set range for channel {Name} on board {BoardID}");
        }

        private void ActivateDigitalChannel()
        {
            string target = $"BoardID{BoardID}/{Name}";
            TrionError error;

            error = TrionApi.DeWeSetParamStruct(target, "Used", "True");
            Utils.CheckErrorCode(error, $"Failed to activate channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Mode", _mode.Name);
            Utils.CheckErrorCode(error, $"Failed to set mode {_mode.Name} for channel {Name} on board {BoardID}");
        }
    }
}