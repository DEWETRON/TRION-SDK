/**
 * Short example to showing how to list all installed boards.
 * Shows: - used configuration directory
 *        - BoardID, Board Name, SlotNo
 *
 * Note: look into the generated Board[0..127]_Properties.xml to see were
 * SlotNo and SerialNo information is stored.
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"
#include <stdio.h>

void configureNetwork();
int doListBoards();

int ntestLoop = 1;

int main(int argc, char* argv[])
{
    int nErrorCode = 0;

    while (--ntestLoop >= 0)
    {
        printf("Test Loop: %d\n\n", ntestLoop);

        // Load pxi_api.dll (the TRIONET Wrapper API)
        if (0 != LoadTrionApi())
        {
            return 1;
        }

        // get access to trionet
        configureNetwork();

        // use hw:
        doListBoards();

        // Unload pxi_api.dll
        UnloadTrionApi("\nEnd Of Example\n");
    }

    return nErrorCode;
}


void configureNetwork()
{
    int nErrorCode;

    // Optional: Prints available network interfaces
    // ListNetworkInterfaces();

    char* address = "127.0.0.1";
    char* netmask = "255.255.255.0";

    printf("Example is listening for TRIONET devices on: %s (%s)\n", address, netmask);

    // TODO: Configure the network interface to access TRIONET devices
    nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/LocalIP", address);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/NetMask", netmask);
    CheckError(nErrorCode);
}


int doListBoards()
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nBoardID = 0;
    int nSlotNo = 0;
    int nEncID = 0;
    char sPath[256]      = { 0 };
    char sBoardName[256] = {0};
    char sSerialNo[256] = {0};
    char sEncID[256]    = {0};
    char sSlotNo[256]   = {0};
    char sBoardID[256]  = {0};
    char sKey[1024] = { 0 };

    // Initialize driver and retrieve the number of TRION boards
    // nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode = DeWeDriverInit(&nNoOfBoards);
    CheckError(nErrorCode);

    if (nNoOfBoards == 0)
    {
        printf("No TRION board found\n");
    }

    nNoOfBoards = abs(nNoOfBoards);

    if (nNoOfBoards == 0)
    {
        printf("No boards found\n");
        DeWeDriverDeInit();
        return ERR_NONE;
    }


    printf("%-7s %-7s %-8s %-30s %s\n", "EncID", "SlotNo", "BoardID", "Name", "SerialNo");
    printf("------------------------------------------------------------------------\n");

    // Open ALL
    nErrorCode = DeWeSetParam_i32(0, CMD_OPEN_BOARD_ALL, 0);
    CheckError(nErrorCode);

    // Reset ALL
    nErrorCode = DeWeSetParam_i32(0, CMD_RESET_BOARD_ALL, 0);
    CheckError(nErrorCode);

    // iterate all boards
    for (nBoardID = 0; nBoardID < nNoOfBoards; ++nBoardID)
    {
        // Get the Enclosure ID:
        // Path to <SystemInfo> element in Board[0..127]Properties.xml
        snprintf(sBoardID, sizeof(sBoardID), "BoardID%d/boardproperties/SystemInfo/EnclosureInfo", nBoardID);
        nErrorCode = DeWeGetParamXML_str(sBoardID, "EnclosureID", sEncID, sizeof(sEncID));
        CheckError(nErrorCode);
        sscanf(sEncID, "%d", &nEncID);

        // Get the SlotNo:
        // Path to <SystemInfo> element in Board[0..127]Properties.xml
        //   -> BoardID%d/SystemInfo  (root element <Properies> is ignored by API)
        snprintf(sBoardID, sizeof(sBoardID), "BoardID%d/boardproperties/SystemInfo", nBoardID);
        nErrorCode = DeWeGetParamXML_str(sBoardID, "SlotNo", sSlotNo, sizeof(sSlotNo));
        CheckError(nErrorCode);
        sscanf(sSlotNo, "%d", &nSlotNo);

        // Build a string in the format: "BoardID0", "BoardID1", ...
        snprintf(sBoardID, sizeof(sBoardID), "BoardID%d", nBoardID);

        // Request the TRION board name
        nErrorCode = DeWeGetParamStruct_str(sBoardID, "BoardName", sBoardName, sizeof(sBoardName));
        CheckError(nErrorCode);

        //Build a string to query the XML-stored serial number
        snprintf(sBoardID, sizeof(sBoardID), "BoardID%d/boardproperties/BoardInfo", nBoardID);
        nErrorCode = DeWeGetParamXML_str(sBoardID, "SerialNumber", sSerialNo, sizeof(sSerialNo));
        CheckError(nErrorCode);

        // Print Properties
        {
            printf("  %-7d %-8d %-5d %-30s %-12s\n", nEncID, nSlotNo, nBoardID, sBoardName, sSerialNo);
        }
    }


    // Reset ALL
    nErrorCode = DeWeSetParam_i32(0, CMD_CLOSE_BOARD_ALL, 0);
    CheckError(nErrorCode);


    return ERR_NONE;
}

