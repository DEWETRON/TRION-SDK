/**
 * Short example to describe how to rad out CAN data as decoded frames
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


//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-CAN",
                                    NULL};

#define CANBUFFER   1000


int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nErrorCode = 0;
    int nBoardID = 0;
    char sChannelStr[256]={0};
    char sErrorText[256]={0};
    char sBoardID[256]={0};
    BOARD_CAN_FRAME aDecodedFrame[CANBUFFER];

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

    // Set the circular buffer size to 50 blocks. So the circular buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // configure the CAN-channel 0
    // only two properties that need to be changed for this example are
    // SyncCounter: set it to 10Mhz, so the CAN Data will have timestamps with
    // Used: enable the channel for usage
    //snprintf(sChannelStr, sizeof(sChannelStr),"%s/CAN0", sBoardID);
    snprintf(sChannelStr, sizeof(sChannelStr), "%s/CANAll", sBoardID);
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

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "ListenOnly", "False");
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring ListenOnly\nAborting.....\n");
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
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_ASYNC_POLLING_TIME, 100);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal polling time\nAborting.....\n");
    }

#if 0
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_ASYNC_FRAME_SIZE, 8);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal frame size\nAborting.....\n");
    }
#endif

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

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        // the synchronous data won't be evaluated at all
        // the samples will immediately being freed - just
        // to prevent an overrun - error (cosmetic)
        printf("\nAcquisition started. Waiting for CAN frames\n\n\n");

        while( !kbhit() )
        {
            int nAvailSamples=0;
            int nAvailCanMsgs=0;
            int i=0;
            float timestamp = 0.0f;

            // wait for 500ms
            // CAN data are typically slow anyway
            // any longer or shorter timespan is also feasible
            Sleep(100);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);

            // Free the circular buffer
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);

            // now obtain all CAN - frames that have been collected in this timespan

            do {
                nAvailCanMsgs = 0;

                // DeWeReadCAN makes a copy of CAN data and does not require a separate DeWeFree*() call
                nErrorCode = DeWeReadCAN( nBoardID, &aDecodedFrame[0], CANBUFFER, &nAvailCanMsgs);
                if ( CheckError(nErrorCode))
                {
                    printf("Error at obtaining CAN - Frames\nAborting.....\n");
                    break;
                }

                for ( i = 0; i < nAvailCanMsgs; ++i )
                {
                    timestamp = (float)((float)(aDecodedFrame[i].SyncCounter) / 10000000);  //Timestamp in 100ns re-formated to seconds
                    // note here: with a 10Mhz counter @ 32Bit width, the timestamp will wrap
                    // around after roughly 7 minutes. This Warp around has to be handled by the
                    // application on raw data
                    printf("[%012.7f] MSGID: %8.8X   Port: %d   Errorcount: %d   DataLen: %d   Data: %2.2X %2.2X %2.2X %2.2X   %2.2X %2.2X %2.2X %2.2X\n",
                                timestamp,
                                aDecodedFrame[i].MessageId,
                                aDecodedFrame[i].CanNo,
                                aDecodedFrame[i].ErrorCounter,
                                aDecodedFrame[i].DataLength,
                                aDecodedFrame[i].CanData[0], aDecodedFrame[i].CanData[1], aDecodedFrame[i].CanData[2], aDecodedFrame[i].CanData[3],
                                aDecodedFrame[i].CanData[4], aDecodedFrame[i].CanData[5], aDecodedFrame[i].CanData[6], aDecodedFrame[i].CanData[7]
                            );
                }
            } while (nAvailCanMsgs > (CANBUFFER/2));

            if ( nErrorCode > 0 )
            {
                break;
            }
        }
    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
