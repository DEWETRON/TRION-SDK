using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

public class AcquisitionManager(Enclosure enclosure)
{
    /// <summary>The discovered enclosure which owns the boards used for acquisition.</summary>
    private readonly Enclosure _enclosure = enclosure;

    /// <summary>Background acquisition tasks (one per board with selected channels).</summary>
    private readonly List<Task> _acquisitionTasks = [];

    /// <summary>Cancellation tokens for running acquisition tasks.</summary>
    private readonly List<CancellationTokenSource> _ctsList = [];

    /// <summary>True while at least one board is actively acquiring.</summary>
    public bool _isRunning = false;

    // Thread-safe queues per channel key
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Sample>> _sampleQueues = new();

    // Encapsulates all precomputed, per-board data needed to run the acquisition loop.
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

        // Offsets are bit-based in XML; convert to bytes here.
        var offsets = channelInfos.Select(ci => (int)ci!.SampleOffset / 8).ToArray();
        var sampleSizes = channelInfos.Select(ci => (int)ci!.SampleSize).ToArray();
        var channelKeys = channels.Select(ch => $"{ch.BoardID}/{ch.Name}").ToArray();

        // Ensure per-channel queues exist (idempotent)
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

    public async Task StartAcquisitionAsync(IEnumerable<Channel> selectedChannels, Action<string, IEnumerable<double>> onSamplesReceived)
    {
        if (_isRunning) await StopAcquisitionAsync();

        _acquisitionTasks.Clear();
        _ctsList.Clear();

        // Open/configure only boards that own at least one selected channel
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

        // Prepare and start one acquisition task per board.
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
        // Request all loops to exit
        foreach (var cts in _ctsList)
        {
            cts.Cancel();
        }
        try
        {
            // Await all tasks; exceptions are traced but suppressed to keep shutdown robust
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

    // Drain up to maxPerChannel samples per channel
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

    /// <summary>
    /// Read the LSB from a discrete (digital) sample position.
    /// Assumes one bit indicates the digital state.
    /// </summary>
    private static uint ReadDiscreteSample(nint samplePos)
    {
        int raw = Marshal.ReadByte(samplePos);
        return (uint)(raw & 0x1); // only the least significant bit
    }

    /// <summary>
    /// Core acquisition loop for a single board:
    /// - Poll available samples
    /// - Adjust for ADC delay
    /// - Compute the read pointer in the circular buffer
    /// - Decode per-channel values for the available block
    /// - Return buffer space
    /// - Notify UI with per-channel batches
    /// </summary>
    private async Task AcquireDataLoop(
        Board board,
        List<Channel> selectedChannels,
        int[] offsets,
        int[] sampleSizes,
        string[] channelKeys,
        CancellationToken token)
    {
        // Scan size = total bytes per scan across all active channels on this board
        var scanSize = (int)board.ScanSizeBytes;

        // Poll interval heuristic: convert a buffer block duration to milliseconds
        // (Avoids hot-loop polling when no samples are available)
        var polling_interval = (int)(board.BufferBlockSize / (double)board.SamplingRate * 1000);

        // Acquire and apply ADC delay compensation
        TrionError error;
        (error, var adcDelay) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BOARD_ADC_DELAY);
        Utils.CheckErrorCode(error, $"Failed to get ADC Delay {board.Id}");

        // Start board acquisition
        error = TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.START_ACQUISITION, 0);
        Utils.CheckErrorCode(error, $"Failed start acquisition {board.Id}");

        // Convenience wrapper to query end pointer and total size for wrap handling
        CircularBuffer buffer = new(board.Id);
        int availableSamples = 0;

        long startTicks = DateTime.UtcNow.Ticks;
        long sampleIndex = 0;

        while (!token.IsCancellationRequested)
        {
            // Query number of contiguous samples currently available in the circular buffer
            (error, availableSamples) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
            Utils.CheckErrorCode(error, $"Failed to get available samples {board.Id}, {availableSamples}");
            if (availableSamples >= 40_000)
            {
                Debug.WriteLine($"Available Samples {availableSamples}");
            }

            if (availableSamples <= 0)
            {
                // Nothing to read yet; sleep a bit (cooperative with cancellation)
                await Task.Delay(polling_interval, token);
                continue;
            }

            // Compensate ADC pipeline delay to ensure we're only reading settled samples
            availableSamples -= adcDelay;
            if (availableSamples <= 0)
            {
                await Task.Delay(polling_interval, token);
                continue;
            }

            // Compute current read pointer and advance by the adcDelay window
            (error, var readPos) = TrionApi.DeWeGetParam_i64(board.Id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            Utils.CheckErrorCode(error, $"Failed to get actual sample position {board.Id}");

            readPos += adcDelay * scanSize;

            // Prepare per-channel accumulation lists sized to the number of available samples
            var sampleLists = new List<double>[selectedChannels.Count];
            for (int c = 0; c < selectedChannels.Count; ++c)
            {
                sampleLists[c] = new List<double>(availableSamples);
            }

            // Decode the contiguous block
            for (int i = 0; i < availableSamples; ++i)
            {
                // Handle wrap-around of the circular buffer
                if (readPos >= buffer.EndPosition)
                {
                    readPos -= buffer.Size;
                }

                // Extract values for all selected channels from this scan position
                for (int c = 0; c < selectedChannels.Count; ++c)
                {
                    var channel = selectedChannels[c];
                    nint samplePos = (nint)(readPos + offsets[c]);
                    int sampleSize = sampleSizes[c];

                    if (channel.Type == Channel.ChannelType.Digital)
                    {
                        // Digital: read a discrete state (LSB)
                        uint value = ReadDiscreteSample(samplePos);
                        sampleLists[c].Add(value);
                        continue;
                    }
                    else if (channel.Type == Channel.ChannelType.Analog)
                    {
                        // Analog: read signed sample (16/24/32), scale to engineering units (approx. ±10V here)
                        double value = ReadAnalogSample(samplePos, sampleSize);
                        //Stopwatch sw = Stopwatch.StartNew();
                 
                        sampleLists[c].Add(value);

                        //Debug.WriteLine($"ReadAnalogSample took {sw.ElapsedMilliseconds}ms");
                        continue;
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported channel type: {channel.Type}");
                    }
                }

                // Advance to the next scan
                readPos += scanSize;
            }

            TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, availableSamples);
            //Debug.WriteLine($"Board {board.Id} read {availableSamples} samples");

            // Enqueue value+timestamp pairs
            for (int i = 0; i < availableSamples; i++)
            {
                long tsTicks = startTicks + ((sampleIndex + i) * TimeSpan.TicksPerSecond) / board.SamplingRate;
                var ts = new DateTime(tsTicks, DateTimeKind.Utc);

                for (int c = 0; c < selectedChannels.Count; ++c)
                {
                    var key = channelKeys[c];
                    var q = _sampleQueues[key];
                    q.Enqueue(new Sample(sampleLists[c][i], ts));
                }
            }
        }
        sampleIndex += availableSamples;

        // Graceful exit: release any remaining samples and stop acquisition
        TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, availableSamples);
        Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.STOP_ACQUISITION, 0), $"Failed to stop acquisition {board.Id}");
    }

    /// <summary>
    /// Read a signed analog sample at the given position for a specific bit width and scale it.
    /// Assumptions:
    /// - Little-endian order
    /// - Two's complement for signed values
    /// - Scaled to an approximate ±10V full-scale (adjust scaling according to board configuration)
    /// </summary>
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
                    // sign extend from 24-bit
                    if ((raw & 0x800000) != 0)
                    {
                        raw |= unchecked((int)0xFF000000);
                    }
                    break;
                }
            case 32:
                {
                    raw = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<int>((byte*)samplePos);
                    // already 32-bit signed; no sign extension needed
                    break;
                }
            default:
                throw new NotSupportedException($"Unsupported sample size: {sampleSize}");
        }

        // Normalize to [-1, +1] using the MSB as sign bit, then scale to ~±10V.
        // For precise scaling, consider querying "scalevalue"/"scaleoffset" or Range from TRION.
        int signBit = 1 << (sampleSize - 1);
        double value = (double)raw / (double)(signBit - 1) * 10.0;
        return value;
    }

}