/**
 * Short example to describe how to work with multiple TRION boards
 *
 * This example should be used with a TRION-2402-dACC.6.BN as board 0 and 1
 *
 *
 * Describes following:
 *  - Setup of 3 AI channel board x(0)
 *  - Set board 0 as master
 *  - Setup of 3 AI channel board y(1)
 *  - Set board 1 as slave
 *  - Set sample rate to 50kSamples/second
 *  - Print of analog values
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

#define NUM_OF_BOARDS         2
#define CHANNEL_SIZE          1024*32
#define NUM_OF_CHANNELS       6
#define BUFFER_OFFSET_BOARD_1 CHANNEL_SIZE * 3

//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24

//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dACC",
                                    "TRION-2402-dSTG",
                                    "TRION-2402-LV",
                                    "TRION-2402-MULTI",
                                    NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards= 0;
    int nErrorCode = 0;
    int nADCDelay[NUM_OF_BOARDS] = {0};
    int nSizeScan[NUM_OF_BOARDS] = {0};
    int nBoardID1 = 0;
    int nBoardID2 = 1;
    char sChannelStr[256]= {0};
    char sPropStr[256] ={0};
    char sBoardName[256] = {0};
    char sErrorText[256] = {0};
    char sBoardID1[256] = {0};
    char sBoardID2[256] = {0};
    int i=0,j=0,nbrd=0;

    // Channel buffer for 6 channels with 1024 * 32 samples per channel
    sint32* nChannelBuffer=0;
    size_t memsize = CHANNEL_SIZE * NUM_OF_CHANNELS * sizeof(sint32);
    nChannelBuffer = (sint32*) malloc(memsize);
    memset(nChannelBuffer, 0, memsize);

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
        return UnloadTrionApi("No Trion cards found. Aborting...\n");
    }

    // Build BoardIds -> Either comming from command line (arg 1 and arg 2) or default "0" and "1"
    if( TRUE != ARG_GetBoardIdEX(argc, argv, nNoOfBoards, &nBoardID1, &nBoardID2) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardIds: %d  %d\nNumber of found boards: %d", nBoardID1, nBoardID2, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build strings in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardID1, sizeof(sBoardID1),"BoardID%d", nBoardID1);
    snprintf(sBoardID2, sizeof(sBoardID2),"BoardID%d", nBoardID2);

    // Open & Reset First Board
    nErrorCode = DeWeSetParam_i32( nBoardID1, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID1, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardID1, sBoardNameNeeded))
    {    
        return UnloadTrionApi(NULL);
    }

    // Open & Reset Second Board 
    nErrorCode = DeWeSetParam_i32( nBoardID2, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID2, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardID2, sBoardNameNeeded))
    {    
        return UnloadTrionApi(NULL);
    }

    ////////////////////////////////////////////////////////
    // So here 3 analog channels will be enabled on First Board
    for (i=0; i<3; ++i)
    {
        snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AI%d", nBoardID1, i);
        nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Used", "True");
        CheckError(nErrorCode);
    }

    // Set First Board as master
    snprintf(sPropStr, sizeof(sPropStr), "BoardID%d/AcqProp", nBoardID1);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "OperationMode", "Master");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "ExtClk", "False");
    CheckError(nErrorCode);
    // Set Sample Rate to 50kSamples/seconds
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "SampleRate", "50000");
    CheckError(nErrorCode);

    ////////////////////////////////////////////////////////
    // So here 3 analog channels will be enabled on Second Board
    for (i=0; i<3; ++i)
    {
        snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AI%d", nBoardID2, i);
        nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Used", "True");
        CheckError(nErrorCode);
    }

    // Set Second Board as slave
    snprintf(sPropStr, sizeof(sPropStr), "BoardID%d/AcqProp", nBoardID2);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "ExtTrigger", "PosEdge");  // or "NegEdge"
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "ExtClk", "False");
    CheckError(nErrorCode);
    // Set Sample Rate to 50kSamples/seconds
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "SampleRate", "50000");
    CheckError(nErrorCode);


    // Setup the acquisition buffers for board 0 and 1
    //for (nBoardID=0; nBoardID<2; ++nBoardID)
    //{
    for (i=0; i<NUM_OF_BOARDS; ++i)
    {
        int tmp_id=0;
        switch(i){
        case 0: tmp_id = nBoardID1;
            break;
        case 1: tmp_id = nBoardID2;
            break;
        }

        // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
        // For the default samplerate 2000 samples per second, 200 is a buffer for
        // 0.1 seconds
        nErrorCode = DeWeSetParam_i32( tmp_id, CMD_BUFFER_BLOCK_SIZE, 200);
        CheckError(nErrorCode);

        // Set the ring buffer size to 50 blocks. So ring buffer can store samples
        // for 5 seconds
        nErrorCode = DeWeSetParam_i32( tmp_id, CMD_BUFFER_BLOCK_COUNT, 2500);
        CheckError(nErrorCode);

        // Update the hardware with settings
        nErrorCode = DeWeSetParam_i32( tmp_id, CMD_UPDATE_PARAM_ALL, 0);
        CheckError(nErrorCode);

        // Get the ADC delay. The typical conversion time of the ADC.
        // The ADCDelay is the offset of analog samples to digital or counter samples.
        // It is measured in number of samples,
        nErrorCode = DeWeGetParam_i32( tmp_id, CMD_BOARD_ADC_DELAY, &nADCDelay[i]);
        CheckError(nErrorCode);

        // Determine the size of a sample scan
        nErrorCode = DeWeGetParam_i32( tmp_id, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan[i]);
        CheckError(nErrorCode);

    }

    // Data Acquisition - stopped with any key

    // Start slave first!!!
    nErrorCode = DeWeSetParam_i32( nBoardID2, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);

    // Start Master
    nErrorCode = DeWeSetParam_i32( nBoardID1, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        int nAvailSamples;
        while( !kbhit() )
        {

            Sleep(10);

            for (nbrd=0; nbrd<NUM_OF_BOARDS; ++nbrd)
            {
                sint64 nBufEndPos=0;         // Last position in the ring buffer
                int nBufSize=0;           // Total buffer size
                sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
                sint64 nReadPosAI=0;     // Pointer to the ring buffer read pointer for AI samples suffering sample delay
                sint32 nRawData[3]={0};
                int tmp_id=0;

                switch(nbrd){
                case 0: tmp_id = nBoardID1;
                    break;
                case 1: tmp_id = nBoardID2;
                    break;
                }

                // Get detailed information about the ring buffer
                // to be able to handle the wrap around
                nErrorCode = DeWeGetParam_i64( tmp_id, CMD_BUFFER_END_POINTER, &nBufEndPos);
                CheckError(nErrorCode);
                nErrorCode = DeWeGetParam_i32( tmp_id, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
                CheckError(nErrorCode);

                // Get the number of samples already stored in the ring buffer
                nErrorCode = DeWeGetParam_i32( tmp_id, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
                CheckError(nErrorCode);
                if (nErrorCode > 0)
                {
                    return UnloadTrionApi("Buffer Overflow happened\n");
                }

                // Available samples has to be recalculated according to the ADC delay
                nAvailSamples = nAvailSamples - nADCDelay[nbrd];

                // skip if number of samples is smaller than the current ADC delay
                if (nAvailSamples <= 0)
                {
                    continue;
                }

                // Get the current read pointer
                nErrorCode = DeWeGetParam_i64( tmp_id, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
                CheckError(nErrorCode);

                // recalculate nReadPos to handle ADC delay
                nReadPosAI = nReadPos + nADCDelay[nbrd] * nSizeScan[nbrd];

                // Read the current AI samples from the ring buffer
                for (i = 0; i < nAvailSamples; ++i)
                {
                    // Channel buffer overflow check
                    if (i > CHANNEL_SIZE)
                    {
                        return UnloadTrionApi("Channel Overflow happened\n");
                    }

                    // Get the sample value at the read pointer of the ring buffer
                    // memcopy 3 samples to nRawData buffer
                    memcpy(nRawData, (uint32*)nReadPosAI, nSizeScan[nbrd]);
                    // Separate and copy AI samples to AI channel buffer
                    nChannelBuffer[i + BUFFER_OFFSET_BOARD_1 * nbrd + CHANNEL_SIZE * 0] = nRawData[0];
                    nChannelBuffer[i + BUFFER_OFFSET_BOARD_1 * nbrd + CHANNEL_SIZE * 1] = nRawData[1];
                    nChannelBuffer[i + BUFFER_OFFSET_BOARD_1 * nbrd + CHANNEL_SIZE * 2] = nRawData[2];

                    //printf("BoardID: %d  AII: %12d\n", nBoardID, nRawData0);
                    // Increment the read pointer
                    nReadPosAI += nSizeScan[nbrd];

                    // Handle the ring buffer wrap around
                    if (nReadPosAI >= nBufEndPos)
                    {
                        nReadPosAI -= nBufSize;
                    }
                }

                // Free the ring buffer after read of all values
                nErrorCode = DeWeSetParam_i32( tmp_id, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
                CheckError(nErrorCode);
            }

            // print every 1/100 samples
            for (i = 0; i < nAvailSamples; i=i+500)
            {
                printf("B0_AI1: %12d   B0_AI2: %12d   B0_AI3: %12d   B1_AI1: %12d   B1_AI2: %12d   B1_AI3: %12d\n",
                       nChannelBuffer[i], nChannelBuffer[CHANNEL_SIZE+i], nChannelBuffer[CHANNEL_SIZE*2+i],
                       nChannelBuffer[CHANNEL_SIZE*3+i], nChannelBuffer[CHANNEL_SIZE*4+i], nChannelBuffer[CHANNEL_SIZE*5+i]);
                fflush(stdout);
            }
        }

    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoardID2, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID1, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardID1, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID2, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);
    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    free(nChannelBuffer);
    return nErrorCode;
}
