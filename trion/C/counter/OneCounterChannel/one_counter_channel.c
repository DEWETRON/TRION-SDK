/**
 * Short example to describe how a digital counter channel is read.
 *
 * This example should be used with a TRION-CNT.xx or TRION-2402-dACC.6.BN
 *
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-CNT",
                                    "TRION-2402-dACC",
                                    "TRION-1802-dLV",
                                    "TRION-1600-dLV",
                                    NULL};


int main(int argc, char* argv[])
{
    int nBoardId = 0;
    char sBoardId[256]    = {0};
    char sTarget[256]     = {0};
    char sErrorText[256]  = {0};
    int nNoOfBoards = 0;
    int nErrorCode = 0;

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
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardId) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build ID String
    snprintf(sBoardId, sizeof(sBoardId), "BoardID%d", nBoardId);

    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardId, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    // After reset all channels are disabled.
    // So here 1 counter channel will be enabled (CNT)
    // and set its input to ACQ-Clock
    snprintf(sTarget, sizeof(sTarget),"%s/%s", sBoardId, "CNT0");
    nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
    CheckError(nErrorCode);
    if (nErrorCode)
    {
        snprintf(sErrorText,sizeof(sErrorText), "Could not enable CNT channel on board %d: %s\n",nBoardId,DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

    nErrorCode = DeWeSetParamStruct_str( sTarget, "Source_A", "Acq_Clk");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "Reset", "OnReStart");
    CheckError(nErrorCode);

    snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "CNT0");
    nErrorCode = DeWeSetParamStruct_str(sTarget, "UsedSub", "True");
    CheckError(nErrorCode);
    if (nErrorCode)
    {
        snprintf(sErrorText, sizeof(sErrorText), "Could not enable CNT channel on board %d: %s\n", nBoardId, DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

    // Set configuration to use one board in standalone operation
    snprintf(sTarget, sizeof(sTarget),"%s/%s", sBoardId, "AcqProp");
    nErrorCode = DeWeSetParamStruct_str( sTarget, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtClk", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "SampleRate", "1000");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default samplerate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the circular buffer size to 50 blocks. So the circular buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_START_ACQUISITION, 0);
    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;         // Last position in the circular buffer
        int nBufSize=0;              // Total buffer size

        // Get detailed information about the circular buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        printf("\nAcquisition started. Waiting for CNTer Samples\n\n\n");

        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
            int nAvailSamples=0;
            int i=0;
            uint32 nRawData=0;

            Sleep(100);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);
            if (ERR_BUFFER_OVERWRITE == nErrorCode)
            {
                printf("Measurement Buffer Overflow happened - stopping measurement\n");
                break;
            }

            printf("CMD_BUFFER_AVAIL_NO_SAMPLE: %d  (%x)\n", nAvailSamples, nErrorCode);
            fflush(stdout);

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
            CheckError(nErrorCode);

            // Read the current samples from the circular buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Get the sample value at the read pointer of the circular buffer
                nRawData = *(uint32*)nReadPos;

                // Print the sample value
                printf("CNT=%8.8x   SUB=%8.8x\n", nRawData, *(uint32*)(nReadPos+4));
                fflush(stdout);

                // Increment the read pointer
                nReadPos += 2 * sizeof(uint32);

                // Handle the circular buffer wrap around
                if (nReadPos >= nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }
            }

            // Free the circular buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);
        }
    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
