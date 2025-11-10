
using ScottPlot.Interactivity.UserActionResponses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.Models
{
    public class ModeOption
    {
        public double Default { get; set; }
        public required string Name { get; set; }
        public required List<string> Values { get; set; }
        public string Unit { get; set; }
        public string Programmable { get; set; }
        public double ProgMax { get; set; }
        public double ProgMin { get; set; }
        public double ProgRes { get; set; }
    }
}
