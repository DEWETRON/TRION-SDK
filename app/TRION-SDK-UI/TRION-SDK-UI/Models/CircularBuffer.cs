using HarfBuzzSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.Models
{
    internal class CircularBuffer(int board_id)
    {
        public long StartPosition { get; set; } = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_START_POINTER).value;

        public long EndPosition { get; set; } = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER).value;
        public int Size { get; set; } = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE).value;
    }
}
