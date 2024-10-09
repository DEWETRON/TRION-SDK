/**
 * Short example to describe how to communicate with a TEDS
 *
 * This example should be used with a TRION-2402-dSTG.8.RJ as board 0
 *
 * Describes following:
 *
 *      Usage:
 *      - No parameters: Try to read TEDS information from all channels of board 0 and print them on the screen
 *      - ch=BoardIdXXXX/AIYYY: channel selection, read/write only board XXX and channel YYY
 *      - if=path/to/file.xml: input file for writing (nothing is written without a valid input file or no selected ch)
 *      - of=path/to/file.xml: output file when reading a TEDS (requires a valid ch selection)
 *
 *      Prerequisites:
 *      - any TEDS device connected to BOARD 0 and CHANNEL 0
 *      - Board Type: TRION-2402-dSTG or similar
 */


#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

#include <stdlib.h>
#include <string.h>

struct ProgramSettings
{
    int selected_board;
    int selected_channel;
    char input_filename[256];
    char output_filename[256];
};

int readTEDS(int nBoardId, int nNumAIChannels, const struct ProgramSettings* settings);
int readSingleTEDS(int nBoardId, int nChannelIndex, const char* pszOutputFile);
int writeSingleTEDS(int nBoardId, int nChannelIndex, const char* pszOutputFile);
int processArgs(int argc, char* argv[], struct ProgramSettings* settings);

//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG",
                                    "TRION-1620-LV",
                                    "TRION-1603-LV",
                                    "TRION-2402-MULTI",
                                    "TRION-2402-dACC",
                                    "TRION-1820-POWER",
                                    "TRION3-1820-POWER",
                                    "TRION-1820-MULTI",
                                    "TRION-1810M-POWER",
                                    "TRION3-1810M-POWER",
                                    "TRION-1802-dLV",
                                    "TRION-1600-dLV",
                                    NULL };


int main(int argc, char* argv[])
{
    int nBoardId = 0;
    char sBoardId[256] = {0};
    char sTarget[256] = {0};
    char sResult[256] = {0};
    char sErrorText[256] = {0};
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nNumAIChannels = 0;
    struct ProgramSettings settings;
    //const char* channelId = "AI0";

    if (ERR_NONE != processArgs(argc, argv, &settings))
    {
        return EXIT_FAILURE;
    }

    // Load pxi_api.dll
    if ( 0 != LoadTrionApi() )
    {
        return EXIT_FAILURE;
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
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardId) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }


    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardId, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    // Determine number of AI channels
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d/AI", nBoardId);
    nErrorCode = DeWeGetParamStruct_str(sBoardId, "Channels", sResult, sizeof(sResult));
    CheckError(nErrorCode);
    snscanf(sResult, sizeof(sResult), "%d", &nNumAIChannels);

    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d", nBoardId);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI)
    //snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, channelId);
    snprintf(sTarget, sizeof(sTarget), "%s/AIALL", sBoardId);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    if (CheckError(nErrorCode))
    {
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD, 0);
        CheckError(nErrorCode);
        return UnloadTrionApi("-> ERROR before TEDS Action ...\n\n ");
    }


    // Query the AI channels for a TEDS sensor
    nErrorCode = readTEDS(nBoardId, nNumAIChannels, &settings);

    // Warning: when this code is executed on real hardware, existing TEDS chips will be overwritten without warning!
    if (settings.selected_channel >= 0 && settings.input_filename[0] != '\0')
    {
        nErrorCode = writeSingleTEDS(settings.selected_board, settings.selected_channel, settings.input_filename);
    }

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}

