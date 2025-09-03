using System.Diagnostics;
using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;
public class AcquisitionManager(Enclosure enclosure) : IAsyncDisposable
{
    private readonly Enclosure _enclosure = enclosure;
    private readonly List<Task> _acquisitionTasks = [];
    private readonly List<CancellationTokenSource> _ctsList = [];
    public bool _isRunning = false;

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

        // Board and channel setup
        foreach (var board in selectedBoards)
        {
            board.Reset();
            board.SetAcquisitionProperties();
            board.ActivateChannels(selectedChannels.Where(c => c.BoardID == board.Id));
            // Before activating channels
            Debug.WriteLine($"Activating channels for board {board.Id}: {string.Join(", ", selectedChannels.Select(c => c.Name))}");
            board.Update();
            board.RefreshScanDescriptor();
            // After refreshing scan descriptor
            Debug.WriteLine($"ScanDescriptorXml for board {board.Id}: {board.ScanDescriptorXml}");
            Debug.WriteLine($"ScanDescriptorDecoder channels: {string.Join(", ", board.ScanDescriptorDecoder.Channels.Select(ch => ch.Name))}");
        }


        // Precompute per-channel arrays
        var selectedChannelList = selectedChannels.ToList();
        var scanDescriptor = selectedBoards.First().ScanDescriptorDecoder; // or per board if needed

        var channelInfos = selectedChannelList
            .Select(ch => scanDescriptor.Channels.FirstOrDefault(c => c.Name == ch.Name))
            .ToArray();
        var offsets = channelInfos.Select(ci => (int)ci.SampleOffset / 8).ToArray();
        var sampleSizes = channelInfos.Select(ci => (int)ci.SampleSize).ToArray();
        var channelKeys = selectedChannelList.Select(ch => $"{ch.BoardID}/{ch.Name}").ToArray();

        // Start acquisition tasks
        var channelsByBoard = selectedChannelList.GroupBy(c => c.BoardID);
        foreach (var boardGroup in channelsByBoard)
        {
            var board = _enclosure.Boards.First(b => b.Id == boardGroup.Key);
            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);
            var task = Task.Run(() => AcquireDataLoop(
                board,
                [.. boardGroup],
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
            // Optionally log or handle specific exceptions
        }
        _acquisitionTasks.Clear();
        _ctsList.Clear();
        _isRunning = false;
    }

    private static async Task AcquireDataLoop(
        Board board,
        List<Channel> selectedChannels,
        int[] offsets,
        int[] sampleSizes,
        string[] channelKeys,
        Action<string, IEnumerable<double>> onSamplesReceived,
        CancellationToken token)
    {
        Debug.WriteLine($"TEST: AcquireDataLoop started for Board ID: {board.Id} with channels: {string.Join(", ", selectedChannels.Select(c => c.Name))}");
        var scanSize = (int)board.ScanSizeBytes;
        var polling_interval = (int)(board.BufferBlockSize / (double)board.SamplingRate * 1000);

        TrionError error;
        (error, var adc_delay) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BOARD_ADC_DELAY);
        Utils.CheckErrorCode(error, $"Failed to get ADC Delay {board.Id}");

        error = TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.START_ACQUISITION, 0);
        Utils.CheckErrorCode(error, $"Failed start acquisition {board.Id}");

        CircularBuffer buffer = new(board.Id);
        int available_samples = 0;

        while (!token.IsCancellationRequested)
        {
            (error, available_samples) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
            Utils.CheckErrorCode(error, $"Failed to get available samples {board.Id}, {available_samples}");
            if (available_samples <= 0)
            {
                await Task.Delay(polling_interval, token);
                continue;
            }

            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                await Task.Delay(polling_interval, token);
                continue;
            }

            (error, var read_pos) = TrionApi.DeWeGetParam_i64(board.Id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            Utils.CheckErrorCode(error, $"Failed to get actual sample position {board.Id}");

            read_pos += adc_delay * scanSize;

            var sampleLists = new List<double>[selectedChannels.Count];
            for (int c = 0; c < selectedChannels.Count; ++c)
            {
                sampleLists[c] = new List<double>(available_samples);
            }

            for (int i = 0; i < available_samples; ++i)
            {
                if (read_pos >= buffer.EndPosition)
                {
                    read_pos -= buffer.Size;
                }

                for (int c = 0; c < selectedChannels.Count; ++c)
                {
                    nint samplePos = (nint)(read_pos + offsets[c]);
                    int sampleSize = sampleSizes[c];

                    double value = ReadSample(samplePos, sampleSize);
                    sampleLists[c].Add(value);
                }
                read_pos += scanSize;
            }

            TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);

            for (int c = 0; c < selectedChannels.Count; ++c)
            {
                onSamplesReceived(channelKeys[c], sampleLists[c]);
            }
        }
        TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);
        Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.STOP_ACQUISITION, 0), $"Failed to stop acquisition {board.Id}");
    }

    private static double ReadSample(nint samplePos, int sampleSize)
    {
        // little endian (i guess could be different, maybe check xml)
        int raw;
        switch (sampleSize)
        {
            case 16:
                {
                    byte b0 = Marshal.ReadByte(samplePos);
                    byte b1 = Marshal.ReadByte(samplePos + 1);
                    raw = b0 | (b1 << 8);
                    if ((raw & 0x8000) != 0)
                    {
                        raw |= unchecked((int)0xFFFF0000);
                    }
                    break;
                }
            case 24:
                {
                    byte b0 = Marshal.ReadByte(samplePos);
                    byte b1 = Marshal.ReadByte(samplePos + 1);
                    byte b2 = Marshal.ReadByte(samplePos + 2);
                    raw = b0 | (b1 << 8) | (b2 << 16);
                    if ((raw & 0x800000) != 0)
                    {
                        raw |= unchecked((int)0xFF000000);
                    }
                    break;
                }
            case 32:
                {
                    raw = Marshal.ReadInt32(samplePos);
                    // no sign extension needed already 32 bits
                    break;
                }
            default:
                throw new NotSupportedException($"Unsupported sample size: {sampleSize}");
        }
        int signBit = 1 << (sampleSize - 1);
        double value = (double)raw / (double)(signBit - 1) * 10.0;
        return value;
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