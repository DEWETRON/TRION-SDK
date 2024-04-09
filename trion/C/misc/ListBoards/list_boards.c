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

int doListBoards();

int nUseAllCommands = 0;
int nUseBoardReset = 0;
int nUseThreads = 0;
int nUseMergeCache = 0;
int nShowKey = 0;
int ntestLoop = 1;

int main(int argc, char* argv[])
{
    int nErrorCode = 0;
    const char* sThreadPoolSize = 0;
    int i = 0;

    if (argc > 1)
    {
        for (i = 1; i < argc; ++i)
        {
            if (strncmp(argv[i], "--all", sizeof("--all")) == 0)
            {
                // use advanced board reset
                nUseAllCommands = 1;
            }

            if (strncmp(argv[i], "--reset", sizeof("--reset")) == 0)
            {
                // use advanced board reset
                nUseBoardReset = 1;
            }
            else if (strncmp(argv[i], "--thread", sizeof("--thread")) == 0)
            {
                // enable the usage of thread
                nUseThreads = 1;
            }
            else if (strncmp(argv[i], "--pool", sizeof("--pool")) == 0)
            {
                if (argc > (i + 1))
                {
                    sThreadPoolSize = argv[i + 1];
                }
            }
            else if (strncmp(argv[i], "--runs", sizeof("--runs")) == 0)
            {
                if (argc > (i + 1))
                {
                    ntestLoop = atoi(argv[i + 1]);
                }
            }
            else if (strncmp(argv[i], "--cache", sizeof("--cache")) == 0)
            {
                // enable the usage of thread
                nUseMergeCache = 1;
            }
            else if (strncmp(argv[i], "--key", sizeof("--key")) == 0)
            {
                // enable the usage of thread
                nShowKey = 1;
            }
            else if (strncmp(argv[i], "--help", sizeof("--help")) == 0)
            {
                printf("Usage: Listboards [-all] [--reset] [--thread] [--pool NUM] [--cache]\n");
                printf("  --all         use _ALL commands for open,close,reset\n");
                printf("  --reset       do a board reset\n");
                printf("  --thread      use threads in the TRION API for performance improvements\n");
                printf("  --pool NUM    used number of threads in the TRION API for performance improvements\n");
                printf("  --runs NUM    number of re-runs of this example\n");
                printf("  --cache       use cached merge documents\n");
                printf("  --key         show board key info\n");
                printf("  --help    s   how this usage screen\n");
                return 0;
            }
        }

    }

    while (--ntestLoop >= 0)
    {
        printf("Test Loop: %d\n\n", ntestLoop);

        // Load pxi_api.dll
        if (0 != LoadTrionApi())
        {
            return 1;
        }


        if (nUseThreads)
        {
            char buffer[1024] = {0};

            nErrorCode = DeWeSetParamStruct_str("driver/api/config/thread", "Enabled", "true");
            CheckError(nErrorCode);

            if (sThreadPoolSize)
            {
                nErrorCode = DeWeSetParamStruct_str("driver/api/config/thread", "PoolSize", sThreadPoolSize);
                CheckError(nErrorCode);
            }

        }

        if (nUseMergeCache)
        {
            nErrorCode = DeWeSetParamStruct_str("driver/api/config/xml", "AllowCachedMergeResult", "true");
            CheckError(nErrorCode);
        }
        else
        {
            nErrorCode = DeWeSetParamStruct_str("driver/api/config/xml", "AllowCachedMergeResult", "false");
            CheckError(nErrorCode);
        }

        printf("Options used:\n");
        printf("  --all    => %s\n", nUseAllCommands ? "yes" : "no");
        printf("  --reset  => %s\n", nUseBoardReset ? "yes" : "no");
        printf("  --thread => %s\n", nUseThreads ? "yes" : "no");
        printf("  --pool   => %s\n", sThreadPoolSize);
        printf("  --cache  => %s\n", nUseMergeCache ? "yes" : "no");

        // use sim:
        DeWeSetParamStruct_str("driver/api/config/simulation", "mode", "SIMMODE_FORCE");
        doListBoards();

        // use hw:
        DeWeSetParamStruct_str("driver/api/config/simulation", "mode", "SIMMODE_OFF");
        doListBoards();



        // Unload pxi_api.dll
        UnloadTrionApi("\nEnd Of Example\n");
    }

    return nErrorCode;
}


