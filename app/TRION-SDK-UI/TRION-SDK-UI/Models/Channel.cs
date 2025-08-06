using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.Models
{
    public class Channel
    {
        public string? Name { get; set; }
        public string? ChannelType { get; set; }
        public uint Index { get; set; }
        public uint SampleSize { get; set; }
        public uint SampleOffset { get; set; }
    }
}
