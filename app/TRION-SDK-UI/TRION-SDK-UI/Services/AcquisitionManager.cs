using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TRION_SDK_UI.POCO;
using TrionApiUtils;

namespace TRION_SDK_UI.Services;

internal sealed class CircularBuffer
{
    private long EndPosition { get; }
    private int Size { get; }
    private long StartPosition { get; }

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

public class AcquisitionManager(Enclosure enclosure)
{
    private readonly Enclosure _enclosure = enclosure;
    private List<Channel> _selectedChannels = [];
    private readonly List<Task> _acquisitionTasks = [];
    private readonly List<CancellationTokenSource> _ctsList = [];
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Sample>> _sampleQueues = new();
    private readonly ConcurrentDictionary<int, BoardRunContext> _runningBoards = new();
    private bool _isRunning = false;

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

        var worker = new AcquisitionWorker(ctx, _sampleQueues);
        var task = Task.Run(() => worker.RunAsync(cts.Token), cts.Token);

        _acquisitionTasks.Add(task);
    }

    public async Task StartAcquisitionAsync(IEnumerable<Channel> channels)
    {
        if (_isRunning)
        {
            await StopAcquisitionAsync();
        }

        _selectedChannels = [.. channels];
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

        _isRunning = true;
    }
    public Dictionary<string, Sample[]> DrainSamples(int maxPerChannel = 100_00000)
    {
        var result = new Dictionary<string, Sample[]>(_sampleQueues.Count);

        foreach (var (key, q) in _sampleQueues)
        {
            if (q.IsEmpty)
            {
                continue;
            }

            var count = Math.Min(q.Count, maxPerChannel);

            if (0 == count)
            {
                continue;
            }

                var rented = ArrayPool<Sample>.Shared.Rent(count);
            var n = 0;
            try
            {
                while (n < count && q.TryDequeue(out var sample))
                {
                    rented[n++] = sample;
                }

                if (n <= 0)
                {
                    continue;
                }

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
            _isRunning = false;
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

    private sealed class AcquisitionWorker(
        BoardRunContext context,
        ConcurrentDictionary<string, ConcurrentQueue<Sample>> sampleQueues)
    {
        private readonly Board _board = context.Board;
        private readonly List<Channel> _channels = context.Channels;
        private readonly int[] _offsets = context.Offsets;
        private readonly int[] _sampleSizes = context.SampleSizes;
        private readonly string[] _channelKeys = context.ChannelKeys;
        private readonly List<double>[] _channelBuffers = [.. context.Channels.Select(_ => new List<double>())];

        public async Task RunAsync(CancellationToken token)
        {
            if (_board.ScanDescriptor is null)
            {
                Debug.WriteLine($"ScanDescriptor is null for board {_board.Id}. Acquisition loop will exit.");
                return;
            }
            var scanSize = (int)_board.ScanDescriptor.ScanSizeBytes;

            (var error, var adcDelay) = TrionApi.DeWeGetParam_i32(_board.Id, TrionCommand.BOARD_ADC_DELAY);
            Utils.CheckErrorCode(error, $"Failed to get ADC Delay {_board.Id}");

            error = TrionApi.DeWeSetParam_i32(_board.Id, TrionCommand.START_ACQUISITION, 0);
            Utils.CheckErrorCode(error, $"Failed start acquisition {_board.Id}");

            CircularBuffer buffer = new(_board.Id);
            long sampleIndex = 0;

            (error, var hwReadPos) = TrionApi.DeWeGetParam_i64(_board.Id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            Utils.CheckErrorCode(error, $"Failed to get initial sample position {_board.Id}");

            buffer.CheckWrapAround(ref hwReadPos);

            while (!token.IsCancellationRequested)
            {
                (error, var rawAvailable) = TrionApi.DeWeGetParam_i32(_board.Id, TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
                Utils.CheckErrorCode(error, $"Failed to get available samples {_board.Id}");

                if (rawAvailable <= adcDelay)
                {
                    await Task.Delay(1, token);
                    continue;
                }

                var processableSamples = rawAvailable - adcDelay;
                var basePtr = hwReadPos;
                var analogPtr = hwReadPos + ((long)adcDelay * scanSize);

                for (int c = 0; c < _channelBuffers.Length; ++c)
                {
                    var list = _channelBuffers[c];
                    list.Clear();
                    if (list.Capacity < processableSamples) 
                    {
                        list.Capacity = processableSamples;
                    }
                }

                for (int i = 0; i < processableSamples; ++i)
                {
                    buffer.CheckWrapAround(ref basePtr);
                    buffer.CheckWrapAround(ref analogPtr);

                    ProcessScan(basePtr, analogPtr);

                    basePtr += scanSize;
                    analogPtr += scanSize;
                }

                hwReadPos = basePtr;
                buffer.CheckWrapAround(ref hwReadPos);

                for (int i = 0; i < processableSamples; ++i)
                {
                    var elapsedSeconds = (double)(sampleIndex + i) / _board.SamplingRate;
                    for (int c = 0; c < _channelKeys.Length; ++c)
                    {
                        sampleQueues[_channelKeys[c]].Enqueue(new Sample(_channelBuffers[c][i], elapsedSeconds));
                    }
                }
                sampleIndex += processableSamples;

                TrionApi.DeWeSetParam_i32(_board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, processableSamples);
            }
        }

        private void ProcessScan(long readPos, long analogPos)
        {
            for (int c = 0; c < _channels.Count; ++c)
            {
                var channel = _channels[c];
                var samplePos = (nint)(readPos + _offsets[c]);
                var analogSamplePos = (nint)(analogPos + _offsets[c]);
                var sampleSize = _sampleSizes[c];

                var value = ReadChannelValue(channel, samplePos, analogSamplePos, sampleSize);
                
                _channelBuffers[c].Add(value);
            }
        }

        private static double ReadChannelValue(Channel channel, nint samplePos, nint analogPos, int sampleSize)
        {
            if (channel.Type == Channel.ChannelType.Digital)
            {
                return ReadDiscreteSample(samplePos);
            }
            else if (channel.Type == Channel.ChannelType.Analog)
            {
                double range = 1.0; 
                if (double.TryParse(channel.Range, out var parsedRange))
                {
                    range = parsedRange;
                }
                return ReadAnalogSample(analogPos, sampleSize, range);
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

        private static uint ReadDiscreteSample(nint samplePos)
        {
            int raw = Marshal.ReadByte(samplePos);
            return (uint)(raw & 0x1);
        }

        private unsafe static double ReadCounterSample(nint samplePos, int sampleSize)
        {
            if (sampleSize == 32)
            {
                return (double)System.Runtime.CompilerServices.Unsafe.ReadUnaligned<uint>((byte*)samplePos);
            }
            else if (sampleSize == 24)
            {
                var ptr = (byte*)samplePos;
                var val = (uint)(ptr[0] | (ptr[1] << 8) | (ptr[2] << 16));
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
                        var test = (byte*)samplePos;
                        var b0 = test[0];
                        var b1 = test[1];
                        raw = b0 | (b1 << 8);
                        if ((raw & 0x8000) != 0)
                        {
                            raw |= unchecked((int)0xFFFF0000);
                        }
                        break;
                    }
                case 24:
                    {
                        var test = (byte*)samplePos;
                        var b0 = test[0];
                        var b1 = test[1];
                        var b2 = test[2];
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
            var value = (double)raw / (double)(signBit - 1) * scale;
            return value;
        }
    }
}