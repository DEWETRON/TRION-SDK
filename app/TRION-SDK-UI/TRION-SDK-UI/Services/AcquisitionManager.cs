using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

public class AcquisitionManager : IDisposable
{
    private readonly Enclosure _enclosure;
    private readonly List<Task> _acquisitionTasks = [];
    private readonly List<CancellationTokenSource> _ctsList = [];
    public bool _isRunning = false;

    public AcquisitionManager(Enclosure enclosure)
    {
        _enclosure = enclosure;
    }

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
        }
        foreach (var channel in selectedChannels)
        {
            Utils.CheckErrorCode(TrionApi.DeWeSetParamStruct($"BoardID{channel.BoardID}/{channel.Name}", "Used", "True"), $"Failed to set channel used {channel.Name}");
            Utils.CheckErrorCode(TrionApi.DeWeSetParamStruct($"BoardID{channel.BoardID}/{channel.Name}", "Range", "10 V"), $"Failed to set channel range {channel.Name}");
        }
        foreach (var board in selectedBoards)
        {
            board.UpdateBoard();
        }

        foreach (var board in selectedBoards)
        {
            (var error, board.ScanDescriptorXml) = TrionApi.DeWeGetParamStruct_String($"BoardID{board.Id}", "ScanDescriptor_V3");
            Utils.CheckErrorCode(error, $"Failed to get scan descriptor {board.Id}");
            board.ScanDescriptorDecoder = new ScanDescriptorDecoder(board.ScanDescriptorXml);
            board.ScanSizeBytes = board.ScanDescriptorDecoder.ScanSizeBytes;
            //Debug.WriteLine($"TEST XML: {board.ScanDescriptorXml}");
        }

        // Start acquisition tasks
        foreach (var channel in selectedChannels)
        {
            var cts = new CancellationTokenSource();
            _ctsList.Add(cts);
            var task = Task.Run(() => AcquireDataLoop(channel, onSamplesReceived, cts.Token), cts.Token);
            _acquisitionTasks.Add(task);
        }

        _isRunning = true;
    }

    public void StopAcquisition()
    {
        foreach (var cts in _ctsList)
        {
            cts.Cancel();
        }
        Task.WaitAll(_acquisitionTasks.ToArray(), 1000);
        _acquisitionTasks.Clear();
        _ctsList.Clear();
        TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0);
        _isRunning = false;
    }

    private void AcquireDataLoop(Channel selectedChannel, Action<string, IEnumerable<double>> onSamplesReceived, CancellationToken token)
    {
        var board = _enclosure.Boards.First(b => b.Id == selectedChannel.BoardID);
        var scanSize = (int)board.ScanSizeBytes;
        var scanDescriptor = board.ScanDescriptorDecoder;
        var channelInfo = scanDescriptor.Channels.FirstOrDefault(c => c.Name == selectedChannel.Name);
        var polling_interval = (int)(board.BufferBlockSize / (double)board.SamplingRate * 1000);

        TrionError error;
        if (channelInfo == null) return;

        var board_id = selectedChannel.BoardID;
        (error, var adc_delay) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BOARD_ADC_DELAY);
        Utils.CheckErrorCode(error, $"Failed to get ADC Delay {board_id}");

        if (board.IsAcquiring)
        {
            error = TrionApi.DeWeSetParam_i32(board_id, TrionCommand.START_ACQUISITION, 0);
            Utils.CheckErrorCode(error, $"Failed start acquisition {board_id}");
        }

        CircularBuffer buffer = new(board_id);

        Debug.WriteLine($"AcquireDataLoop started for channel: {selectedChannel.Name}");
        Debug.WriteLine($"Sample Size {(int)channelInfo.SampleSize}, Sample Offset {(int)channelInfo.SampleOffset / 8}");

        while (!token.IsCancellationRequested)
        {
            (error, var available_samples) = TrionApi.DeWeGetParam_i32(board_id, TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
            Utils.CheckErrorCode(error, $"Failed to get available samples {board_id}, {available_samples}");
            if (available_samples <= 0)
            {
                Thread.Sleep(polling_interval);
            }

            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                Thread.Sleep(10);
                continue;
            }
            (error, var read_pos) = TrionApi.DeWeGetParam_i64(board_id, TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
            Utils.CheckErrorCode(error, $"Failed to get actual sample position {board_id}");

            read_pos += adc_delay * scanSize;

            List<double> tempValues = new(available_samples);
            // loop over available samples
            for (int i = 0; i < available_samples; ++i)
            {
                if (read_pos >= buffer.EndPosition)
                {
                    read_pos -= buffer.Size;
                }
                // calculate the position of the sample in memory
                var offset_bytes = (int)channelInfo.SampleOffset / 8;
                var samplePos = read_pos + offset_bytes;

                // read the raw data
                int raw = Marshal.ReadInt32((IntPtr)samplePos);

                // extract the actual sample bits
                int sampleSize = (int)channelInfo.SampleSize;
                int bitmask = (1 << sampleSize) - 1; // = 0xFFFFFF
                raw &= bitmask; // keeps only the lower 24 bits

                // general sign extension for N-bit signed value
                int signBit = 1 << (sampleSize - 1);
                if ((raw & signBit) != 0)
                    raw |= ~bitmask;

                // scale to engineering units (i guess the range needs to be adjustable)
                double value = (double)raw / (double)(signBit - 1) * 10.0;

                // store the result
                tempValues.Add(value);

                // move to the next sample in the buffer
                read_pos += scanSize;            
            }
            TrionApi.DeWeSetParam_i32(board_id, TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);

            Debug.WriteLine("TEST");
            onSamplesReceived(selectedChannel.Name, tempValues);
        }
        StopAcquisition();
    }

    public void Dispose()
    {
        StopAcquisition();
        Utils.CheckErrorCode(TrionApi.DeWeSetParam_i32(0, TrionCommand.CLOSE_BOARD_ALL, 0), "Failed to close Boards");
    }
}