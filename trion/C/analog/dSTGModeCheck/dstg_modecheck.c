/**
 * Short example to showcase the dSTG mode check functionality.
 *
 * This example should be used with a TRION-2402-dSTG-6.LE as board 0
 *
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Setup the channel properties for bridge measurement
 *  - Peform mode check
 *  - Print of mode check report
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG",
                                     NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nBoardID = 0;
    uint32 nLen=0;
    char* pcBuff=0;
    char sErrorText[256]  = {0};
    char sSettingStr[256] = {0};
    char sBoardID[256] = {0};

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
        return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using the DEWE2 Explorer.");
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
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI0)
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AI0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Used", "True");
    CheckError(nErrorCode);

    // Also set up the channel properties
    // First the mode is selected. In this case it will be Bridge measurement mode
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Mode", "Bridge");
    CheckError(nErrorCode);

    // The bridge type is Quarter bridge with 3 wires
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "InputType", "BRQUARTER3W");
    CheckError(nErrorCode);

    // The bridge resistanmce is 350 Ohm
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "BridgeRes", "350");
    CheckError(nErrorCode);

    // 5V of excitation will be applied to the bridge
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Excitation", "5V");
    CheckError(nErrorCode);

    // The measurement range will be 100 mV/V
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Range", "100 mV/V");
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

    // Start the actual mode check.
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ModeCheck", "" );
    CheckError(nErrorCode);

    // In this case the mode check call will retrun only after all mode checks have been performed
    // As it is a synchronous call no polling is needed

    // Get the length of the check report
    DeWeGetParamStruct_strLEN( sSettingStr , "ModeCheck", &nLen );
    CheckError(nErrorCode);

    // Allocate a buffer big enough
    pcBuff = (char*)malloc(nLen + 1);

    // Get the actual report
    DeWeGetParamStruct_str( sSettingStr , "ModeCheck", pcBuff, nLen + 1 );
    CheckError(nErrorCode);

    //Print the results
    printf("\n%s\n", pcBuff);

    // Free buffer
    free(pcBuff);

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
