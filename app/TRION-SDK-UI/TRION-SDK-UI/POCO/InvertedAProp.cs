using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.POCO
{
    public class InvertedAProp
    {
        public bool IsPresent { get; init; } = true;
        public int DefaultIndex { get; init; }
        public string[] Values { get; init; } = [];
    }
}
