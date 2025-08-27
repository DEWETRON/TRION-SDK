using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    public enum BoardType
    {
        Unknown = 0,
        Analog = 1,
        Digital = 2,
        Counter = 3
    }
    public class Board()
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsOpen { get; set; }
        public BoardPropertyModel? BoardProperties { get; set; }
        public List<Channel> Channels { get; set; } = [];
        public uint ScanSizeBytes { get; set; }
        public ScanDescriptorDecoder? ScanDescriptorDecoder { get; set; }
        public string ScanDescriptorXml { get; set; } = string.Empty;
        public int BufferBlockSize { get; set; }
        public int SamplingRate { get; set; }
        public int BufferBlockCount { get; set; } 
        public bool IsAcquiring { get; set; }

        public void SetBoardProperties()
        {
            Id = BoardProperties.GetBoardID();
            Name = BoardProperties.GetBoardName();
        }

        public void SetAcquisitionProperties(string operationMode = "Slave",
                                             string externalTrigger = "False",
                                             string externalClock = "False",
                                             int sampleRate = 2000,
                                             int buffer_block_size = 200,
                                             int buffer_block_count = 50)
        {
            SamplingRate = sampleRate;
            BufferBlockCount = buffer_block_count;
            BufferBlockSize = buffer_block_size;
            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "OperationMode", operationMode);
            error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtTrigger", externalTrigger);
            error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtClk", externalClock);
            error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "SampleRate", sampleRate.ToString());

            error |= TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_SIZE, buffer_block_size);
            error |= TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_COUNT, buffer_block_count);

            Utils.CheckErrorCode(error, $"Failed to set acquisition properties for board {Id}");
        }

        public void ActivateChannels(IEnumerable<Channel> channelsToActivate)
        {
            // First, deactivate all channels on this board
            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AIAll", "Used", "False");

            // Then, activate only the specified channels
            foreach (var channel in channelsToActivate)
            {
                Debug.WriteLine($"TEST channelsToActivate {channel.Name}");
                if (channel.BoardID != Id)
                {
                    return;
                }
                error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/{channel.Name}", "Used", "True");
                Debug.WriteLine($"TEST Used: BoardID{Id}/{channel.Name}");
                error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/{channel.Name}", "Range", "10 V");
                error |= TrionApi.DeWeSetParam_i32(Id, TrionCommand.RESET_BOARD, 0);
                
            }
            ScanDescriptorXml = TrionApi.DeWeGetParamStruct_String($"BoardID{Id}", "ScanDescriptor_V3").value;
            Debug.WriteLine($"TEST XML {ScanDescriptorXml}");
            ScanDescriptorDecoder = new ScanDescriptorDecoder(ScanDescriptorXml);
            ScanSizeBytes = ScanDescriptorDecoder.ScanSizeBytes;


            SetAcquisitionProperties(sampleRate: 2000, buffer_block_size: 200, buffer_block_count: 50);
            UpdateBoard();
            Utils.CheckErrorCode(error, $"Failed to activate channels for board {Id}");
        }

        public void ResetBoard()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error, $"Failed to reset board {Id}");
        }

        public void UpdateBoard()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error, $"Failed to update board {Id}");
        }
    }
}
