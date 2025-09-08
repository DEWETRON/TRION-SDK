using System.Diagnostics;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

public class DataAcquisitionService
{
    public Task StartAcquisition(Board board, List<Channel> channels, Action<string, IEnumerable<double>> onSamplesReceived, CancellationToken token)
    {
        // Move AcquireDataLoop logic here
        return Task.Run(() => AcquireDataLoop(board, channels, onSamplesReceived, token), token);
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
                //await Task.Delay(polling_interval, token);
                continue;
            }

            available_samples -= adc_delay;
            if (available_samples <= 0)
            {
                //await Task.Delay(polling_interval, token);
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
                    var channel = selectedChannels[c];
                    nint samplePos = (nint)(read_pos + offsets[c]);
                    int sampleSize = sampleSizes[c];
                    if (channel.Type == Channel.ChannelType.Digital)
                    {
                        uint value = ReadDiscreteSample(samplePos);
                        sampleLists[c].Add(value);
                        continue;
                    }
                    else if (channel.Type == Channel.ChannelType.Analog)
                    {
                        double value = ReadSample(samplePos, sampleSize);
                        sampleLists[c].Add(value);
                        continue;
                    }
                    else
                    {
                        throw new NotSupportedException($"Unsupported channel type: {channel.Type}");
                    }
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
}