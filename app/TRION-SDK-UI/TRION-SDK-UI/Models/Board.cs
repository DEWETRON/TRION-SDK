using System.Diagnostics;
using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class Board()
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsOpen { get; set; }
        public BoardPropertyModel? BoardProperties { get; init; }
        public List<Channel> Channels { get; set; } = [];
        public ScanDescriptorDecoder? ScanDescriptor { get; set; }
        public string ScanDescriptorXml { get; set; } = string.Empty;
        public int BufferBlockSize { get; set; }
        public int SamplingRate { get; set; }
        public int BufferBlockCount { get; set; }
        public void RefreshScanDescriptor()
        {
            (var error, ScanDescriptorXml) = TrionApi.DeWeGetParamStruct_String($"BoardID{Id}", "ScanDescriptor_V3");
            Utils.CheckErrorCode(error, $"Failed to get scan descriptor {Id}");

            ScanDescriptor = new ScanDescriptorDecoder(ScanDescriptorXml);
        }

        public void ActivateChannels(IEnumerable<Channel> selectedChannels)
        {
            DeactivateAllChannels(Id);

            foreach (var channel in selectedChannels)
            {
                channel.Activate();
            }
        }

        private static void DeactivateAllChannels(int boardId)
        {
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{boardId}/AIAll", "Used", "False"),
                $"Failed to deactivate all analog channels on board {boardId}");
        }
        // TODO: make more robust
        public void SetAcquisitionProperties(string operationMode = "Slave",
                                             string externalTrigger = "False",
                                             string externalClock = "False",
                                             int sampleRate = 2_000,
                                             int buffer_block_size = 200,
                                             int buffer_block_count = 50)
        {
            Debug.WriteLine($"Setting sampling rate to {sampleRate} Hz on board {Id}");
            SamplingRate = sampleRate;
            BufferBlockCount = buffer_block_count;
            BufferBlockSize = (int)(SamplingRate * 0.1); // 100 ms buffer

            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "OperationMode", operationMode);
            Utils.CheckErrorCode(error, $"Failed to set operation mode for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtTrigger", externalTrigger);
            Utils.CheckErrorCode(error, $"Failed to set external trigger for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtClk", externalClock);
            Utils.CheckErrorCode(error, $"Failed to set external clock for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "SampleRate", SamplingRate.ToString());
            Utils.CheckErrorCode(error, $"Failed to set sample rate for board {Id}");

            error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_SIZE, BufferBlockSize);
            Utils.CheckErrorCode(error, $"Failed to set buffer block size for board {Id}");

            error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_COUNT, BufferBlockCount);
            Utils.CheckErrorCode(error, $"Failed to set buffer block count for board {Id}");
        }

        public void Reset()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error, $"Failed to reset board {Id}");
        }

        public void Update()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error, $"Failed to update board {Id}");
        }
    }
}