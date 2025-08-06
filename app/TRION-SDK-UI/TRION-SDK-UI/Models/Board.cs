using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trion;

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
        public List<Channel> Channels { get; set; } = new();
        public uint ScanSizeBytes { get; set; }
        public ScanDescriptorDecoder? ScanDescriptorDecoder { get; set; }
        public string ScanDescriptorXml { get; set; } = string.Empty;
        public void ReadScanDescriptor(string scanDescriptorXml)
        {
            if (string.IsNullOrWhiteSpace(scanDescriptorXml))
                return;

            ScanDescriptorDecoder = new ScanDescriptorDecoder(scanDescriptorXml);
            Channels = ScanDescriptorDecoder.Channels
                .Select(c => new Channel
                {
                    Name = c.Name ?? string.Empty,
                    ChannelType = c.Type,
                    Index = c.Index,
                    SampleSize = c.SampleSize,
                    SampleOffset = c.SampleOffset
                })
                .ToList();
            ScanSizeBytes = ScanDescriptorDecoder.ScanSizeBytes;
        }

        public void SetBoardProperties()
        {
            System.Diagnostics.Debug.WriteLine($"TRION_API: Setting board properties for board {BoardProperties.GetBoardName()}");
            Id = BoardProperties.GetBoardID();
            Name = BoardProperties.GetBoardName();
            IsActive = true;
        }
    }
}
