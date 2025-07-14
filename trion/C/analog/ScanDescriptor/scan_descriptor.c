/**
 * ScanDescriptor Example
 * Shows the sample data layout
 *
 * This example shows how scan descriptors can be used.
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"
#include <stdio.h>


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode  = 0;
    int nBoardID    = 0;
    char sTarget[256] = {0};
    char sBoardName[256] = {0};
    char* sScanDescriptor = 0;
    uint32  nScanDescriptorLen = 0;
    const char* csScanDescriptorCommand = NULL;

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

    if (nNoOfBoards == 0)
    {
        return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using the DEWE2 Explorer.");
    }

    for (nBoardID=0; nBoardID < nNoOfBoards; ++nBoardID)
    {
        // Open the board to be able to access it
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_OPEN_BOARD, 0 );
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_RESET_BOARD, 0 );
        CheckError(nErrorCode);

        // Enable All AI channels
        snprintf(sTarget, sizeof(sTarget), "BoardID%d/AIAll", nBoardID);
        nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
        CheckError(nErrorCode);
        // Enable All CNT channels
        snprintf(sTarget, sizeof(sTarget), "BoardID%d/CntAll", nBoardID);
        nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
        CheckError(nErrorCode);
        // Enable All DI channels
        snprintf(sTarget, sizeof(sTarget), "BoardID%d/DISCRETALL", nBoardID);
        nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
        CheckError(nErrorCode);

        // Enable All BoardCNT channels
        snprintf(sTarget, sizeof(sTarget), "BoardID%d/BoardCNTAll", nBoardID);
        nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
        CheckError(nErrorCode);

        // A measurement buffer has to be configured as a necessary pre step
        // for CMD_UPDATE_PARAM_ALL
        nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_BLOCK_SIZE, 1000);
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_BLOCK_COUNT, 10);
        CheckError(nErrorCode);

        // Commit settings
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
        CheckError(nErrorCode);

        // Retrieve Scan Descriptor for an individual board
        snprintf(sTarget, sizeof(sTarget), "BoardID%d", nBoardID);

        // First, try to retrieve the latest version of the ScanDescriptor
        csScanDescriptorCommand = "ScanDescriptor_V3";
        nErrorCode = DeWeGetParamStruct_strLEN(sTarget, csScanDescriptorCommand, &nScanDescriptorLen);
        if (nErrorCode != 0)
        {
            // Fallback that requests a ScanDescriptor Version 1
            csScanDescriptorCommand = "ScanDescriptor";
            nErrorCode = DeWeGetParamStruct_strLEN(sTarget, csScanDescriptorCommand, &nScanDescriptorLen);
            CheckError(nErrorCode);
        }

        // allocate a buffer for the ScanDescriptor
        sScanDescriptor = malloc(nScanDescriptorLen + 1);

        nErrorCode = DeWeGetParamStruct_str(sTarget, csScanDescriptorCommand, sScanDescriptor, nScanDescriptorLen + 1);
        CheckError(nErrorCode);

        nErrorCode = DeWeGetParamStruct_str(sTarget, "BoardName", sBoardName, sizeof(sBoardName));
        CheckError(nErrorCode);

        printf("Scan descriptor for %s: \n%s\n", sBoardName, sScanDescriptor);

        // free ScanDescriptor buffer
        free(sScanDescriptor);
        sScanDescriptor = NULL;

        // Close the board connection
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0 );
        CheckError(nErrorCode);
    }

    // Unload TRION api
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
