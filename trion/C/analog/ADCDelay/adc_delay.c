/**
 * Short example to describe how to correctly handle the ADC Delay
 *
 * This example should be used with a TRION-board featured with analoge channels
 *
 * ADC Delay:
 * As ADCs suffer from a conversion time (=ADC Delay). So analog samples are
 * added to the ring buffer with a constant offset.
 *
 * Example Ringbuffer for AI1, CNT0, (ADC Delay == 3):
 *
 * Sample No  |   AI1   |   CNT0
 * --------------------------------
 *    0       |   null  |    1
 *    1       |   null  |    2
 *    2       |   null  |    3
 *    3       |    1    |    4
 *    4       |    2    |    5
 *    5       |    3    |    6
 *    6       |    4    |    7
 *
 * The first three entries of AI1 are invalid (null).
 * How to handle the ADC Delay:
 *   - Recalculate the read pointer for analog channels
 *
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Setup of 1 CNT channel
 *  - Query for the ADC delay
 *  - Print of analog and CNT value.
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH        24

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-dACC", 
                                    "TRION-2402-dSTG", 
                                    "TRION-1620-LV", 
                                    "TRION-1620-ACC",
                                    "TRION-1603-LV", 
                                    NULL };

int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nErrorCode = 0;
    int nADCDelay=0;
    int nBoardID = 0;
    int nSizeScan=0;
    char sBoardID[256]={0};
    char sChannelStr[256]={0};
    char sSettingStr[256]={0};
    char sErrorText[256]={0};

    // create extra channels buffers
    sint32 ai_channel[1024];
    uint32 cnt_channel[1024];

    memset(ai_channel,  0, sizeof(ai_channel));
    memset(cnt_channel, 0, sizeof(cnt_channel));

    // Load pxi_api.dll
    if ( 0 != LoadTrionApi() )
    {
        return 1;
    }

    // Initialize driver and retrieve the number of TRION boards
    // nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode = DeWeDriverInit(&nNoOfBoards);
    CheckError(nErrorCode);
    nNoOfBoards = abs(nNoOfBoards);

    // Check if TRION cards are in the system
    if (nNoOfBoards == 0)
    {
        return UnloadTrionApi("No TRION board found. Aborting...\nPlease configure a system using the DEWE2 Explorer.\n");
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

    if ( FALSE == TestBoardType(nBoardID, sBoardNameNeeded) )
    {
        return UnloadTrionApi(NULL);
    }

    // After reset all channels are disabled.
    // So here 1 analog channels will be enabled (AI1)
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AI1", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Used", "True");

    // Additionally add a counter: CNT0
    // and set its input to ACQ-Clock
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/CNT0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Used", "True");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Source_A", "Acq_Clk");
    CheckError(nErrorCode);

    // Set configuration to use one board in standalone operation
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "OperationMode", "Master");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default samplerate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);

    // Determine the size of a sample scan
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan);
    CheckError(nErrorCode);

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;      // Last position in the ring buffer
        int nBufSize=0;           // Total buffer size

        // Get detailed information about the ring buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        printf("\nAcquisition started ..\n\n");
        while( !kbhit() )
        {
            sint64 nReadPosAI=0;     // Pointer to the ring buffer read pointer for AI samples suffering sample delay
            sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
            int nAvailSamples=0;
            int i=0;
            sint32 nRawData0=0;

            Sleep(100);

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);

            // Available samples has to be recalculated according to the ADC delay
            nAvailSamples = nAvailSamples - nADCDelay;

            // skip if number of samples is smaller than the current ADC delay
            if (nAvailSamples <= 0)
            {
                continue;
            }

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
            CheckError(nErrorCode);

            // recalculate nReadPos to handle ADC delay
            nReadPosAI = nReadPos + nADCDelay * nSizeScan;

            // Read the current AI samples from the ring buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // handle possible channel overflow
                if (i > sizeof(ai_channel)/sizeof(uint32))
                {
                    nAvailSamples = sizeof(ai_channel)/sizeof(uint32);
                }

                // Get the sample value at the read pointer of the ring buffer
                // Here the complete scan is 2 * 32 bit : 1 analog values and 1 CNT Value
                nRawData0 = formatRawData(*(uint32*)nReadPosAI, (int)DATAWIDTH, 8);

                // Copy AI samples to AI channel buffer
                ai_channel[i] = nRawData0;

                // Increment the read pointer
                nReadPosAI += nSizeScan;

                // Handle the ring buffer wrap around
                if (nReadPosAI >= nBufEndPos)
                {
                    nReadPosAI -= nBufSize;
                }
            }

            // Read the current CNT samples from the ring buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Get the sample value at the read pointer of the ring buffer
                // Here the complete scan is 2 * 32 bit : 1 analog values and 1 CNT Value
                nRawData0 = *(uint32*)(nReadPos + sizeof(uint32));

                // Copy CNT samples to CNT channel buffer
                cnt_channel[i] = nRawData0;

                // Increment the read pointer
                nReadPos += nSizeScan;

                // Handle the ring buffer wrap around
                if (nReadPos >= nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }
            }

            // print the unsclaed AI samples and counter samples
            for (i = 0; i < nAvailSamples; ++i)
            {
                printf("\rAI1: %12d   CNT:%012d", ai_channel[i], cnt_channel[i]);
                fflush(stdout);
            }

            // Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);
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

