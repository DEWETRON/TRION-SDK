using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class DigitalChannel : Channel
    {
        public DigitalChannel()
        {
            Type = ChannelType.Digital;
        }

        public override void Activate()
        {
            string target = GetTargetName();
            TrionError error;

            error = TrionApi.DeWeSetParamStruct(target, "Used", "True");
            Utils.CheckErrorCode(error, $"Failed to activate channel {Name} on board {BoardID}");

            error = TrionApi.DeWeSetParamStruct(target, "Mode", Mode.Name);
            Utils.CheckErrorCode(error, $"Failed to set mode {Mode.Name} for channel {Name} on board {BoardID}");
        }
    }
}
