/**
 * Short example to describe how an analog channel is read.
 * This example should be used with a TRION board featuring analog channels
 *
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Print unscaled analog values.
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


int main(int argc, char* argv[])
{
    int nBoardId = 0;
    char sBoardId[256]   = {0};
    char sTarget[256]    = {0};
    char sErrorText[256] = {0};
    char sSetingBuff[1024] = { 0 };
    int nNoOfBoards = 0;
    int nErrorCode  = 0;
    const double fRange = 10;  //V
    char sRangeStr[48] = {0};
    int sample_offset = 8;

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

    nErrorCode = DeWeSetParamStruct_str("driver/api/TrionSystemSim", "SET_OVERFLOW_ERROR_POLICY", "OVERFLOW_ERROR_IGNORE");
    CheckError(nErrorCode);

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

    // Build BoardIDX string for _str functions
    snprintf(sBoardId, sizeof(sBoardId), "BoardID%d", nBoardId);

    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    // Set configuration to use one board in standalone operation
    snprintf(sTarget, sizeof(sTarget),"%s/%s", sBoardId, "AcqProp");
    nErrorCode = DeWeSetParamStruct_str( sTarget, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtClk", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "SampleRate", "2000");
    CheckError(nErrorCode);

    // Disable all channels
    snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "ChannelAll");
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Used", "False");

    // Enable one analog channel will be enabled (AI)
    snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "AI0");
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Used", "True");
    if (nErrorCode)
    {
        snprintf(sErrorText, sizeof(sErrorText), "Could not enable AI channel on board %d: %s\n", nBoardId, DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

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
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        sint64 nBufStartPos = 0;
        sint64 nBufEndPos = 0;         // Last position in the circular buffer
        int nBufSize = 0;              // Total buffer size

        printf("\nMeasurement Started on Brd: %s/AI0:..\n\n\n",sBoardId);
        // Get detailed information about the circular buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_START_POINTER, &nBufStartPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
            int nAvailSamples=0;
            int i=0;
            sint32 nRawData=0;

            //Sleep(100);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);
            if (ERR_BUFFER_OVERWRITE == nErrorCode)
            {
                printf("Measurement Buffer Overflow happened - stopping measurement\n");
                break;
            }

            // skip if number of samples is smaller than the current ADC delay
            if (nAvailSamples <= 0)
            {
                continue;
            }

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
            CheckError(nErrorCode);

            // Read the current samples from the circular buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Handle the circular buffer wrap around
                if (nReadPos >= nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }

                // Get the sample value at the read pointer of the circular buffer
                // The sample value is 24Bit (little endian, encoded in 32bit).
                nRawData = *(sint32*)nReadPos;

                {
                    printf("\nRaw: %8.8x", nRawData);
                    fflush(stdout);
                }

                // Increment the read pointer
                nReadPos += sizeof(uint32);
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
