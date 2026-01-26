using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TRION_SDK_UI.POCO;
using TrionApiUtils;

namespace TRION_SDK_UI.Services;
public class AcquisitionManager(Enclosure enclosure)
{
    private readonly Enclosure _enclosure = enclosure;
    private List<Channel> _selectedChannels = [];

    private readonly List<Task> _acquisitionTasks = [];

    private readonly List<CancellationTokenSource> _ctsList = [];

    public bool IsRunning = false;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<Sample>> _sampleQueues = new();

    private readonly ConcurrentDictionary<int, BoardRunContext> _runningBoards = new();

    private sealed record BoardRunContext(
        Board Board,
        List<Channel> Channels,
        int[] Offsets,
        int[] SampleSizes,
        string[] ChannelKeys
    );

    private void PrepareBoardRunContext(IGrouping<int, Channel> boardGroup)
    {
        var board = _enclosure.Boards.FirstOrDefault(b => b.Id == boardGroup.Key);
        if (board is null)
        {
            Debug.WriteLine($"Skipping board {boardGroup.Key}: board not found in enclosure.");
            return;
        }

        var scanDescriptor = board.ScanDescriptor;
        if (scanDescriptor is null)
        {
            Debug.WriteLine($"Skipping board {board.Id}: ScanDescriptor is null.");
            return;
        }

        var channels = boardGroup.ToList();

        var channelInfos = channels
            .Select(ch => scanDescriptor.Channels.FirstOrDefault(c => c.Name == ch.Name))
            .ToArray();

        if (channelInfos.Any(ci => ci is null))
        {
            var missing = channels
                .Select((ch, i) => (ch, ci: channelInfos[i]))
                .Where(x => x.ci is null)
                .Select(x => x.ch.Name)
                .ToArray();

            Debug.WriteLine($"Skipping board {board.Id}: ScanDescriptor missing channels [{string.Join(", ", missing)}].");
            return;
        }

        var offsets = channelInfos.Select(ci => (int)ci!.SampleOffset / 8).ToArray();
        var sampleSizes = channelInfos.Select(ci => (int)ci!.SampleSize).ToArray();
        var channelKeys = channels.Select(ch => $"{ch.BoardID}/{ch.Name}").ToArray();

        for (int i = 0; i < channelKeys.Length; i++)
        {
            _sampleQueues.TryAdd(channelKeys[i], new ConcurrentQueue<Sample>());
        }

        _runningBoards[board.Id] = new BoardRunContext(board, channels, offsets, sampleSizes, channelKeys);
    }

    private void StartBoardAcquisition(BoardRunContext ctx)
    {
        var cts = new CancellationTokenSource();
        _ctsList.Add(cts);

        _runningBoards[ctx.Board.Id] = ctx;

        var task = Task.Run(() => AcquireDataLoop(ctx, cts.Token), cts.Token);

        _acquisitionTasks.Add(task);
    }

    public async Task StartAcquisitionAsync(IEnumerable<Channel> channels)
    {
        if (IsRunning) await StopAcquisitionAsync();

        _selectedChannels = channels.ToList();

        _acquisitionTasks.Clear();
        _ctsList.Clear();
        _sampleQueues.Clear();
        _runningBoards.Clear();

        var selectedBoardIds = _selectedChannels.Select(c => c.BoardID).Distinct();
        var selectedBoards = _enclosure.Boards.Where(b => selectedBoardIds.Contains(b.Id)).ToList();
        
        foreach (var board in selectedBoards)
        {
            board.Reset();
            board.UpdateAcquisitionProperties();
            board.ActivateChannels(_selectedChannels.Where(c => c.BoardID == board.Id));
            board.Update();
            board.RefreshScanDescriptor();
            board.IsAcquiring = true;
        }

        var channelsByBoard = _selectedChannels.GroupBy(c => c.BoardID);

        foreach (var boardGroup in channelsByBoard)
        {
            PrepareBoardRunContext(boardGroup);
            
            if (_runningBoards.TryGetValue(boardGroup.Key, out var ctx))
            {
                StartBoardAcquisition(ctx);
            }
        }

        IsRunning = true;
    }

    public async Task StopAcquisitionAsync()
    {
        foreach (var cts in _ctsList)
        {
            cts.Cancel();
        }
        try
        {
            if (_acquisitionTasks.Count > 0)
            {
                await Task.WhenAll(_acquisitionTasks);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during StopAcquisitionAsync: {ex}");
        }
        finally
        {
            IsRunning = false;
        }

        foreach (var boardId in _runningBoards.Keys)
        {
            var error = TrionApi.DeWeSetParam_i32(boardId, TrionCommand.STOP_ACQUISITION, 0);
            Utils.CheckErrorCode(error, $"Failed to stop acquisition on board {boardId}");
            
            if (_runningBoards.TryGetValue(boardId, out var ctx))
            {
                ctx.Board.IsAcquiring = false;
            }
        }

        _acquisitionTasks.Clear();
        _ctsList.Clear();
        _runningBoards.Clear();
    }
    
    public Dictionary<string, Sample[]> DrainSamples(int maxPerChannel = 100_000)
    {
        var result = new Dictionary<string, Sample[]>(_sampleQueues.Count);

        foreach (var (key, q) in _sampleQueues)
        {
            if (q.IsEmpty) continue;

            // Determine how many items to read: existing backlog or limit, whichever is smaller
            int count = Math.Min(q.Count, maxPerChannel);
            
            if (count == 0) continue;

            var rented = ArrayPool<Sample>.Shared.Rent(count);
            int n = 0;
            try
            {
                while (n < count && q.TryDequeue(out var sample))
                {
                    rented[n++] = sample;
                }

                if (n <= 0) continue;

                var arr = GC.AllocateUninitializedArray<Sample>(n);
                Array.Copy(rented, arr, n);
                result[key] = arr;
            }
            finally
            {
                ArrayPool<Sample>.Shared.Return(rented, clearArray: false);
            }
        }

        return result;
    }
    private static uint ReadDiscreteSample(nint samplePos)
    {
        int raw = Marshal.ReadByte(samplePos);
        return (uint)(raw & 0x1);
    }

    private async Task AcquireDataLoop(
        BoardRunContext ctx,
        CancellationToken token)
    {
        var board = ctx.Board;
        var offsets = ctx.Offsets;
        var sampleSizes = ctx.SampleSizes;
        var channelKeys = ctx.ChannelKeys;
        var selectedChannels = ctx.Channels;

        if (board.ScanDescriptor is null)
        {
            Debug.WriteLine($"ScanDescriptor is null for board {board.Id}. Acquisition loop will exit.");
            return;
        }
        var scanSize = (int)board.ScanDescriptor.ScanSizeBytes;

        (var error, var adcDelay) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BOARD_ADC_DELAY);
        Utils.CheckErrorCode(error, $"Failed to get ADC Delay {board.Id}");
        Debug.WriteLine($"ADC Delay is {adcDelay}");

        error = TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.START_ACQUISITION, 0);
        Utils.CheckErrorCode(error, $"Failed start acquisition {board.Id}");

