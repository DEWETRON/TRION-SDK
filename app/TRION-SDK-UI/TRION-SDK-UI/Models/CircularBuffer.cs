using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.Models
{
    internal class CircularBuffer
    {
        public long StartPosition { get; set; }
        public long EndPosition { get; set; }
        public int Size { get; set; }
        public byte[] Data { get; set; }
    }
}
