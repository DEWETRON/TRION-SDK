using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

public class AcquisitionManager(Enclosure enclosure) : IDisposable
{
    private readonly Enclosure _enclosure = enclosure;
    private readonly List<Task> _acquisitionTasks = [];
    private readonly List<CancellationTokenSource> _ctsList = [];
    public bool _isRunning = false;

    public void StartAcquisition(IEnumerable<Channel> selectedChannels, Action<string, IEnumerable<double>> onSamplesReceived)
    {
        if (_isRunning)
            StopAcquisition();

        _acquisitionTasks.Clear();
        _ctsList.Clear();

        var selectedBoardIds = selectedChannels.Select(c => c.BoardID).Distinct();
        var selectedBoards = _enclosure.Boards.Where(b => selectedBoardIds.Contains(b.Id)).ToList();

        // Board and channel setup
        foreach (var board in selectedBoards)
        {
            if (!board.IsOpen)
            {
                Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.OPEN_BOARD, 0), "Failed to open board");
                board.IsOpen = true;
            }
            board.ResetBoard();
            board.SetAcquisitionProperties();
            Utils.CheckErrorCode(TrionApi.DeWeSetParamStruct($"BoardID{board.Id}/AIAll", "Used", "False"), $"Failed to reset board {board.Id}");
            var selectedChannelsForBoard = selectedChannels.Where(c => c.BoardID == board.Id).ToList();
            foreach (var channel in selectedChannelsForBoard)
            {
                Utils.CheckErrorCode(TrionApi.DeWeSetParamStruct($"BoardID{channel.BoardID}/{channel.Name}", "Used", "True"), $"Failed to set channel used {channel.Name}");
                Utils.CheckErrorCode(TrionApi.DeWeSetParamStruct($"BoardID{channel.BoardID}/{channel.Name}", "Range", "10 V"), $"Failed to set channel range {channel.Name}");
            }
            board.UpdateBoard();

            (var error, board.ScanDescriptorXml) = TrionApi.DeWeGetParamStruct_String($"BoardID{board.Id}", "ScanDescriptor_V3");
            Utils.CheckErrorCode(error, $"Failed to get scan descriptor {board.Id}");
            board.ScanDescriptorDecoder = new ScanDescriptorDecoder(board.ScanDescriptorXml);
            board.ScanSizeBytes = board.ScanDescriptorDecoder.ScanSizeBytes;
        }

        // Start acquisition tasks
        var channelsByBoard = selectedChannels.GroupBy(c => c.BoardID);
        foreach (var boardGroup in channelsByBoard)
        {
            var board = _enclosure.Boards.First(b => b.Id == boardGroup.Key);
            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);
            var task = Task.Run(() => AcquireDataLoop(board, [.. boardGroup], onSamplesReceived, cts.Token), cts.Token);
            _acquisitionTasks.Add(task);
        }

        _isRunning = true;
    }

    public void StopAcquisition()
    {
        foreach (var board in _enclosure.Boards.Where(b => b.IsOpen))
        {
            var error = TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.STOP_ACQUISITION, 0);
            Utils.CheckErrorCode(error, $"Failed stop acquisition {board.Id}");
            error = TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.CLOSE_BOARD, 0);
            Utils.CheckErrorCode(error, $"Failed close board {board.Id}");
            board.IsOpen = false;
        }
        foreach (var cts in _ctsList)
        {
            cts.Cancel();
        }
        Task.WaitAll(_acquisitionTasks.ToArray(), 1000);
        _acquisitionTasks.Clear();
        _ctsList.Clear();
        _isRunning = false;
    }

    private void AcquireDataLoop(Board board, List<Channel> selectedChannels, Action<string, IEnumerable<double>> onSamplesReceived, CancellationToken token)
    {
        var scanSize = (int)board.ScanSizeBytes;
        var scanDescriptor = board.ScanDescriptorDecoder;
        var polling_interval = (int)(board.BufferBlockSize / (double)board.SamplingRate * 1000);

        TrionError error;
        (error, var adc_delay) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BOARD_ADC_DELAY);
        Utils.CheckErrorCode(error, $"Failed to get ADC Delay {board.Id}");

        error = TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.START_ACQUISITION, 0);
        Utils.CheckErrorCode(error, $"Failed start acquisition {board.Id}");

        CircularBuffer buffer = new(board.Id);

        while (!token.IsCancellationRequested)
        {
            (error, var available_samples) = TrionApi.DeWeGetParam_i32(board.Id, TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
            Utils.CheckErrorCode(error, $"Failed to get available samples {board.Id}, {available_samples}");
            if (available_samples <= 0)
            {
                Thread.Sleep(polling_interval);
                continue;
            }

            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                Thread.Sleep(10);
                continue;
            }
            (error, var read_pos) = TrionApi.DeWeGetParam_i64(board.Id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            Utils.CheckErrorCode(error, $"Failed to get actual sample position {board.Id}");

            read_pos += adc_delay * scanSize;

            // Prepare a dictionary to collect samples for each channel
            var channelSamples = new Dictionary<string, List<double>>();
            foreach (var channel in selectedChannels)
                channelSamples[channel.Name] = new List<double>(available_samples);

            for (int i = 0; i < available_samples; ++i)
            {
                if (read_pos >= buffer.EndPosition)
                    read_pos -= buffer.Size;

                foreach (var channel in selectedChannels)
                {
                    var channelInfo = scanDescriptor.Channels.FirstOrDefault(c => c.Name == channel.Name);
                    if (channelInfo == null) continue;

                    var offset_bytes = (int)channelInfo.SampleOffset / 8;
                    var samplePos = read_pos + offset_bytes;
                    int raw = Marshal.ReadInt32((IntPtr)samplePos);

                    int sampleSize = (int)channelInfo.SampleSize;
                    int bitmask = (1 << sampleSize) - 1;
                    raw &= bitmask;
                    int signBit = 1 << (sampleSize - 1);
                    if ((raw & signBit) != 0)
                        raw |= ~bitmask;
                    double value = (double)raw / (double)(signBit - 1) * 10.0;

                    channelSamples[channel.Name].Add(value);
                }
                read_pos += scanSize;
            }

            Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(board.Id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples), "Failed to free buffer");


            // Call the callback for each channel
            foreach (var kvp in channelSamples)
                onSamplesReceived(kvp.Key, kvp.Value);
        }
        StopAcquisition();
    }

    public void Dispose()
    {
        StopAcquisition();
        Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0), "Failed to close Boards");
    }
}