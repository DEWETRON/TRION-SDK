using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class Channel
    {

        public enum ChannelType
        {
            Unknown = 0,
            Analog = 1,
            Digital = 2,
            Counter = 3
        }

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

        public List <ChannelMode> Modes { get; set; } = [];
        public int BoardID { get; set; }
        public string? BoardName { get; set; }
        public string? Name { get; set; }
        public ChannelType Type { get; set; }
        public uint Index { get; set; }
        public uint SampleSize { get; set; }
        public uint SampleOffset { get; set; }
        public bool IsSelected { get; set; }
        public void DeactivateChannel()
        {
            Utils.CheckErrorCode(TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "False"),$"Failed to deactivate channel {Name} on board {BoardID}");
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
                    throw new NotSupportedException($"Channel type {Type} is not supported.");
                default:
                    throw new NotSupportedException($"Channel type {Type} is not supported.");
            }
        }
        void ActivateAnalogChannel()
        {
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "True"),
                $"Failed to activate channel {Name}  on board  {BoardID}");
            // set range 
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Range", "10 V"),
                $"Failed to set range for channel {Name}  on board  {BoardID}");
        }

        void ActivateDigitalChannel()
        {
            // set mode to DIO
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Mode", "DIO"),
                $"Failed to set DIO mode {BoardID}");

            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{BoardID}/{Name}", "Used", "True"),
                $"Failed to activate channel {Name} on board {BoardID}");
        }
    }
}