        CircularBuffer buffer = new(board.Id);
        long sampleIndex = 0;

        while (!token.IsCancellationRequested)
        {
            (error, var availableSamples) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
            Utils.CheckErrorCode(error, $"Failed to get available samples {board.Id}, {availableSamples}");

            availableSamples -= adcDelay;
            if (availableSamples <= 0)
            {
                await Task.Delay(1, token);
                continue;
            }

            (error, var readPos) = TrionApi.DeWeGetParam_i64(board.Id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            Utils.CheckErrorCode(error, $"Failed to get actual sample position {board.Id}");

            readPos += adcDelay * scanSize;

            var sampleLists = new List<double>[selectedChannels.Count];
            for (int c = 0; c < selectedChannels.Count; ++c)
                sampleLists[c] = new List<double>(availableSamples);

            for (int i = 0; i < availableSamples; ++i)
            {
                buffer.CheckWrapAround(ref readPos);
                ProcessScan(ref readPos, offsets, sampleSizes, sampleLists);
                readPos += scanSize;
            }

            for (int i = 0; i < availableSamples; ++i)
            {
                double elapsedSeconds = (double)(sampleIndex + i) / board.SamplingRate;
                for (int c = 0; c < channelKeys.Length; ++c)
                {
                    var key = channelKeys[c];
                    var q = _sampleQueues[key];
                    q.Enqueue(new Sample(sampleLists[c][i], elapsedSeconds));
                }
            }
            sampleIndex += availableSamples;
            TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, availableSamples);
        }
    }

    private void ProcessScan(
        ref long readPos,
        int[] offsets,
        int[] sampleSizes,
        List<double>[] sampleLists)
    {
        for (int c = 0; c < _selectedChannels.Count; ++c)
        {
            var channel = _selectedChannels[c];
            nint samplePos = (nint)(readPos + offsets[c]);
            int sampleSize = sampleSizes[c];

            double value = ReadChannelValue(channel, samplePos, sampleSize);
            sampleLists[c].Add(value);
        }
    }

    private static double ReadChannelValue(Channel channel, nint samplePos, int sampleSize)
    {
        if (channel.Type == Channel.ChannelType.Digital)
        {
            return ReadDiscreteSample(samplePos);
        }
        else if (channel.Type == Channel.ChannelType.Analog)
        {
            double range = 1; // Default scale value
            if (double.TryParse(channel.Range, out var parsedRange))
            {
                range = parsedRange;
            }
            return ReadAnalogSample(samplePos, sampleSize, range);
        }
        else if (channel.Type == Channel.ChannelType.Counter)
        {
            return ReadCounterSample(samplePos, sampleSize);
        }
        else
        {
            throw new NotSupportedException($"Unsupported channel type: {channel.Type}");
        }
    }

    private unsafe static double ReadCounterSample(nint samplePos, int sampleSize)
    {
        if (sampleSize == 32)
        {
            return (double)System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>((byte*)samplePos);
        }
        else if (sampleSize == 24)
        {
            byte* ptr = (byte*)samplePos;
            uint val = (uint)(ptr[0] | (ptr[1] << 8) | (ptr[2] << 16));
            return (double)val;
        }
        else
        {
            throw new NotSupportedException($"Unsupported counter sample size: {sampleSize}");
        }
    }
    private unsafe static double ReadAnalogSample(nint samplePos, int sampleSize, double scale)
    {
        int raw;
        switch (sampleSize)
        {
            case 16:
                {
                    byte *test = (byte *)samplePos;
                    byte b0 = test[0];
                    byte b1 = test[1];
                    raw = b0 | (b1 << 8);
                    if ((raw & 0x8000) != 0)
                    {
                        raw |= unchecked((int)0xFFFF0000);
                    }
                    break;
                }
            case 24:
                {
                    byte *test = (byte *)samplePos;
                    byte b0 = test[0];
                    byte b1 = test[1];
                    byte b2 = test[2];
                    raw = b0 | (b1 << 8) | (b2 << 16);
                    if ((raw & 0x800000) != 0)
                    {
                        raw |= unchecked((int)0xFF000000);
                    }
                    break;
                }
            case 32:
                {
                    raw = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<int>((byte*)samplePos);
                    break;
                }
            default:
                throw new NotSupportedException($"Unsupported sample size: {sampleSize}");
        }

        int signBit = 1 << (sampleSize - 1);
        double value = (double)raw / (double)(signBit - 1) * scale;
        return value;
    }

}