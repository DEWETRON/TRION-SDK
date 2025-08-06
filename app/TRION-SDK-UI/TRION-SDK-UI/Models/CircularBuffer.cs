using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.Models
{
    internal class CircularBuffer
    {
        private int bufferStartPosition;
        private int bufferEndPosition;
        private int bufferSize;
        private byte[] buffer;
    }
}
