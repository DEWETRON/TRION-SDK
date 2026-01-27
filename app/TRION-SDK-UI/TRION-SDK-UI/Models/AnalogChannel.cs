using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class AnalogChannel : Channel
    {
        public AnalogChannel()
        {
            Type = ChannelType.Analog;
        }

        public override void Activate()
        {
            string target = GetTargetName();
            TrionError error;

            error = TrionApi.DeWeSetParamStruct(target, "Used", "True");
            Utils.CheckErrorCode(error, $"Failed to activate channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Mode", Mode.Name);
            Utils.CheckErrorCode(error, $"Failed to set mode {Mode.Name} for channel {Name} on board {BoardID}");

            if (!string.IsNullOrEmpty(Range))
            {
                error = TrionApi.DeWeSetParamStruct(target, "Range", $"{Range} {Unit}");
                Utils.CheckErrorCode(error, $"Failed to set range for channel {Name} on board {BoardID}");
            }
        }
    }
}
