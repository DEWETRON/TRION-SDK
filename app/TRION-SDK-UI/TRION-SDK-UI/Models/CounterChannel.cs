using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class CounterChannel : Channel
    {
        public CounterChannel()
        {
            Type = ChannelType.Counter;
        }

        public override void Activate()
        {
            var target = GetTargetName();
            TrionError error;

            error = TrionApi.DeWeSetParamStruct(target, "Used", "True");
            Utils.CheckErrorCode(error, $"Failed to activate channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Mode", Mode.Name);
            Utils.CheckErrorCode(error, $"Failed to set mode {Mode.Name} for channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Source_A", "Acq_Clk");
            Utils.CheckErrorCode(error, $"Failed to set Source_A for channel {Name} on board {BoardID}");
        }
    }
}
