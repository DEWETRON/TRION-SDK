/**
 * Short example to describe how to communicate with a TEDS
 *
 * This example should be used with a TRION-2402-dSTG.8.RJ as board 0
 *
 * Describes following:
 *
 *      Execution:
 *      1: Read TEDS ROM CODE (TYPE INFORMATION)
 *      2: READ TEDS MEMORY   (Block 1 = Address 32)
 *      3: WRITE TEDS MEMORY  (Commented out to avoid overwriting of sensor data)
 *
 *
 *      Prerequisites:
 *      - any TEDS device connected to BOARD 0 and CHANNEL 0
 *      - Board Type: TRION-2402-dSTG
 */


#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG",
                                    "TRION-1620-LV",
                                    "TRION-1603-LV",
                                    "TRION-2402-MULTI",
                                    "TRION-2402-dACC",
                                    "TRION-1820-POWER",
                                    "TRION-1810M-POWER",
                                    "TRION3-1820-POWER",
                                    "TRION3-1810M-POWER",
                                    NULL };


static int nNoOfBoards = 0;

void configureAllChannels();
void readTEDS();
void writeTEDS(int nBoardId, int nChanId);
BOOL supportsTEDS(int nBoardId, int nChanId);

int main(int argc, char* argv[])
{
    int nBoardId = 0;
    char sBoardId[256] = {0};
    char sErrorText[256] = {0};
    BOOL bEnableWrite = FALSE;

    int nErrorCode = 0;
    int i;
    //const char* channelId = "AI0";

    // look for enable_write command line argument
    for (i = 1; i < argc; ++i)
    {
        if (!strcmp(argv[i], "enable_write"))
        {
            bEnableWrite = TRUE;
        }
    }

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
       return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using DEWETRON Explorer.");
    }

    // Build BoardId -> Either comming from command line (arg 1) or default "0"
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardId) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d", nBoardId);

    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( 0, CMD_OPEN_BOARD_ALL, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( 0, CMD_RESET_BOARD_ALL, 0 );
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardId, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    // After reset all channels are disabled.
    // Enable all channels
    configureAllChannels();


    ///////////////////////
    // Get TEDS ROM CODE //
    ///////////////////////

    readTEDS();
    printf("\n\n");

    if (bEnableWrite)
    {
        int nChanId = 4; // Write to channel 4
        writeTEDS(nBoardId, nChanId);
        printf("\n\n");
    }

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( 0, CMD_CLOSE_BOARD_ALL, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}

void configureAllChannels()
{
    // After reset all channels are disabled.
    char sBoardId[256] = {0};
    int nBoardId = 0;
    int nErrorCode = 0;
    for (nBoardId=0; nBoardId < nNoOfBoards; nBoardId++)
    {
        snprintf(sBoardId, sizeof(sBoardId), "BoardID%d/AIALL", nBoardId);
        nErrorCode = DeWeSetParamStruct_str( sBoardId, "Used", "True");
        CheckError(nErrorCode);
        // Update the hardware with settings
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
        if (CheckError(nErrorCode))
        {
            nErrorCode = DeWeSetParam_i32( 0, CMD_CLOSE_BOARD_ALL, 0);
            CheckError(nErrorCode);
            UnloadTrionApi("-> ERROR CMD_UPDATE_PARAM_ALL ...\n\n ");
            exit(1);
        }
    }

}

BOOL supportsTEDS(int nBoardId, int nChanId)
{
    int j = 0;
    int num_props = 0;
    char sProp[256] = {0};

    char sChannelTarget[256] = {0};
    char sMode[256] = {0};

    snprintf(sChannelTarget, sizeof(sChannelTarget), "BoardID%d/AI%d", nBoardId, nChanId);
    int nErrorCode = DeWeGetParamStruct_str( sChannelTarget, "Mode", sMode, sizeof(sMode));
    CheckError(nErrorCode);

    // Check if the channels supports TEDS
    num_props = TRION_ChanProp_GetNum(nBoardId, nChanId, "AI", sMode, "ChannelFeatures");
    for (j = 0; j < num_props; ++j)
    {
        int nErrorCode = TRION_ChanProp_GetEntry(nBoardId, nChanId, "AI", sMode, "ChannelFeatures", j, sProp, sizeof(sProp));
        if (0 == strnicmp("SupportTEDS", sProp, sizeof(sProp)))
        {
            return TRUE;
        }
    }
    return FALSE;
}

void writeTEDS(int nBoardId, int nChanId)
{
    char TEDS_DATA[128] ={0};
    int nErrorCode = 0;
    char sBoardId[256] = {0};

    printf("Writing EEPROM to BoardID: %d  AI: %d\n", nBoardId, nChanId);

    if (!supportsTEDS(nBoardId, nChanId))
    {
        printf("  TEDS not supported\n");
        return;
    }

    snprintf(sBoardId, sizeof(sBoardId), "BoardID%d/AI%d", nBoardId, nChanId);

    ///////////////////////
    // WRITE TEDS MEMORY //
    ///////////////////////
    printf("Write 32Byte EEPROM DATA to Address Block 1\n");

    nErrorCode = DeWeGetParamStruct_str( sBoardId, "TedsType", TEDS_DATA, sizeof(TEDS_DATA) );
    if (nErrorCode != ERR_NONE)
    {
        printf("ROM CODE DATA READ FAILED!!\n\n");
        return;
    }
    else
    {
        printf("ROM CODE: %s\n", TEDS_DATA);
    }

#if 0
    nErrorCode = DeWeSetParamStruct_str(sBoardId, "TedsMem1", "0022334455667788990011223344556677889900112233445566778899001333");
    if (CheckError(nErrorCode))
    {
        printf("EPROM DATA WRITE FAILED!!\n");
    }
    else
    {
        printf("EEPROM DATA WRITE PASSED\n");
    }
#else
    printf("!!!!!!!!!!!!! NOTE: Not implemented to avoid overwriting of Sensor DATA !!!!!!!!!!!!!!!\n");
#endif

    /////////////////////////////////
    // Get TEDS MEMORY AFTERWARDS ///
    /////////////////////////////////
    printf("\n-> 4. Read back EEPROM DATA\n");
    printf("Read 32Byte EEPROM DATA @ Address Block 1 \n");

    nErrorCode = DeWeGetParamStruct_str(sBoardId, "TedsMem1", TEDS_DATA, sizeof(TEDS_DATA));
    if (CheckError(nErrorCode))
    {
        printf("EEPROM DATA READ FAILED!!\n\n");
    }
    else
    {
        printf("EEPROM DATA: %s\n\n", TEDS_DATA);
    }
}


void readTEDS()
{
    char sBoardId[256] = {0};
    int nBoardId = 0;
    int nErrorCode = 0;
    int nNrOfChannels = 0;
    int nChanId = 0;
    char TEDS_DATA[128] ={0};
    for (nBoardId=0; nBoardId < nNoOfBoards; nBoardId++)
    {
        // determine number of analog channel
        char num_chan[16] = { 0 };
        snprintf(sBoardId, sizeof(sBoardId), "BoardID%d/BoardProperties/BoardFeatures/AI", nBoardId);
        nErrorCode = DeWeGetParamXML_str( sBoardId, "Channels", num_chan, sizeof(num_chan));


        sscanf(num_chan, "%d", &nNrOfChannels);
        printf("BoardID: %d  AI: %d\n", nBoardId, nNrOfChannels);

        for (nChanId = 0; nChanId < nNrOfChannels; nChanId++)
        {
            printf("TEDS BoardID: %d  AI: %d\n", nBoardId, nChanId);

            if (!supportsTEDS(nBoardId, nChanId))
            {
                printf("  TEDS not supported\n");
                continue;
            }

            snprintf(sBoardId, sizeof(sBoardId), "BoardID%d/AI%d", nBoardId, nChanId);

            //printf("\n-> 1. Get ROM Code:\n");
            nErrorCode = DeWeGetParamStruct_str( sBoardId, "TedsType", TEDS_DATA, sizeof(TEDS_DATA) );
            if (nErrorCode != ERR_NONE)
            {
                //printf("ROM CODE DATA READ FAILED!!\n\n");
            }
            else
            {
                printf("ROM CODE: %s\n\n",TEDS_DATA);
            }

            if (nErrorCode == ERR_NONE)
            {
                printf("\n-> 2. Get EEPROM DATA\n");
                printf("Read 32Byte EEPROM DATA @ Address Block 0 \n");
                nErrorCode = DeWeGetParamStruct_str(sBoardId, "TedsMem0", TEDS_DATA, sizeof(TEDS_DATA));
                if (CheckError(nErrorCode))
                {
                    //printf("EEPROM DATA READ FAILED!!\n\n");
                }
                else
                {
                    printf("EEPROM DATA 0: %s\n\n", TEDS_DATA);
                }


                printf("\n-> 2. Get EEPROM DATA\n");
                printf("Read 32Byte EEPROM DATA @ Address Block 1 \n");
                nErrorCode = DeWeGetParamStruct_str( sBoardId , "TedsMem1", TEDS_DATA, sizeof(TEDS_DATA) );
                if (CheckError(nErrorCode))
                {
                    //printf("EEPROM DATA READ FAILED!!\n\n");
                }
                else
                {
                    printf("EEPROM DATA 1: %s\n\n",TEDS_DATA);
                }

            }
        }
    }

}
