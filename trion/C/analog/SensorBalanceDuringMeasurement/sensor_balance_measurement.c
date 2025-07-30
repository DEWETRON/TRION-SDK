/**
 * Short example to describe how to trigger an sensor balance
 *
 * for analogue channels of a TRION-dSTG or TRION-MULTI board
 *
 *
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-dSTG",
                                    "TRION-2402-MULTI",
                                    NULL};

#define START_SENSORBALANCE_ON_ITERATION    50


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nBoardID = 0;
    int nSizeScan=0;
    char sBoardID[256]={0};
    char sChannelStr[256]={0};
    char sErrorText[256] = {0};
    char sSettingStr[32*1024]={0};  //The result-set will be an XML-Document, that may be rather large
    int loopcounter = 0;

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

    // Enable Channels 0,1,2
    // leave all other AI channels (3,4,5 or 3,4,5,6,7 depending on connector-panel)
    // disabled
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AI0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Used", "True");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Mode", "Bridge");
    CheckError(nErrorCode);
    // switch to one of the lower Ranges, to make effect more obvious
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Range", "10 mV/V");
    CheckError(nErrorCode);

    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AI1", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Used", "True");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Mode", "Bridge");
    CheckError(nErrorCode);
    // switch to one of the lower Ranges, to make effect more obvious
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Range", "10 mV/V");
    CheckError(nErrorCode);

    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AI2", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Used", "True");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Mode", "Bridge");
    CheckError(nErrorCode);
    // switch to one of the lower Ranges, to make effect more obvious
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Range", "10 mV/V");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_CHN_ALL, 0);
    CheckError(nErrorCode);

    //prepare the acquisition parameters
    // Set configuration to use one board in standalone operation
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "OperationMode", "Master");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtClk", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "Samplerate", "2000");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the samplerate 200000 samples per second, 20000 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the circular buffer size to 50 blocks. So the circular buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Determine the size of a sample scan
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan);
    CheckError(nErrorCode);

    // Start the Measurement
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        // not strictly necessary - just to illustrate, that the SensorOffset command can
        // be used during any time after Acquisition-Start
        Sleep(100);

        do
        {
            int nAvailSamples;
            ++loopcounter;

            Sleep(100);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);

            if ( loopcounter == START_SENSORBALANCE_ON_ITERATION ) {
                printf ("starting SensorOffset on Board %d\n", nBoardID);
                snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AIAll", nBoardID);
                nErrorCode = DeWeSetParamStruct_str( sChannelStr, "SensorOffset", "100msec");
                CheckError(nErrorCode);
                // please note, that though the command is issued to AIAll
                // only the channels 0,1,2 will actually be affected by this command now
                // reason: as the application currently holds the control over a running
                // acquisition, the API is not able to change the active/inactive state
                // of the various channels, and can only perform the operation on the
                // channels that have been activated by the application itself

                // the actual measured data won't be processed in this example anyway
                // therefore all the circular-buffer handling is skipped. please refer to
                // one_analogue_channel how to actually process data
            } else {
                printf ("doing some arbitrary processing of %d samples on iteration %d\n", nAvailSamples, loopcounter);
            }

            // normally here some form of data-processing would take place

            // Free the circular buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);

            if ( loopcounter < START_SENSORBALANCE_ON_ITERATION ){
                continue;
            }

            // in parallel the application can poll about the state of the balancing process
            nErrorCode = DeWeGetParamStruct_str( sChannelStr, "SensorOffset", sSettingStr, sizeof(sSettingStr));
            CheckError(nErrorCode);
            if ( nErrorCode > 0 ) {
                //Error - Trap
                printf("Error during Balancing: %s\nAborting.....\n", DeWeErrorConstantToString(nErrorCode));
                break;
            }
            // Test, if the result contains the substring "EstDuration"
            // this will also hold the estimated runtime for the whole test in ms
            // please not, that this estimation depends on several details
            // how the application handles acquisition - so it should
            // only be used as a rough estimation - or as used here in this
            // example as a stop-condition
            if ( NULL != strstr(sSettingStr, "EstDuration") )
            {
                printf("%s\n", sSettingStr);
            }
            else
            {
#if 0
                // sOffsetValue has to be extracted from XML (not shown in this example)
                snprintf(sOffsetValue, sizeof(sOffsetValue), "%f", 1.2345);

                snprintf(sChannelStr, sizeof(sChannelStr),"%s/AI0", sBoardID);
                nErrorCode = DeWeSetParamStruct_str( sChannelStr, "InputOffset", sOffsetValue);
                checkError(nErrorCode);
                nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_AI, 0);
                checkError(nErrorCode);
# else
                DumpXmlTree(sSettingStr);
#endif
                printf("\n\nAmplifier Balancing finished for Board %d\n", nBoardID);
                break;
            }

        } while (1);

        //stop the Acquisition again
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_STOP_ACQUISITION, 0);
        CheckError(nErrorCode);
    }

    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}

