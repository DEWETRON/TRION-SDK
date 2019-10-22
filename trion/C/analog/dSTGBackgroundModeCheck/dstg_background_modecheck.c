/**
 * Short example to showcase the dSTG mode check functionality.
 *
 * This example should be used with a TRION-2402-dSTG-6.LE as board 0
 *
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Setup the channel properties for bridge measurement
 *  - Start data acquisition on board
 *  - Peform background mode check
 *  - Print of mode check report
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24

//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG",
                                    NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nBoardID = 0;
    int nErrorCode = 0;
    int nADCDelay = 0;
    uint32 nLen = 0;
    char sBoardID[256] = {0};
    char sErrorText[256] = {0};
    char sChannelStr[256] = {0};
    char* pcBuff = NULL;

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

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI0)
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AI0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Used", "True");
    CheckError(nErrorCode);

    // Also set up the channel properties
    // First the mode is selected. In this case it will be Bridge measurement mode
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Mode", "Bridge");
    CheckError(nErrorCode);
    // The bridge type is Quarter bridge with 3 wires
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "InputType", "BRQUARTER3W");
    CheckError(nErrorCode);
    // The bridge resistanmce is 350 Ohm
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "BridgeRes", "350");
    CheckError(nErrorCode);
    // 5V of excitation will be applied to the bridge
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Excitation", "5V");
    CheckError(nErrorCode);
    // The measurement range will be 100 mV/V
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Range", "100 mV/V");
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

    printf("\nStarting acquisition.\n");
    fflush(stdout);

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;         // Last position in the ring buffer
        int nBufSize=0;           // Total buffer size
        int nAcqCycle=0;  // Acquisition cycle counter
        BOOLEAN bCheckInProgress = FALSE; // Reminder that a mode check has been started
        BOOLEAN bModeCheckFinished = FALSE; // The mode check has finished
        BOOLEAN bAlreadyStarted = FALSE; // A mode check has already been performed. No need to start another one.

        // Get detailed information about the ring buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        while (!kbhit())
        {
            sint64 nReadPos = 0;       // Pointer to the ring buffer read pointer
            int nAvailSamples = 0;
            sint32 nRawData = 0;

            Sleep(100);

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32(nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples);
            if (nErrorCode != ERR_NONE)
                CheckError(nErrorCode);

            // Available samples has to be recalculated according to the ADC delay
            nAvailSamples = nAvailSamples - nADCDelay;

            // skip if number of samples is smaller than the current ADC delay
            if (nAvailSamples <= 0)
            {
                continue;
            }

            // Print the sample value. Only print it if no mode check is going on.
            // In case a mode check is active this data is meaningless anyway
            if (FALSE == bCheckInProgress) {
                // Get the current read pointer
                nErrorCode = DeWeGetParam_i64(nBoardID, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos);
                CheckError(nErrorCode);

                // recalculate nReadPos to handle ADC delay
                nReadPos = nReadPos + nADCDelay * sizeof(uint32);

                // Print just the first sample frmo the ring buffer
                // Get the sample value at the read pointer of the ring buffer
                // The sample value is 24Bit (little endian, encoded in 32bit).
                nRawData = formatRawData(*(sint32*)nReadPos, (int)DATAWIDTH, 8 );
                printf("Raw Data: %d\n", nRawData);
                fflush(stdout);
            }
            // Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples);
            if (nErrorCode != ERR_NONE)
                CheckError(nErrorCode);

            // Another acquisition cycle has ended
            nAcqCycle++;

            // After enough cycles trigger the mode check
            if ((10 < nAcqCycle) && (FALSE == bAlreadyStarted))
            {
                // Start the actual mode check.
                nErrorCode = DeWeSetParamStruct_str(sChannelStr, "ModeCheck", "");
                //Remember that a mode check is going on
                if (nErrorCode <= 0)
                {
                    bCheckInProgress = TRUE;
                    bModeCheckFinished = FALSE;
                    bAlreadyStarted = TRUE;
                    printf("\nMode check started!\n");
                    fflush(stdout);
                }
            }

            if (FALSE != bCheckInProgress)
            {
                // Get the mode check report. As long as the mode checks are still running the report will
                // contain the number of still pending tests. This can be used to determine the mode check is completed.
                // First get the length of the check report
                DeWeGetParamStruct_strLEN( sChannelStr , "ModeCheck", &nLen );

                // Allocate a buffer big enough
                pcBuff = (char*)malloc(nLen + 1);

                // Get the actual report
                nErrorCode = DeWeGetParamStruct_str( sChannelStr, "ModeCheck", pcBuff, nLen + 1 );
                CheckError(nErrorCode);

                // Print the results
                printf("\n%s\n", pcBuff);
                fflush(stdout);

                // Check whether the results are for still running test
                // Such a result will have a node named "EstDuration" that will hold the estimated
                // remaining time of the mode check
                if (NULL == strstr(pcBuff, "EstDuration"))
                {
                    // No such node. Test completed.
                    bModeCheckFinished = TRUE;
                }

                // Mode check completed. Reset everything and print a message for the user.
                if (FALSE != bModeCheckFinished)
                {
                    bCheckInProgress = FALSE;
                    bModeCheckFinished = FALSE;
                    fflush(stdout);
                    free(pcBuff);
                    printf("\nMode check completed!\n");
                }
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
