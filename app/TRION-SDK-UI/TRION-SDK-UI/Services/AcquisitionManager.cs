using System.Diagnostics;
using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;
public class AcquisitionManager : IAsyncDisposable
{
    private readonly Enclosure _enclosure;
    private readonly BoardService _boardService = new();
    private readonly DataAcquisitionService _dataAcquisitionService = new();
    private readonly List<Task> _acquisitionTasks = [];
    private readonly List<CancellationTokenSource> _ctsList = [];
    public bool _isRunning = false;

    public AcquisitionManager(Enclosure enclosure)
    {
        _enclosure = enclosure;
    }

    public async Task StartAcquisitionAsync(IEnumerable<Channel> selectedChannels, Action<string, IEnumerable<double>> onSamplesReceived)
    {
        Debug.WriteLine($"TEST: StartAcquisition called with channels: {string.Join(", ", selectedChannels.Select(c => c.Name))}");
        if (_isRunning)
        {
            await StopAcquisitionAsync();
            Debug.WriteLine("TEST: Previous acquisition stopped.");
        }

        _acquisitionTasks.Clear();
        _ctsList.Clear();

        var selectedBoardIds = selectedChannels.Select(c => c.BoardID).Distinct();
        var selectedBoards = _enclosure.Boards.Where(b => selectedBoardIds.Contains(b.Id)).ToList();

        foreach (var board in selectedBoards)
        {
            board.Reset();
            board.SetAcquisitionProperties();
            board.ActivateChannels(selectedChannels.Where(c => c.BoardID == board.Id));
            Debug.WriteLine($"Activating channels for board {board.Id}: {string.Join(", ", selectedChannels.Select(c => c.Name))}");
            board.Update();
            board.RefreshScanDescriptor();
            Debug.WriteLine($"ScanDescriptorXml for board {board.Id}: {board.ScanDescriptorXml}");
            Debug.WriteLine($"ScanDescriptorDecoder channels: {string.Join(", ", board.ScanDescriptorDecoder.Channels.Select(ch => ch.Name))}");
        }


        // precompute per-channel arrays per board
        var selectedChannelList = selectedChannels.ToList();
        var channelsByBoard = selectedChannelList.GroupBy(c => c.BoardID);

        foreach (var boardGroup in channelsByBoard)
        {
            var board = _enclosure.Boards.First(b => b.Id == boardGroup.Key);
            var scanDescriptor = board.ScanDescriptorDecoder;

            var boardChannels = boardGroup.ToList();
            var channelInfos = boardChannels
                .Select(ch => scanDescriptor.Channels.FirstOrDefault(c => c.Name == ch.Name))
                .ToArray();

            if (channelInfos.Any(ci => ci == null))
            {
                Debug.WriteLine($"ERROR: Some channels not found in scan descriptor for board {board.Id}");
                continue; 
            }

            var offsets = channelInfos.Select(ci => (int)ci.SampleOffset / 8).ToArray();
            var sampleSizes = channelInfos.Select(ci => (int)ci.SampleSize).ToArray();
            var channelKeys = boardChannels.Select(ch => $"{ch.BoardID}/{ch.Name}").ToArray();

            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);
            var task = Task.Run(() => AcquireDataLoop(
                board,
                boardChannels,
                offsets,
                sampleSizes,
                channelKeys,
                onSamplesReceived,
                cts.Token), cts.Token);
            _acquisitionTasks.Add(task);
        }

        _isRunning = true;
    }

    public async Task StopAcquisitionAsync()
    {
        Debug.WriteLine("TEST: StopAcquisition called");
        foreach (var cts in _ctsList)
        {
            cts.Cancel();
        }
        try
        {
            await Task.WhenAll(_acquisitionTasks);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during StopAcquisitionAsync: {ex}");
        }
        _acquisitionTasks.Clear();
        _ctsList.Clear();
        _isRunning = false;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await StopAcquisitionAsync();
        foreach (var board in _enclosure.Boards)
        {
            if (board.IsOpen)
            {
                Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.CLOSE_BOARD, 0), $"Failed to close board {board.Id}");
                board.IsOpen = false;
            }
        }
    }
}