int doListBoards()
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nBoardID = 0;
    int nSlotNo = 0;
    int nSegmentNo = 0; // for internal chassis controller
    int nEncID = 0;
    char sPath[256]      = { 0 };
    char sBoardName[256] = {0};
    char sSerialNo[256] = {0};
    char sEncID[256]    = {0};
    char sSlotNo[256]   = {0};
    char sSegmentNo[256]  = {0};
    char sBoardID[256]  = {0};
    char sKey[1024] = { 0 };
    char sBuffer[2048] = { 0 };

    // Initialize driver and retrieve the number of TRION boards
    // nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode = DeWeDriverInit(&nNoOfBoards);
    CheckError(nErrorCode);

    if (nNoOfBoards == 0)
    {
        printf("No TRION board found\n");
    }

    if (nNoOfBoards < 0)
    {
        printf("Trion API is set to use simulated boards\n");
    }
    else
    {
        printf("Trion API is set to use real boards\n");
    }

    nNoOfBoards = abs(nNoOfBoards);

    if (nNoOfBoards == 0)
    {
        printf("No boards found\n");
        DeWeDriverDeInit();
        return ERR_NONE;
    }


    nErrorCode = DeWeGetParamStruct_str("driver/api", "SystemVersions", sBuffer, sizeof(sBuffer));
    //if (ERR_NONE == nErrorCode) printf("Trion Versions : %s\n", sBuffer);

    nErrorCode = GetApiPath(TRION_CONFIG_PATH, sPath, sizeof(sPath));
    if (ERR_NONE == nErrorCode) printf("Trion API config path : %s\n", sPath);

    nErrorCode = GetApiPath(TRION_LOG_PATH, sPath, sizeof(sPath));
    if (ERR_NONE == nErrorCode) printf("Trion API log path    : %s\n", sPath);

    nErrorCode = GetApiPath(TRION_BACKUP_PATH, sPath, sizeof(sPath));
    if (ERR_NONE == nErrorCode) printf("Trion API backup path : %s\n", sPath);

    // enclosure info
    {
        char sEncName[32] = {0};
        snprintf(sBoardID, sizeof(sBoardID), "BoardID%d/boardproperties/SystemInfo/EnclosureInfo", 0);
        nErrorCode = DeWeGetParamXML_str(sBoardID, "Name", sEncName, sizeof(sEncName));
        CheckError(nErrorCode); 
        printf("Enclosure name: %s\n", sEncName);
    }

    printf("%-7s %-7s %-8s %-30s %s\n", "EncID", "SlotNo[i]", "BoardID", "Name", "SerialNo");
    printf("------------------------------------------------------------------------\n");

    // iterate all boards for open
    if (nUseAllCommands)
    {
        // Open ALL
        nErrorCode = DeWeSetParam_i32(0, CMD_OPEN_BOARD_ALL, 0);
        CheckError(nErrorCode);
    }
    else
    {
        for (nBoardID = 0; nBoardID < nNoOfBoards; ++nBoardID)
        {
            // Open
            nErrorCode = DeWeSetParam_i32(nBoardID, CMD_OPEN_BOARD, 0);
            CheckError(nErrorCode);
        }
    }

    // iterate all boards for reset
    if (nUseBoardReset)
    {
        if (nUseAllCommands)
        {
            // Reset ALL
            nErrorCode = DeWeSetParam_i32(0, CMD_RESET_BOARD_ALL, 0);
            CheckError(nErrorCode);
        }
        else
        {
            for (nBoardID = 0; nBoardID < nNoOfBoards; ++nBoardID)
            {
                // Reset
                nErrorCode = DeWeSetParam_i32(nBoardID, CMD_RESET_BOARD, 0);
                CheckError(nErrorCode);
            }
        }
    }


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

        // Slot ID for internal controller devices (DEWE3)
        nErrorCode = DeWeGetParamXML_str(sBoardID, "InternalSegmentNo", sSegmentNo, sizeof(sSegmentNo));
        CheckError(nErrorCode);
        sscanf(sSegmentNo, "%d", &nSegmentNo);

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
        if (nShowKey)
        {
            snprintf(sBoardID, sizeof(sBoardID), "BoardID%d", nBoardID);
            nErrorCode = DeWeGetParamStruct_str(sBoardID, "key", sKey, sizeof(sKey));
            CheckError(nErrorCode);
            printf("  %-7d %-2d[%d] %5d      %-30s %-12s <%s>\n", nEncID, nSlotNo, nSegmentNo, nBoardID, sBoardName, sSerialNo, sKey);
        }
        else
        {
            printf("  %-7d %-2d[%d] %5d      %-30s %-12s\n", nEncID, nSlotNo, nSegmentNo, nBoardID, sBoardName, sSerialNo);
        }
    }


    if (nUseAllCommands)
    {
        // Reset ALL
        nErrorCode = DeWeSetParam_i32(0, CMD_CLOSE_BOARD_ALL, 0);
        CheckError(nErrorCode);
    }
    else
    {
        // iterate all boards
        for (nBoardID = 0; nBoardID < nNoOfBoards; ++nBoardID)
        {
            // Close the board connection
            nErrorCode = DeWeSetParam_i32(nBoardID, CMD_CLOSE_BOARD, 0);
            CheckError(nErrorCode);
        }
    }


    DeWeDriverDeInit();

    return ERR_NONE;
}

