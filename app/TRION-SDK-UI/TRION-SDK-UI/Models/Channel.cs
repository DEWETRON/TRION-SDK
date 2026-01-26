using System.ComponentModel;
using System.Runtime.CompilerServices;
using TRION_SDK_UI.POCO;

namespace TRION_SDK_UI.Models
{
    // 1. Mark class as abstract
    public abstract class Channel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
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
        
        // Allowed to be set by subclasses
        public ChannelType Type { get; protected set; }

        private string? _range;
        
        // Kept in base for now to ensure UI bindings and AcquisitionManager don't break
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
        public required ChannelMode Mode
        {
            get => _mode;
            set
            {
                if (ReferenceEquals(_mode, value)) return;
                _mode = value;
                OnModeChanged(); 
                OnPropertyChanged();
            }
        }

        // Shared logic for Mode changes
        protected virtual void OnModeChanged()
        {
            if (_mode == null) return;
            
            if (_mode.Unit != null)
                Unit = _mode.Unit;

            if (!string.IsNullOrEmpty(_mode.DefaultValue))
                Range = _mode.DefaultValue;
            else if (_mode.Ranges.Count > 0)
                Range = _mode.Ranges[0];
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

        // 2. Abstract method forces subclasses to implement their specific logic
        public abstract void Activate();

        // Helper for subclasses
        protected string GetTargetName() => $"BoardID{BoardID}/{Name}";
    }
}