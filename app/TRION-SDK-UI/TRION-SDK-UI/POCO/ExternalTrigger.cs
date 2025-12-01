using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.POCO
{
    public class ExternalTrigger
    {
        public required int DefaultIndex { get; set; }
        public required string[] Values { get; set; }
    }
}