int readSingleTEDS(int nBoardId, int nChannelIndex, const char* pszOutputFile)
{
    int nErrorCode = ERR_NONE;
    int teds_supported = 0;
    int num_props = 0;
    char current_mode[80] = {0};
    char sTarget[256] = {0};
    int j;

    snprintf(sTarget, sizeof(sTarget), "BoardId%d/AI%d", nBoardId, nChannelIndex);

    // Check if the channels supports TEDS
    nErrorCode = DeWeGetParamStruct_str( sTarget, "Mode", current_mode, sizeof(current_mode));
    num_props = TRION_ChanProp_GetNum(nBoardId, nChannelIndex, "AI", current_mode, "ChannelFeatures");
    for (j = 0; j < num_props; ++j)
    {
        char sProp[256] = {0};
        nErrorCode = TRION_ChanProp_GetEntry(nBoardId, nChannelIndex, "AI", current_mode, "ChannelFeatures", j, sProp, sizeof(sProp));

        if (0 == strnicmp("SupportTEDS", sProp, sizeof(sProp)))
        {
            teds_supported = 1;
        }
    }

    if (!teds_supported)
    {
        printf("Scanning AI%d... no support\n", nChannelIndex);
        nErrorCode = WARNING_TEDS_NOT_SUPPORTED;
    }
    else
    {
        char TEDS_DATA[32*1024] ={0};       //large enough for even the 20kbit E2Prom
        char sTarget[256] = {0};
        FILE* fOut = NULL;

        if (pszOutputFile && pszOutputFile[0] != '\0')
        {
            fOut = fopen(pszOutputFile, "w");
            if (fOut == NULL)
            {
                fprintf(stderr, "Cannot open file %s for writing\n", pszOutputFile);
                return ERROR_XML_FILE_NOT_FOUND;
            }
        }

        printf("Scanning AI%d...\n", nChannelIndex);

        snprintf(sTarget, sizeof(sTarget), "BoardID%d/AI%d", nBoardId, nChannelIndex);

        ////////////////////////////
        // Get TEDS Complete Data //
        ////////////////////////////

        TRION_StopWatchHandle sw;
        TRION_StopWatch_Create(&sw);
        TRION_StopWatch_Start(sw);

        // TedsReadEx fails if there are multiple 1wire devices
        nErrorCode = DeWeGetParamStruct_str( sTarget, "TedsReadEx", TEDS_DATA, sizeof(TEDS_DATA) );
        if (CheckError(nErrorCode))
        {
            printf("ROM CODE DATA READ FAILED!!\n\n");
        }
        else
        {
            printf("TEDS XML:\n%s\n\n",TEDS_DATA);
            if (fOut)
            {
                fputs(TEDS_DATA, fOut);
            }
        }
        TRION_StopWatch_Stop(sw);
        printf("TedsReadEx      %lu ms\n", TRION_StopWatch_GetMS(sw));
        TRION_StopWatch_Destroy(&sw);

        TRION_StopWatch_Create(&sw);
        TRION_StopWatch_Start(sw);


        // TedsReadExChain is slower but is capable of detecting all 1wire devices on a bus
        nErrorCode = DeWeGetParamStruct_str(sTarget, "TedsReadExChain", TEDS_DATA, sizeof(TEDS_DATA));
        if (CheckError(nErrorCode))
        {
            printf("ROM CODE DATA READ FAILED!!\n\n");
        }
        else
        {
            printf("TEDS XML (Chain):\n%s\n\n", TEDS_DATA);
            if (fOut)
            {
                fputs(TEDS_DATA, fOut);
            }
        }

        TRION_StopWatch_Stop(sw);
        printf("TedsReadExChain %lu ms\n", TRION_StopWatch_GetMS(sw));
        TRION_StopWatch_Destroy(&sw);


        if (fOut)
        {
            fclose(fOut);
        }
    }


    return nErrorCode;
}

