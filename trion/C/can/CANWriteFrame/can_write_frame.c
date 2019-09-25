/**
 * Short example to describe how to rad out CAN data as raw frames
 *
 * This example should be used with a TRION-CAN board installed
 * or configured in the simulated system
 * 
 * Describes following:
 *  - Setup of 1 CAN channel
 *  - Print raw CAN frames + Timestamp
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "trion_sdk_util.h"


#define VERBOSE
#undef VERBOSE

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-CAN",    
                                    NULL};


int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nErrorCode = 0;
    int nBoardID = 0;
    char sChannelStr[256]={0};
    char sErrorText[256]={0};
    char sBoardID[256]={0};

    // Load pxi_api.dll
    if ( 0 != LoadTrionApi() )
    {
        return 1;
    }

    // Initialize driver and retrieve the number of TRION boards
    // nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode = DeWeDriverInit(&nNoOfBoards);
    CheckError(nErrorCode);
    nNoOfBoards=abs(nNoOfBoards);

    // Check if TRION cards are in the system
    if (nNoOfBoards == 0)
    {
        return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using the DEWE2 Explorer.\n");
    }

    // Build BoardId -> Either comming from command line (arg 1) or default "0"
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardID) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardID, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardID, sizeof(sBoardID),"BoardID%d", nBoardID);

    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardID, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    // Set configuration to use one board in standalone operation
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // configure the BoardCounter-channel
    // for HW - timestamping to work it is necessary to have
    // at least one synchronous channel active. All TRION
    // boardtypes support a channel called Board-Counter (BoardCNT)
    // this is a basic counter channel, that usually has no
    // possibility to feed an external signal, and is usually
    // used to route internal signals to its input
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/BoardCNT0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Used", "True");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default sample-rate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // configure the CAN-channel 0
    // only two properties that need to be changed for this example are
    // SyncCounter: set it to 10Mhz, so the CAN Data will have timestamps with
    // Used: enable the channel for usage
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/CAN0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "SyncCounter", "10 MHzCount");
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring Synccounter\nAborting.....\n");
    }

    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Termination", "True");
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring Termination\nAborting.....\n");
    }

    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "BaudRate", "500000");
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring BaudRate\nAborting.....\n");
    }

    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Used", "True");
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring Used-flag\nAborting.....\n");
    }

    // Open the CAN - Interface to this Board
    nErrorCode = DeWeOpenCAN(nBoardID);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at opening CAN-Interface\nAborting.....\n");
    }

    // Configure the ASYNC-Polling Time to 100ms
    // Configure the Frame-Size (CAN == 8)
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_ASYNC_POLLING_TIME, 33);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal polling time\nAborting.....\n");
    }
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_ASYNC_FRAME_SIZE, 8);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal frame size\nAborting.....\n");
    }

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal frame size\nAborting.....\n");
    }

    // Start CAN capture, before start sync-acquisition
    // the sync - acquisition will synchronize the async data
    nErrorCode = DeWeStartCAN(nBoardID, -1 );
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at starting CAN\nAborting.....\n");
    }

    BOARD_CAN_FRAME frame;
    frame.CanNo = 0;
    frame.MessageId = 42;
    frame.DataLength = 7;
    memcpy(frame.CanData, "1234567", 7);
    frame.StandardExtended = 0;
    frame.FrameType = 0;
    frame.SyncCounter = 0;
    frame.ErrorCounter = 0;
    frame.SyncCounterEx = 0;

    int n_written = 666;
    nErrorCode = DeWeWriteCAN(nBoardID, &frame, 1, &n_written);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error writing CAN frame\nAborting.....\n");
    }
    if (n_written != 1)
    {
        return UnloadTrionApi("Huh? Didn't write *one* frame?\n");
    }

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
