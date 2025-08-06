using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRION_SDK_UI.Models
{
    public enum BoardType
    {
        Unknown = 0,
        Analog = 1,
        Digital = 2,
        Counter = 3
    }
    public class Board
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public bool IsActive { get; set; }
        public BoardPropertyModel BoardProperties { get; set; }
        public string ScanDescriptor { get; set; }
        public List<Channel> Channels { get; set; } = new();
        public uint ScanSizeBytes { get; set; }

        public void ReadScanDescriptor()
        {
            if (string.IsNullOrWhiteSpace(ScanDescriptor))
                return;

            var decoder = new ScanDescriptorDecoder(ScanDescriptor);
            Channels = decoder.Channels.ToList();
            ScanSizeBytes = decoder.ScanSizeBytes;
        }

        public Board(int id, string name, bool isActive, BoardPropertyModel boardProperties, string scanDescriptor)
        {
            Id = id;
            Name = name;
            IsActive = isActive;
            BoardProperties = boardProperties;
            ScanDescriptor = scanDescriptor;
            Channels = new List<Channel>();
            ScanSizeBytes = 0; // Explicitly set to default
        }
    }
}
