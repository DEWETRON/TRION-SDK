/**
 * Short example to describe how to trigger an sensor balance 
 *
 * for analogue channels of a specific board
 *
 *
 * In this example no application-owned acquisition is running
 * so the balancing-operation will execute blocking, and will not
 * return until finished
 *
 * After execution the offset-values can be obtained as XML-Document
 * These values have to be applied manually by setting the property
 * "InputOffset"
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


int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nErrorCode = 0;
    int nBoardID = 0;
    char sBoardID[256]={0};
    char sChannelStr[256]={0};
    char sErrorText[256]={0};
    char sSettingStr[32*1024]={0};  //The result-set will be an XML-Document, that may be rather large
#if 0
    char sOffsetValue[256]={0};
#endif

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

    // Switch all Channels to BridgeMode
    // Reason: Not all modes support Sensor-Unbalance
    // See ChannelProperties/AIx/Mode/ChannelFeatures in Board-Properties.XML for the given Board
    // if it includes "SensorUnbalance", this functionality is supported in the given mode
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AIAll", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Mode", "Bridge");
    CheckError(nErrorCode);
    // switch to one of the lower Ranges, to make effect more obvious
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Range", "-10..10 mV/V");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Excitation", "5 V");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "InputType", "BRQUARTER3W");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "LPFilter_Val", "Auto");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "BridgeRes", "350");
    CheckError(nErrorCode);

    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Do not start an Acquisition
    // Just set the SensorOffset command to the specific Board for all Channels
    // The Balancing duration shall be 100msec
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "SensorOffset", "100msec");
    CheckError(nErrorCode);

    // In this case (no own acquisition running) this operation is non-blocking
    // so the final result is available with the next line of code
    nErrorCode = DeWeGetParamStruct_str( sChannelStr, "SensorOffset", sSettingStr, sizeof(sSettingStr));
    CheckError(nErrorCode);
    if ( nErrorCode > 0 ) {
        //Error - Trap
        printf("Error during Balancing: %s\nAborting.....\n", DeWeErrorConstantToString(nErrorCode));

    } else {
        // here the Balancing operation is finished
        // In a production code the element <Offset></Offset> now would need
        // to be extracted from the resulting XML-Document and than be applied to
        // InputOffset for the given channel
#if 0
        // sOffsetValue has to be extracted from XML (not shown in this example)
        snprintf(sOffsetValue, sizeof(sOffsetValue), "%f", 1.2345); 

        snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AI0", nBoardID);
        nErrorCode = DeWeSetParamStruct_str( sChannelStr, "InputOffset", sOffsetValue);
        checkError(nErrorCode);
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_AI, 0);
        checkError(nErrorCode);
# else
        DumpXmlTree(sSettingStr);
#endif
        printf("\n\nSensor Balancing finished for Board %d\n", nBoardID);
    };

    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}


