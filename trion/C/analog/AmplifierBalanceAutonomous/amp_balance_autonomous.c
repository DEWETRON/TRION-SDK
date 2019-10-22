/**
 * Short example to describe how to trigger an amplifier balance
 *
 * for analogue channels of a specific board
 *
 *
 * In this example no application-owned acquisition is running
 * so the balancing-operation will execute blocking, and will not
 * return until finished
 *
 * After execution the new offset-correction values are already
 * written to the e"prom data of the board
 * Any subsequent measurement will use this new correction values
 *
 * reading back the values, and printing them out is only added
 * to supply visual feedback in this example
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dACC",
                                    "TRION-2402-dSTG",
                                    "TRION-2402-MULTI",
                                    "TRION-1620-ACC",
                                    "TRION-1820-POWER",
                                    "TRION-1810M-POWER",
                                    NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nBoardID = 0;
    char sBoardID[256]    = {0};
    char sChannelStr[256] = {0};
    char sErrorText[256]  = {0};
    char sSettingStr[32*1024] = {0};  //The result-set will be an XML-Document, that may be rather large
    char* pc = NULL;

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

    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BOARD_BASEEEPROM_RESTORE_BACKUP, 0);
    CheckError(nErrorCode);

    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BOARD_RESET_SELFCAL, 0);
    CheckError(nErrorCode);

    // Do not start an Acquisition
    // Just set the AmplifierOffset command to the specific Board for all Channels
    // The Balancing duration for each of the ranges shall be 100msec
    // In this case (no own acquisition running) this operation is blocking
    snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AIAll", nBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "AmplifierOffset", "100msec");
    CheckError(nErrorCode);

    // so the final result is available with the next line of code
    nErrorCode = DeWeGetParamStruct_str( sChannelStr, "AmplifierOffset", sSettingStr, sizeof(sSettingStr));
    CheckError(nErrorCode);
    if ( nErrorCode > 0 )
    {
        //Error - Trap
        printf("Error during Balancing: %s\nAborting.....\n", DeWeErrorConstantToString(nErrorCode));
    }
    else
    {
        // here the Balancing operation is finished
        pc = sSettingStr;
        while ( '\0' != *pc )
        {
            printf("%c", *pc);
            if ( '>' == *pc )
                printf("\n");
            ++pc;
        }
        printf("\n\nAmplifier Balancing finished for Board %d\nThe offset-correction values are written to E2Prom\n", nBoardID);
    };

    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
