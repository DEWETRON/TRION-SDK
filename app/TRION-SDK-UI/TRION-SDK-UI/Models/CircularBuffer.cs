using Trion;

namespace TRION_SDK_UI.Models
{
    /// <summary>
    /// Lightweight snapshot of the first acquisition circular buffer
    /// associated with a TRION board.
    /// </summary>
    /// <remarks>
    /// Values are captured at construction time:
    /// - EndPosition: Physical (device) end pointer of buffer 0.
    /// - Size: Total allocated memory size of buffer 0 (bytes).
    /// This class does not poll for updates; create a new instance to refresh.
    /// </remarks>
    internal class CircularBuffer(int board_id)
    {
        /// <summary>
        /// Hardware-reported absolute end pointer (address) of buffer 0.
        /// Useful for wrap‑around calculations when interpreting DMA offsets.
        /// </summary>
        public long EndPosition { get; set; } =
            TrionApi.DeWeGetParam_i64(board_id, TrionCommand.BUFFER_0_END_POINTER).value;

        /// <summary>
        /// Total memory size (in bytes) of buffer 0. Can be used to modulo
        /// pointer arithmetic when walking samples in a ring layout.
        /// </summary>
        public int Size { get; set; } =
            TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BUFFER_0_TOTAL_MEM_SIZE).value;
    }
}