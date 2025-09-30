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

        public enum ChannelType { Unknown = 0, Analog = 1, Digital = 2, Counter = 3 }
        public enum ChannelModeAnalog { Calibration = 0, Voltage = 1, Resistance = 2, IEPE = 3, Bridge = 4, ExcCurrentMonitor = 5, ExcVoltMonitor = 6 }

        public List<ChannelMode> Modes { get; set; } = [];
        
        // fields that don’t change at runtime can stay auto-properties
        public int BoardID { get; set; }
        public string? BoardName { get; set; }
        public string? Name { get; set; }
        public ChannelType Type { get; set; }
        public uint Index { get; set; }
        public uint SampleSize { get; set; }
        public uint SampleOffset { get; set; }

        // notify for properties that change
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

        private double _currentValue;
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

        public string Unit => Type == ChannelType.Analog ? "V" : "";

        public void DeactivateChannel()
        {
            Utils.CheckErrorCode(TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "False"),
                $"Failed to deactivate channel {Name} on board {BoardID}");
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
                default:
                    throw new NotSupportedException($"Channel type {Type} is not supported.");
            }
        }

        void ActivateAnalogChannel()
        {
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "True"),
                $"Failed to activate channel {Name}  on board  {BoardID}");
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Range", "10 V"),
                $"Failed to set range for channel {Name}  on board  {BoardID}");
        }

        void ActivateDigitalChannel()
        {
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Mode", "DIO"),
                $"Failed to set DIO mode {BoardID}");
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "True"),
                $"Failed to activate channel {Name} on board {BoardID}");
        }
    }
}