int writeSingleTEDS(int nBoardId, int nChannelIndex, const char* pszInputFile)
{
    int nErrorCode = ERR_NONE;
    int teds_supported = 0;
    int num_props = 0;
    char current_mode[80] = {0};
    char sTarget[256] = {0};
    int j;

    snprintf(sTarget, sizeof(sTarget), "BoardId%d/AI%d", nBoardId, nChannelIndex);

    // Check if the channels supports TEDS
    nErrorCode = DeWeGetParamStruct_str( sTarget, "Mode", current_mode, sizeof(current_mode));
    num_props = TRION_ChanProp_GetNum(nBoardId, nChannelIndex, "AI", current_mode, "ChannelFeatures");
    for (j = 0; j < num_props; ++j)
    {
        char sProp[256] = {0};
        nErrorCode = TRION_ChanProp_GetEntry(nBoardId, nChannelIndex, "AI", current_mode, "ChannelFeatures", j, sProp, sizeof(sProp));

        if (0 == strnicmp("SupportTEDS", sProp, sizeof(sProp)))
        {
            teds_supported = 1;
        }
    }

    if (!teds_supported)
    {
        printf("Writing AI%d... no support\n", nChannelIndex);
        nErrorCode = WARNING_TEDS_NOT_SUPPORTED;
    }
    else
    {
        char TEDS_DATA[32*1024] = {0};       //large enough for even the 20kbit E2Prom
        char sTarget[256] = {0};
        FILE* fIn = NULL;

        if (pszInputFile && pszInputFile[0] != '\0')
        {
            fIn = fopen(pszInputFile, "r");
        }
        if (fIn == NULL)
        {
            fprintf(stderr, "Cannot open file %s for reading\n", pszInputFile);
            return ERROR_XML_FILE_NOT_FOUND;
        }
        else
        {
            size_t bytes_read = fread(TEDS_DATA, sizeof(char), sizeof(TEDS_DATA) - 1, fIn);
            fclose(fIn);
            if (bytes_read == 0)
            {
                return ERROR_XML_FILE_NOT_FOUND;
            }
        }

        printf("Writing to AI%d:\n", nChannelIndex);
        printf("%s\n\n", TEDS_DATA);

        snprintf(sTarget, sizeof(sTarget), "BoardID%d/AI%d", nBoardId, nChannelIndex);

        nErrorCode = DeWeSetParamStruct_str( sTarget, "TedsWriteEx", TEDS_DATA );
        if (CheckError(nErrorCode) || nErrorCode != ERR_NONE)
        {
            printf("EEPROM CODE DATA WRITE FAILED!!\n\n");
        }
        else
        {
            printf("EEPROM CODE DATA WRITTEN SUCCESSFULLY!\n\n");
        }
    }

    return nErrorCode;
}

int readTEDS(int nBoardId, int nNumAIChannels, const struct ProgramSettings* settings)
{
    int nErrorCode = ERR_NONE;
    const int begin_channel = settings->selected_channel >= 0 ? settings->selected_channel : 0;
    const int end_channel =   settings->selected_channel >= 0 ? settings->selected_channel + 1 : nNumAIChannels;
    int i = 0;

    if (begin_channel >= nNumAIChannels || end_channel > nNumAIChannels)
    {
        fprintf(stderr, "Invalid channel number: %d\n", begin_channel);
        return ERR_INVALID_CHANNEL_NO;
    }

    for (i = begin_channel; i < end_channel; ++i)
    {
        int nSingleErrorCode = readSingleTEDS(nBoardId, i, settings->output_filename);
        if (nSingleErrorCode > 0)
        {
            nErrorCode = nSingleErrorCode;
        }
    }

    return nErrorCode;
}

int processArgs(int argc, char* argv[], struct ProgramSettings* settings)
{
    int nErrorCode = ERR_NONE;
    int n;

    memset(settings, 0, sizeof(struct ProgramSettings));
    settings->selected_channel = -1;

    for (n = 1; n < argc; ++n)
    {
        if (strstr(argv[n], "if=") == argv[n])
        {
            strncpy(settings->input_filename, argv[n] + 3, sizeof(settings->input_filename));
        }
        else if (strstr(argv[n], "of=") == argv[n])
        {
            strncpy(settings->output_filename, argv[n] + 3, sizeof(settings->output_filename));
        }
        else if (strstr(argv[n], "ch=") == argv[n])
        {
            int nBoard, nChannel;
            if (2 == sscanf(argv[n] + 3, "BoardId%d/AI%d", &nBoard, &nChannel))
            {
                settings->selected_board = nBoard;
                settings->selected_channel = nChannel;
            }
            else
            {
                fprintf(stderr, "Invalid channel format: %s\n", argv[n] + 3);
                return ERR_INVALID_VALUE;
            }
        }
    }

    return nErrorCode;
}
