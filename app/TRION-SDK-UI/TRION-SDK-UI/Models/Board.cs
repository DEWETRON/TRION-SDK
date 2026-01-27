using System.Diagnostics;
using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public class Board()
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required BoardPropertyParser BoardProperties { get; init; }
        public required List<Channel> Channels { get; set; } = [];
        public ScanDescriptorDecoder? ScanDescriptor { get; set; }
        public string ScanDescriptorXml { get; set; } = string.Empty;
        private int BufferBlockSize { get; set; }
        public required int SamplingRate { get; set; }
        public required int BufferBlockCount { get; set; }
        public required string OperationMode { get; set; }
        public required string ExternalTrigger { get; set; }
        public required string ExternalClock { get; set; }
        public bool IsAcquiring { get; set; }
        public int SampleRateDivider { get; set; }
        public string? ResolutionAI { get; set; }
        public void RefreshScanDescriptor()
        {
            Debug.WriteLine($"Refreshing scan descriptor for board {Id}");
            Debug.WriteLine($"Current ScanDescriptorXml: {ScanDescriptorXml}");
            (var error, ScanDescriptorXml) = TrionApi.DeWeGetParamStruct_String($"BoardID{Id}", "ScanDescriptor_V3");
            Utils.CheckErrorCode(error, $"Failed to get scan descriptor {Id}");

            ScanDescriptor = new ScanDescriptorDecoder(ScanDescriptorXml);
            Debug.WriteLine($"Updated ScanDescriptorXml: {ScanDescriptorXml}");
        }

        public void SetAcqProp(string str, string value)
        {
            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", str, value);
            Utils.CheckErrorCode(error, $"Failed to set acquisition property '{str}' for board {Id}");
            Update();
        }

        public void ActivateChannels(IEnumerable<Channel> selectedChannels)
        {
            DeactivateAllChannels(Id);

            foreach (var channel in selectedChannels)
            {
                channel.Activate();
            }
            Update();
        }

        private static void DeactivateAllChannels(int boardId)
        {
            var error = TrionApi.DeWeSetParamStruct($"BoardID{boardId}/AIAll", "Used", "False");
            Utils.CheckErrorCode(error, $"Failed to deactivate all analog channels on board {boardId}");
        }

        public void UpdateBuffer(bool update)
        {
            const double PollingInterval = 0.1; // 100ms target

            int BufferBlockSize = (int)(SamplingRate * PollingInterval);
            var test = Math.Max(1, BufferBlockSize);

            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_SIZE, test);
            Utils.CheckErrorCode(error, $"Failed to set buffer block size for board {Id}");

            error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_COUNT, BufferBlockCount);
            Utils.CheckErrorCode(error, $"Failed to set buffer block count for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "SampleRate", SamplingRate.ToString());
            Utils.CheckErrorCode(error, $"Failed to set sampling rate for board {Id}");

            if (update) Update();
        }

        public void SetResolutionAI(bool update)
        {
            if (string.IsNullOrEmpty(ResolutionAI)) return;

            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ResolutionAI", ResolutionAI);
            if (error > 0) 
            {
                Debug.WriteLine($"Failed to set ResolutionAI to {ResolutionAI} on board {Id}. Error: {error}");
            }

            if (update) Update();
        }

        public void UpdateAcquisitionProperties()
        {
            UpdateBuffer(false);
            Debug.WriteLine($"Setting sampling rate to {SamplingRate} Hz on board {Id}");
            SetAcqProp("OperationMode", OperationMode);
            SetAcqProp("ExtTrigger", ExternalTrigger);
            SetAcqProp("ExtClk", ExternalClock);
            //SetAcqProp("ResolutionAI", ResolutionAI ?? "");
            Update();
        }

        public void Reset()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error, $"Failed to reset board {Id}");
            
            ScanDescriptor = null;
            ScanDescriptorXml = string.Empty;
            
            Update();
        }

        public void Update()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error, $"Failed to update board {Id}");
        }
    }
}