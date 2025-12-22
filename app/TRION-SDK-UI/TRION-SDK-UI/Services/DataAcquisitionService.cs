using System.Diagnostics;
using System.Runtime.InteropServices;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

public class DataAcquisitionService
{
    public Task StartAcquisition(Board board, List<Channel> channels, Action<string, IEnumerable<double>> onSamplesReceived, CancellationToken token)
    {
        // Prepare required arguments for AcquireDataLoop
        int[] offsets = channels.Select(c => (int)c.SampleOffset).ToArray();
        int[] sampleSizes = channels.Select(c => (int)c.SampleSize).ToArray();
        string[] channelKeys = channels.Select(c => c.Name ?? $"Channel_{c.Index}").ToArray();

        // Move AcquireDataLoop logic here
        return Task.Run(() => AcquireDataLoop(board, channels, offsets, sampleSizes, channelKeys, onSamplesReceived, token), token);
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

    private static uint ReadDiscreteSample(nint samplePos)
    {
        int raw = Marshal.ReadByte(samplePos);
        return (uint)(raw & 0x1); // only the least significant bit
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

}