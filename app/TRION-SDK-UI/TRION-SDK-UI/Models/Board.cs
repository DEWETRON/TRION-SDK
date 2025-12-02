using System.Diagnostics;
using Trion;
using TRION_SDK_UI.POCO;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class Board()
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public BoardPropertyModel? BoardProperties { get; init; }
        public List<Channel> Channels { get; set; } = [];
        public ScanDescriptorDecoder? ScanDescriptor { get; set; }
        public string ScanDescriptorXml { get; set; } = string.Empty;
        public int BufferBlockSize { get; set; }
        public int SamplingRate { get; set; }
        public int BufferBlockCount { get; set; }
        public string OperationMode { get; set; }
        public string ExternalTrigger { get; set; }
        public string ExternalClock { get; set; }
        public bool IsAcquiring { get; set; }
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
        public void SetOperationMode(bool update)
        {
            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "OperationMode", OperationMode);
            Utils.CheckErrorCode(error, $"Failed to set operation mode for board {Id}");
            if (update) Update();
        }

        public void SetExternalTrigger(bool update)
        {
            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtTrigger", ExternalTrigger);
            Utils.CheckErrorCode(error, $"Failed to set external trigger for board {Id}");
            if (update) Update();
        }

        public void SetExternalClock(bool update)
        {
            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtClk", ExternalClock);
            Utils.CheckErrorCode(error, $"Failed to set external clock for board {Id}");
            if (update) Update();
        }

        public void UpdateBuffer(bool update)
        {
            BufferBlockSize = (int)(SamplingRate * 0.1);
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_SIZE, BufferBlockSize);
            Utils.CheckErrorCode(error, $"Failed to set buffer block size for board {Id}");

            error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_COUNT, BufferBlockCount);
            Utils.CheckErrorCode(error, $"Failed to set buffer block count for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "SampleRate", "2000");
            Utils.CheckErrorCode(error, $"Failed to set sampling rate for board {Id}");

            if (update) Update();
        }

        // TODO: make more robust
        public void UpdateAcquisitionProperties()
        {
            Debug.WriteLine($"Setting sampling rate to {SamplingRate} Hz on board {Id}");
            SetOperationMode(false);
            SetExternalClock(false);
            SetExternalTrigger(false);
            UpdateBuffer(false);

            Update();
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