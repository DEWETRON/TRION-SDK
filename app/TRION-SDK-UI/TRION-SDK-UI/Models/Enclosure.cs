using System.Collections.ObjectModel;
using Trion;
using TRION_SDK_UI.Models;
using TrionApiUtils;

/// <summary>
/// Represents a physical TRION enclosure (chassis) that hosts one or more boards.
/// Provides helper methods to open boards, read their metadata (BoardProperties + ScanDescriptor),
/// and populate a bindable collection of <see cref="Board"/> instances for UI / acquisition logic.
/// </summary>
/// <remarks>
/// Lifecycle:
/// 1. Call <see cref="Init"/> with the detected (or expected) number of boards.
/// 2. Each board is opened via the TRION API (OPEN_BOARD) and its XML metadata queried.
/// 3. A <see cref="Board"/> object is created with parsed channels and initial scan descriptor XML.
/// This class does not yet handle board removal, re-enumeration, or error recovery beyond
/// throwing on first API failure (via <c>Utils.CheckErrorCode</c>).
/// Thread-safety: Not thread-safe; manipulate from a single (UI or controller) thread.
/// </remarks>
public class Enclosure
{
    /// <summary>
    /// Human-readable enclosure name as reported by the TRION system XML (SystemInfo/EnclosureInfo/Name).
    /// Populated during <see cref="Init"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Collection of boards currently opened in this enclosure.
    /// Observable for UI binding (e.g., .NET MAUI CollectionView).
    /// </summary>
    public ObservableCollection<Board> Boards { get; set; } = [];

    /// <summary>
    /// Opens a board (by numeric ID), retrieves its property and scan descriptor XML,
    /// constructs a <see cref="Board"/> instance (including parsed channels), and adds it to <see cref="Boards"/>.
    /// </summary>
    /// <param name="boardId">Zero-based board identifier as recognized by the TRION API.</param>
    /// <exception cref="TrionException">Propagated if any underlying API call fails (wrapped by <c>Utils.CheckErrorCode</c>).</exception>
    /// <remarks>
    /// Channels are parsed immediately from BoardProperties XML. If channel activation changes later,
    /// remember to call <c>Board.RefreshScanDescriptor()</c> to update layout-dependent values (e.g., ScanSizeBytes).
    /// </remarks>
    public void AddBoard(int boardId)
    {
        // Open the board
        var error = TrionApi.DeWeSetParam_i32(boardId, TrionCommand.OPEN_BOARD, 0);
        Utils.CheckErrorCode(error, "Failed to open board");

        // Fetch and parse board properties
        var boardPropertiesXml = TrionApi.DeWeGetParamStruct_String($"BoardID{boardId}", "boardproperties").value;
        var boardPropertiesModel = new BoardPropertyModel(boardPropertiesXml);

        // Create the board model (includes initial channels + raw scan descriptor XML)
        var newBoard = new Board
        {
            Id = boardId,
            Name = boardPropertiesModel.GetBoardName(),
            BoardProperties = boardPropertiesModel,
            Channels = boardPropertiesModel.GetChannels(),
            IsOpen = true,
            ScanDescriptorXml = TrionApi.DeWeGetParamStruct_String($"BoardID{boardId}", "ScanDescriptor_V3").value
        };

        Boards.Add(newBoard);
    }

    /// <summary>
    /// Initializes the enclosure by reading its name and opening a sequence of boards.
    /// </summary>
    /// <param name="numberOfBoards">Number of boards to attempt to open (typically discovered beforehand via enumeration API).</param>
    /// <remarks>
    /// Assumes board IDs are contiguous starting at 0. If hardware enumeration can produce gaps,
    /// replace the simple loop with an explicit discovery step.
    /// </remarks>
    public void Init(int numberOfBoards)
    {
        // Retrieve enclosure name (rooted at BoardID0 metadata path).
        Name = TrionApi.DeWeGetParamXML_String("BoardID0/boardproperties/SystemInfo/EnclosureInfo", "Name").value;

        for (int i = 0; i < numberOfBoards; ++i)
        {
            AddBoard(i);
        }
    }
}