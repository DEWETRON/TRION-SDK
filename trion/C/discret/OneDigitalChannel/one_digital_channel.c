/**
 * Short example to describe how a digital channel is read.
 *
 * This example should be used with a TRION-BASE board 0
 *
 * Describes:
 *  - How to find the board id of a dedicated TRION board (TRION-BASE)
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-BASE",
                                    "TRION-TIMING",
                                    "TRION-DI",
                                    "TRION-1802",
                                    "TRION-1600",
                                    NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards;
    int nErrorCode = 0;
    int nBoardID = -1;
    char sBoardID[256] = {0};
    char sChannelStr[256] = {0};
    char sSettingStr[256] = {0};
    char sErrorText[256]  = {0};
    BOOLEAN bBoardFound = FALSE;

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

    // Build BoardId -> Either coming from command line (arg 1) or default "0"
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

    // After reset all channels are disabled.
    // So here 1 digital channel will be enabled (Discret0)

    snprintf(sChannelStr, sizeof(sChannelStr), "%s/Discret0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Mode", "DIO");
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Used", "True");

    // Set configuration to use one board in standalone operation
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtClk", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "SampleRate", "1000");
    CheckError(nErrorCode);

    nErrorCode = DeWeSetParamStruct_str(sSettingStr, "ResolutionAI", "24");

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default samplerate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_0_BLOCK_SIZE, 1);
    CheckError(nErrorCode);

    // Set the circular buffer size to 50 blocks. So the circular buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_0_BLOCK_COUNT, 200);
    CheckError(nErrorCode);

    //char BUFFER[50000] = { 0 };
    //DeWeGetParamStruct_str("BoardID0", "config", BUFFER, 50000);


    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    //memset(BUFFER, 0, 50000);
    //DeWeGetParamStruct_str("BoardID0", "config", BUFFER, 50000);

    char sScanDescriptor[5000] = { 0 };

    nErrorCode = DeWeGetParamStruct_str(sBoardID, "ScanDescriptor_V3", sScanDescriptor, sizeof(sScanDescriptor));
    CheckError(nErrorCode);

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;       // Last position in the circular buffer
        int nBufSize=0;            // Total buffer size

        // Get detailed information about the circular buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_0_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_0_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        printf("\nAcquisition started. Waiting for CNTer Samples\n\n\n");
        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
            int nAvailSamples=0;
            int i=0;
            uint32 nRawData=0;
            uint32 nBit=0;

           Sleep(50);

            // Get the number of samples already stored in the circular buffer
            // using CMD_BUFFER_WAIT_AVAIL_NO_SAMPLE no sleep is necessary
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLE, &nAvailSamples );
            if (CheckError(nErrorCode))
            {
                break;
            }

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_0_ACT_SAMPLE_POS, &nReadPos );
            if (CheckError(nErrorCode))
            {
                break;
            }

            // toggle do
            //DeWeSetParam_i32(nBoardID, CMD_DISCRET_STATE_CLEAR, 8);
            DeWeSetParam_i32(nBoardID, CMD_DISCRET_STATE_SET, 8);

            // Read the current samples from the circular buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                uint64* data = (uint64*)nReadPos;

                // Get the sample value at the read pointer of the circular buffer
                nRawData = *(uint32*)nReadPos;

                // mask the bit for Discret0
                nBit = (nRawData & 0x1);

                // Print the sample value
                if (0 == i % 100)
                {
                    printf("\rReceived Data: 0x%2.2X", nBit);
                    fflush(stdout);
                }

                // Increment the read pointer
                nReadPos += sizeof(uint32);

                // Handle the circular buffer wrap around
                if (nReadPos >= nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }
            }

            // Free the circular buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_0_FREE_NO_SAMPLE, nAvailSamples );
            if (CheckError(nErrorCode))
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
