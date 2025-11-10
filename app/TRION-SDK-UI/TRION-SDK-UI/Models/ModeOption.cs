
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.Models
{
    public class ModeOption
    {
        public required string Name { get; set; }
        public double Default { get; set; }
        public required List<double> Values { get; set; }
    }
}
