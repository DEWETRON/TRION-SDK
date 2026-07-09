using TRION_SDK_UI.Models;

public class BoardService
{
    public void SetupBoard(Board board, IEnumerable<Channel> channels)
    {
        board.Reset();
        board.SetAcquisitionProperties();
        board.ActivateChannels(channels);
        board.Update();
        board.RefreshScanDescriptor();
    }
}