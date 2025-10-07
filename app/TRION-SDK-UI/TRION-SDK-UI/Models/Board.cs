using Trion;
using TrionApiUtils;

namespace TRION_SDK_UI.Models
{
    /// <summary>
    /// Represents a single physical TRION board and encapsulates
    /// identification, configuration, channel activation, and acquisition settings.
    /// </summary>
    /// <remarks>
    /// Uses TRION API wrapper calls (TrionApi.*) to communicate with hardware.
    /// The class is stateful: several methods assume that <see cref="BoardProperties"/>
    /// has been set and <see cref="SetBoardProperties"/> has been called to populate
    /// <see cref="Id"/> and <see cref="Name"/>.
    /// </remarks>
    public class Board()
    {
        /// <summary>
        /// Numeric board identifier returned by the TRION API.
        /// Must be initialized via <see cref="SetBoardProperties"/>.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Human-readable board name (e.g., model or slot designation).
        /// Populated via <see cref="SetBoardProperties"/>.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Indicates whether the board is currently opened / initialized at a higher layer.
        /// Not set internally in this class; maintained externally.
        /// </summary>
        public bool IsOpen { get; set; }

        /// <summary>
        /// Parsed board metadata source (XML-backed). Required before calling <see cref="SetBoardProperties"/>.
        /// </summary>
        public BoardPropertyModel? BoardProperties { get; init; }

        /// <summary>
        /// In-memory collection of channel objects associated with this board.
        /// These should originate from <see cref="BoardPropertyModel.GetChannels"/>.
        /// </summary>
        public List<Channel> Channels { get; set; } = [];

        /// <summary>
        /// Total byte size of one complete scan frame (all active channel samples).
        /// Populated after <see cref="RefreshScanDescriptor"/>.
        /// </summary>
        public uint ScanSizeBytes { get; set; }

        /// <summary>
        /// Decoder containing parsed ScanDescriptor XML structure.
        /// </summary>
        public ScanDescriptorDecoder? ScanDescriptorDecoder { get; set; }

        /// <summary>
        /// Raw ScanDescriptor XML string (version 3) retrieved from hardware.
        /// </summary>
        public string ScanDescriptorXml { get; set; } = string.Empty;

        /// <summary>
        /// Hardware acquisition buffer block size (samples per block or bytes depending on API semantics).
        /// Set in <see cref="SetAcquisitionProperties"/>.
        /// </summary>
        public int BufferBlockSize { get; set; }

        /// <summary>
        /// Sampling rate configured for this board (Hz).
        /// Set in <see cref="SetAcquisitionProperties"/>.
        /// </summary>
        public int SamplingRate { get; set; }

        /// <summary>
        /// Number of hardware buffer blocks to allocate.
        /// </summary>
        public int BufferBlockCount { get; set; }

        /// <summary>
        /// Initializes <see cref="Id"/> and <see cref="Name"/> from <see cref="BoardProperties"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="BoardProperties"/> is null.</exception>
        public void SetBoardProperties()
        {
            if (BoardProperties == null)
            {
                throw new InvalidOperationException("BoardProperties must not be null when setting board properties.");
            }
            Id = BoardProperties.GetBoardID();
            Name = BoardProperties.GetBoardName();
        }

        /// <summary>
        /// Retrieves the latest ScanDescriptor (V3) from the TRION API, parses it, and updates
        /// <see cref="ScanDescriptorDecoder"/> plus <see cref="ScanSizeBytes"/>.
        /// </summary>
        /// <remarks>
        /// Should be called after channel activation changes or sample layout modifications.
        /// </remarks>
        public void RefreshScanDescriptor()
        {
            (var error, ScanDescriptorXml) = TrionApi.DeWeGetParamStruct_String($"BoardID{Id}", "ScanDescriptor_V3");
            Utils.CheckErrorCode(error, $"Failed to get scan descriptor {Id}");

            ScanDescriptorDecoder = new ScanDescriptorDecoder(ScanDescriptorXml);
            ScanSizeBytes = ScanDescriptorDecoder.ScanSizeBytes;
        }

        /// <summary>
        /// Deactivates all currently active channels and activates only the provided set.
        /// </summary>
        /// <param name="selectedChannels">Channels to activate (must belong to this board).</param>
        /// <remarks>
        /// This method does not repopulate the <see cref="Channels"/> collection; it operates
        /// only on the channels passed in. Each channel's own <c>Activate()</c> method is invoked.
        /// </remarks>
        public void ActivateChannels(IEnumerable<Channel> selectedChannels)
        {
            DeactivateAllChannels(Id);

            // Activate selected channels (per-channel activation handles type-specific logic).
            foreach (var channel in selectedChannels)
            {
                channel.Activate();
            }
        }

        /// <summary>
        /// Deactivates all channels on the specified board through TRION API calls.
        /// Currently only analog channels are processed; digital support is commented out.
        /// </summary>
        /// <param name="boardId">Target board identifier.</param>
        private static void DeactivateAllChannels(int boardId)
        {
            Utils.CheckErrorCode(
                TrionApi.DeWeSetParamStruct($"BoardID{boardId}/AIAll", "Used", "False"),
                $"Failed to deactivate all analog channels on board {boardId}");
        }

        /// <summary>
        /// Configures acquisition parameters for the board (operation mode, external trigger/clock,
        /// sampling rate, and buffer sizing) via TRION API calls.
        /// </summary>
        /// <param name="operationMode">Operation mode string (e.g., "Slave", "Master").</param>
        /// <param name="externalTrigger">"True"/"False" flag for external trigger usage.</param>
        /// <param name="externalClock">"True"/"False" flag for external clock usage.</param>
        /// <param name="sampleRate">Sampling rate</param>
        /// <param name="buffer_block_size">Size of one buffer block.</param>
        /// <param name="buffer_block_count">Number of buffer blocks.</param>
        /// <remarks>
        /// Persists values into local properties for later reference. Fails fast on any API error.
        /// Consider calling <see cref="Update"/> after changing multiple parameters if required by hardware.
        /// </remarks>
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
            Utils.CheckErrorCode(error, $"Failed to set operation mode for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtTrigger", externalTrigger);
            Utils.CheckErrorCode(error, $"Failed to set external trigger for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "ExtClk", externalClock);
            Utils.CheckErrorCode(error, $"Failed to set external clock for board {Id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{Id}/AcqProp", "SampleRate", sampleRate.ToString());
            Utils.CheckErrorCode(error, $"Failed to set sample rate for board {Id}");

            error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_SIZE, buffer_block_size);
            Utils.CheckErrorCode(error, $"Failed to set buffer block size for board {Id}");

            error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.BUFFER_BLOCK_COUNT, buffer_block_count);
            Utils.CheckErrorCode(error, $"Failed to set buffer block count for board {Id}");
        }

        /// <summary>
        /// Issues a hardware reset command to the board.
        /// </summary>
        /// <remarks>
        /// After a reset, previously configured parameters may need to be reapplied.
        /// </remarks>
        public void Reset()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error, $"Failed to reset board {Id}");
        }

        /// <summary>
        /// Requests the board to apply (commit) any pending parameter changes.
        /// </summary>
        public void Update()
        {
            var error = TrionApi.DeWeSetParam_i32(Id, TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error, $"Failed to update board {Id}");
        }
    }
}