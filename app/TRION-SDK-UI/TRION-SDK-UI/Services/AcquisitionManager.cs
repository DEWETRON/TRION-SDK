using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

public class AcquisitionManager(Enclosure enclosure)
{
    private readonly Enclosure _enclosure = enclosure;

    private readonly List<Task> _acquisitionTasks = [];

    private readonly List<CancellationTokenSource> _ctsList = [];

    public bool _isRunning = false;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<Sample>> _sampleQueues = new();

    private sealed record BoardRunContext(
        Board Board,
        List<Channel> Channels,
        int[] Offsets,
        int[] SampleSizes,
        string[] ChannelKeys
    );

    private BoardRunContext? PrepareBoardRunContext(IGrouping<int, Channel> boardGroup)
    {
        var board = _enclosure.Boards.FirstOrDefault(b => b.Id == boardGroup.Key);
        if (board is null)
        {
            Debug.WriteLine($"Skipping board {boardGroup.Key}: board not found in enclosure.");
            return null;
        }

        var scanDescriptor = board.ScanDescriptorDecoder;
        if (scanDescriptor is null)
        {
            Debug.WriteLine($"Skipping board {board.Id}: ScanDescriptor is null.");
            return null;
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
            return null;
        }

        var offsets = channelInfos.Select(ci => (int)ci!.SampleOffset / 8).ToArray();
        var sampleSizes = channelInfos.Select(ci => (int)ci!.SampleSize).ToArray();
        var channelKeys = channels.Select(ch => $"{ch.BoardID}/{ch.Name}").ToArray();

        for (int i = 0; i < channelKeys.Length; i++)
        {
            _sampleQueues.TryAdd(channelKeys[i], new ConcurrentQueue<Sample>());
        }

        return new BoardRunContext(board, channels, offsets, sampleSizes, channelKeys);
    }

    private void StartBoardAcquisition(BoardRunContext ctx)
    {
        var cts = new CancellationTokenSource();
        _ctsList.Add(cts);

        var task = Task.Run(() => AcquireDataLoop(
            ctx.Board,
            ctx.Channels,
            ctx.Offsets,
            ctx.SampleSizes,
            ctx.ChannelKeys,
            cts.Token), cts.Token);

        _acquisitionTasks.Add(task);
    }

    public async Task StartAcquisitionAsync(IEnumerable<Channel> selectedChannels)
    {
        if (_isRunning) await StopAcquisitionAsync();

        _acquisitionTasks.Clear();
        _ctsList.Clear();

        var selectedBoardIds = selectedChannels.Select(c => c.BoardID).Distinct();
        var selectedBoards = _enclosure.Boards.Where(b => selectedBoardIds.Contains(b.Id)).ToList();

        foreach (var board in selectedBoards)
        {
            board.Reset();
            board.SetAcquisitionProperties();
            board.ActivateChannels(selectedChannels.Where(c => c.BoardID == board.Id));
            board.Update();
            board.RefreshScanDescriptor();
        }

        var channelsByBoard = selectedChannels.GroupBy(c => c.BoardID);

        foreach (var boardGroup in channelsByBoard)
        {
            var ctx = PrepareBoardRunContext(boardGroup);
            if (ctx is null) continue;
            StartBoardAcquisition(ctx);
        }

        _isRunning = true;
    }

    public async Task StopAcquisitionAsync()
    {
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

    public Dictionary<string, Sample[]> DrainSamples(int maxPerChannel = 1000)
    {
        var result = new Dictionary<string, Sample[]>();
        int minCount = int.MaxValue;

        foreach (var (key, q) in _sampleQueues)
        {
            if (q.IsEmpty) continue;
            minCount = Math.Min(q.Count, minCount);
        }

        if (minCount == int.MaxValue)
            return result;

        int toTake = Math.Min(minCount, maxPerChannel);

        foreach (var (key, q) in _sampleQueues)
        {
            if (q.IsEmpty) continue;

            var samples = new Sample[toTake];
            for (int i = 0; i < toTake; i++)
            {
                if (q.TryDequeue(out var sample))
                {
                    samples[i] = sample;
                }
                else
                {
                    break;
                }
            }
            result[key] = samples;
        }
        return result;
    }

    private static uint ReadDiscreteSample(nint samplePos)
    {
        int raw = Marshal.ReadByte(samplePos);
        return (uint)(raw & 0x1);
    }

    private async Task AcquireDataLoop(
        Board board,
        List<Channel> selectedChannels,
        int[] offsets,
        int[] sampleSizes,
        string[] channelKeys,
        CancellationToken token)
    {
        var scanSize = (int)board.ScanSizeBytes;
        var polling_interval = (int)(board.BufferBlockSize / (double)board.SamplingRate * 1000);

        TrionError error;
        (error, var adcDelay) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BOARD_ADC_DELAY);
        Utils.CheckErrorCode(error, $"Failed to get ADC Delay {board.Id}");

        error = TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.START_ACQUISITION, 0);
        Utils.CheckErrorCode(error, $"Failed start acquisition {board.Id}");

        CircularBuffer buffer = new(board.Id);
        int availableSamples = 0;

        long startTicks = DateTime.UtcNow.Ticks;
        long sampleIndex = 0;

        while (!token.IsCancellationRequested)
        {
            (error, availableSamples) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
            Utils.CheckErrorCode(error, $"Failed to get available samples {board.Id}, {availableSamples}");

            if (availableSamples <= 0)
            {
                await Task.Delay(polling_interval, token);
                continue;
            }

            availableSamples -= adcDelay;
            if (availableSamples <= 0)
            {
                await Task.Delay(polling_interval, token);
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
                if (readPos >= buffer.EndPosition)
                    readPos -= buffer.Size;

                for (int c = 0; c < selectedChannels.Count; ++c)
                {
                    var channel = selectedChannels[c];
                    nint samplePos = (nint)(readPos + offsets[c]);
                    int sampleSize = sampleSizes[c];

                    if (channel.Type == Channel.ChannelType.Digital)
                    {
                        uint value = ReadDiscreteSample(samplePos);
                        sampleLists[c].Add(value);
                    }
                    else if (channel.Type == Channel.ChannelType.Analog)
                    {
                        double value = ReadAnalogSample(samplePos, sampleSize);
                        sampleLists[c].Add(value);
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported channel type: {channel.Type}");
                    }
                }

                readPos += scanSize;
            }

            TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, availableSamples);

            for (int i = 0; i < availableSamples; i++)
            {
                long tsTicks = startTicks + ((sampleIndex + i) * TimeSpan.TicksPerSecond) / board.SamplingRate;
                var ts = new DateTime(tsTicks, DateTimeKind.Utc);
                double elapsedSeconds = (sampleIndex + i) / (double)board.SamplingRate;

                for (int c = 0; c < selectedChannels.Count; ++c)
                {
                    var key = channelKeys[c];
                    var q = _sampleQueues[key];
                    q.Enqueue(new Sample(sampleLists[c][i], ts, elapsedSeconds));
                }
            }

            sampleIndex += availableSamples;
        }

        TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, availableSamples);
        Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.STOP_ACQUISITION, 0), $"Failed to stop acquisition {board.Id}");
    }

    private unsafe static double ReadAnalogSample(nint samplePos, int sampleSize)
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
        double value = (double)raw / (double)(signBit - 1) * 10.0;
        return value;
    }

}