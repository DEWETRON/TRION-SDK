using System;
using System.Collections.Generic;
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
    public class Board(BoardPropertyModel BoardProperties)
    {
        public int Id { get; set; }

        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public BoardPropertyModel BoardProperties { get; set; } = BoardProperties;
        public List<Channel> Channels { get; set; } = [];
        public uint ScanSizeBytes { get; set; }
        public ScanDescriptorDecoder? ScanDescriptorDecoder { get; set; }
        public string ScanDescriptorXml { get; set; } = string.Empty;

        public void ReadScanDescriptor(string scanDescriptorXml)
        {
            if (string.IsNullOrWhiteSpace(scanDescriptorXml))
            {
                System.Diagnostics.Debug.WriteLine($"Return Early");
                return;
            }
            System.Diagnostics.Debug.WriteLine($"BoardID {Id}");

            ScanDescriptorDecoder = new ScanDescriptorDecoder(scanDescriptorXml);
            Channels = [.. ScanDescriptorDecoder.Channels
                .Select(c => new Channel
                {
                    BoardID = Id,
                    Name = c.Name ?? string.Empty,
                    ChannelType = c.Type,
                    Index = c.Index,
                    SampleSize = c.SampleSize,
                    SampleOffset = c.SampleOffset
                })];
            ScanSizeBytes = ScanDescriptorDecoder.ScanSizeBytes;
        }

        public void SetBoardProperties()
        {
            Id = BoardProperties.GetBoardID();
            Name = BoardProperties.GetBoardName();
            System.Diagnostics.Debug.WriteLine($"Board ID: {Id}, Name: {Name}");
            IsActive = true;
        }

        public void SetAcquisitionProperties(string operationMode = "Slave",
                                             string externalTrigger = "False",
                                             string externalClock = "False",
                                             string sampleRate = "2000",
                                             int buffer_block_size = 200,
                                             int buffer_block_count = 50)
        {
            var error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "OperationMode", operationMode);
            error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtTrigger", externalTrigger);
            error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtClk", externalClock);
            error |= TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "SampleRate", sampleRate);

            error |= TrionApi.DeWeSetParam_i32(Id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, buffer_block_size);
            error |= TrionApi.DeWeSetParam_i32(Id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, buffer_block_count);

            Utils.CheckErrorCode(error, $"Failed to set acquisition properties for board {Id}");
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
