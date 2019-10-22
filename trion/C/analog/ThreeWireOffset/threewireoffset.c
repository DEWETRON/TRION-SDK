/**
 * Short example to describe how to trigger a three-wire-offset check
 * to compensate for the internal line-resistance in 3WireType2 input-types
 *
 *
 * After execution the resultset is queried, an printed to the
 * console. No XML evaluation is shown in this example to
 * evade the need of any 3rd-party xml-library.
 *
 * Note: The example is only executable on TRION-2402-MULTI
 *
 */

 

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-MULTI",
                                    NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nErrorCode = 0;
    int nBoardID = 0;
    char sBoardID[256] = {0};
    char sTarget[256] = {0};
    char sErrorString[256] = {0};
    char threewireresult_str[32*1024] = {0};  //The result-set will be an XML-Document, that may be rather large

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
        snprintf(sErrorString, sizeof(sErrorString), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardID, nNoOfBoards);
        return UnloadTrionApi(sErrorString);
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

    // no specific preset is required for the threewireoffset to execute
    // however: The API would push away the boards current setting before the test-run, and restore (on logical level)
    // after the test is run.

    printf("Executing threewireoffset.\nPlease wait....\n");
    //Issue the command - blocking
    snprintf(sTarget, sizeof(sTarget),"%s/AI0", sBoardID);
    
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ThreeWireInternalLineResistance", "");
    if ( nErrorCode > 0 ){
        snprintf( sErrorString, sizeof(sErrorString), "Error on command Set 'ThreeWireInternalLineResistance': %s\n", DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorString);
    }

    //now it is safe to query the resultset from API for this board
    nErrorCode = DeWeGetParamStruct_str( sTarget, "ThreeWireInternalLineResistance", threewireresult_str, sizeof(threewireresult_str));
    if ( nErrorCode > 0 ){
        snprintf( sErrorString, sizeof(sErrorString), "Error on command Get 'ThreeWireInternalLineResistance': %s\n", DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorString);
    }
    DumpXmlTree(threewireresult_str);

    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}


