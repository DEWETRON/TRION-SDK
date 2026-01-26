using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models;

internal class CircularBuffer
{
    public long EndPosition { get; }
    public int Size { get; }
    public long StartPosition { get; }

    public CircularBuffer(int board_id)
    {
        var (err, endPos) = TrionApi.DeWeGetParam_i64(board_id, TrionCommand.BUFFER_0_END_POINTER);
        Utils.CheckErrorCode(err, "Failed to get buffer end pointer");
        EndPosition = endPos;

        (err, var size) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BUFFER_0_TOTAL_MEM_SIZE);
        Utils.CheckErrorCode(err, "Failed to get buffer total mem size");
        Size = size;

        StartPosition = EndPosition - Size;
    }

    public void CheckWrapAround(ref long readPos)
    {
        if (readPos >= EndPosition)
        {
            readPos -= Size;
        }
    }
}