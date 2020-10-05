/**
 * Short example to describe how to communicate with a TEDS
 *
 * This example should be used with a TRION-18xx-POWER as board 0
 *
 * Describes following:
 *
 *      Usage:
 *      - Initialize a TEDS chip and configure some values
 *      - Use command line arguments such as:
 *        interfacingTEDSCal ch=BoardId0/AI4 teds=TRION-POWER-SUB-CUR-20A-1 serial=1234 prop=AdcMinValue:-40.2 prop=AdcMaxValue:39.98
 *        to initizlize the TEDS chip on Board0/AI4
 *        to set the AdcMinValue to -40.2 and AdcMaxValue to 39.98
 *
 *      WARNING:
 *        OVERWRITES ALL DATA ON THE TEDS CHIP WITHOUT WARNING!!!
 *
 *      Prerequisites:
 *      - any TEDS device connected to BOARD 0 and CHANNEL 0
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

#include <stdlib.h>
#include <string.h>

struct CalParameter
{
    char name[80];
    char value[80];
    struct CalParameter* next_parameter;
};

struct ProgramSettings
{
    int selected_board;
    int selected_channel;
    char teds_data[1024];
    uint32 serial;
    struct CalParameter* parameter_list;
};

int isTEDSSupported(int nBoardId, int nChannelIndex);
int initSingleTEDS(int nBoardId, int nChannelIndex, const char* data);
int calibrateSingleTEDS(int nBoardId, int nChannelIndex, uint32 serial, struct CalParameter* parameter_list);
int readSingleTEDS(int nBoardId, int nChannelIndex);
int processArgs(int argc, char* argv[], struct ProgramSettings* settings);
void cleanupSettings(struct ProgramSettings*);

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


int main(int argc, char* argv[])
{
    char sBoardId[256] = {0};
    char sTarget[256] = {0};
    char sResult[256] = {0};
    char sErrorText[256] = {0};
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nNumAIChannels = 0;
    struct ProgramSettings settings;

    if (argc == 1)
    {
        printf("Usage example: interfacingTEDSCal ch=BoardId0/AI0 teds=TRION-POWER-SUB-CUR-20A-1 serial=1234 prop=AdcMinValue:-40.2 prop=AdcMaxValue:39.98\n");
        printf("Supported TEDS types:\n");
        printf("    TRION-POWER-SUB-CUR-20A-1, TRION-POWER-SUB-CUR-2A-1, TRION-POWER-SUB-CUR-1A-1, TRION-POWER-SUB-CUR-02A-1B\n");
        printf("    TRION-POWER-SUB-dLV-1V, TRION-POWER-SUB-dLV-5V, TRION-SUB-5V, TRION-SUB-600V\n\n");
        printf("WARNING: All data on the specified channel is overwritten.\n");
        return EXIT_SUCCESS;
    }

    if (ERR_NONE != processArgs(argc, argv, &settings))
    {
        cleanupSettings(&settings);
        return EXIT_FAILURE;
    }

    if (settings.selected_channel < 0)
    {
        printf("No channel specified");
        cleanupSettings(&settings);
        return EXIT_FAILURE;
    }

    if (strlen(settings.teds_data) == 0)
    {
        printf("No TEDS data specified. Use the commandline parameter teds=data");
        cleanupSettings(&settings);
        return EXIT_FAILURE;
    }

    // Load pxi_api.dll
    if ( 0 != LoadTrionApi() )
    {
        cleanupSettings(&settings);
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
        cleanupSettings(&settings);
        return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using the DEWETRON Explorer.");
    }

    if (settings.selected_board < 0 || settings.selected_board >= nNoOfBoards)
    {
        cleanupSettings(&settings);
        return UnloadTrionApi("Invalid board specified\n");
    }

    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(settings.selected_board, sBoardNameNeeded))
    {
        cleanupSettings(&settings);
        return UnloadTrionApi(NULL);
    }

    // Determine number of AI channels
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d/AI", settings.selected_board);
    nErrorCode = DeWeGetParamStruct_str(sBoardId, "Channels", sResult, sizeof(sResult));
    CheckError(nErrorCode);
    snscanf(sResult, sizeof(sResult), "%d", &nNumAIChannels);

    if (settings.selected_channel >= nNumAIChannels)
    {
        cleanupSettings(&settings);
        return UnloadTrionApi("Invalid input channel number\n");
    }

    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d", settings.selected_board);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI)
    //snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, channelId);
    snprintf(sTarget, sizeof(sTarget), "%s/AIALL", sBoardId);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_UPDATE_PARAM_ALL, 0);
    if (CheckError(nErrorCode))
    {
        nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_CLOSE_BOARD, 0);
        CheckError(nErrorCode);
        cleanupSettings(&settings);
        return UnloadTrionApi("-> ERROR before TEDS Action ...\n\n ");
    }

    // Initialize the board with the given TEDS data
    nErrorCode = initSingleTEDS(settings.selected_board, settings.selected_channel, settings.teds_data);
    if (CheckError(nErrorCode))
    {
        nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_CLOSE_BOARD, 0);
        CheckError(nErrorCode);
        cleanupSettings(&settings);
        return UnloadTrionApi("Could not initialize the TEDS chip\n");
    }

    // Read the initialized TEDS chip
    nErrorCode = readSingleTEDS(settings.selected_board, settings.selected_channel);
    if (CheckError(nErrorCode))
    {
        nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_CLOSE_BOARD, 0);
        CheckError(nErrorCode);
        cleanupSettings(&settings);
        return UnloadTrionApi("Could not read the TEDS chip\n");
    }

    if (settings.parameter_list)
    {
        nErrorCode = calibrateSingleTEDS(settings.selected_board, settings.selected_channel,
                                         settings.serial, settings.parameter_list);
        if (CheckError(nErrorCode))
        {
            nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_CLOSE_BOARD, 0);
            CheckError(nErrorCode);
            cleanupSettings(&settings);
            return UnloadTrionApi("Could not calibrate the TEDS chip\n");
        }

        // Read the calibrated TEDS chip
        nErrorCode = readSingleTEDS(settings.selected_board, settings.selected_channel);
        if (CheckError(nErrorCode))
        {
            nErrorCode = DeWeSetParam_i32( settings.selected_board, CMD_CLOSE_BOARD, 0);
            CheckError(nErrorCode);
            cleanupSettings(&settings);
            return UnloadTrionApi("Could not read the TEDS chip\n");
        }
    }

    cleanupSettings(&settings);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}

int isTEDSSupported(int nBoardId, int nChannelIndex)
{
    int nErrorCode = ERR_NONE;
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
            return 1;
        }
    }
    return 0;
}

int initSingleTEDS(int nBoardId, int nChannelIndex, const char* data)
{
    int nErrorCode = ERR_NONE;
    int teds_supported = isTEDSSupported(nBoardId, nChannelIndex);

    if (!teds_supported)
    {
        printf("Scanning AI%d... no support\n", nChannelIndex);
        nErrorCode = WARNING_TEDS_NOT_SUPPORTED;
    }
    else
    {
        char sTarget[256] = {0};
        printf("Initializing AI%d...\n", nChannelIndex);

        snprintf(sTarget, sizeof(sTarget), "BoardID%d/AI%d", nBoardId, nChannelIndex);

        ////////////////////////////
        // Init TEDS              //
        ////////////////////////////
        nErrorCode = DeWeSetParamStruct_str(sTarget, "TedsInitEx", data);
        if (CheckError(nErrorCode))
        {
            printf("INIT TEDS FAILED!!\n\n");
        }
        else
        {
            printf("INIT TEDS SUCCESS.\n\n");
        }
    }

    return nErrorCode;
}

int readSingleTEDS(int nBoardId, int nChannelIndex)
{
    int nErrorCode = ERR_NONE;
    int teds_supported = isTEDSSupported(nBoardId, nChannelIndex);

    if (!teds_supported)
    {
        printf("Scanning AI%d... no support\n", nChannelIndex);
        nErrorCode = WARNING_TEDS_NOT_SUPPORTED;
    }
    else
    {
        char TEDS_DATA[32*1024] ={0};       //large enough for even the 20kbit E2Prom
        char sTarget[256] = {0};

        printf("Scanning AI%d...\n", nChannelIndex);

        snprintf(sTarget, sizeof(sTarget), "BoardID%d/AI%d", nBoardId, nChannelIndex);

        ////////////////////////////
        // Get TEDS Complete Data //
        ////////////////////////////
        nErrorCode = DeWeGetParamStruct_str( sTarget, "TedsReadEx", TEDS_DATA, sizeof(TEDS_DATA) );
        if (CheckError(nErrorCode))
        {
            printf("ROM CODE DATA READ FAILED!!\n\n");
        }
        else
        {
            printf("ROM CODE: %s\n\n",TEDS_DATA);
        }
    }

    return nErrorCode;
}

int calibrateSingleTEDS(int nBoardId, int nChannelIndex, uint32 serial, struct CalParameter* parameter_list)
{
    int nErrorCode = ERR_NONE;
    int teds_supported = isTEDSSupported(nBoardId, nChannelIndex);

    if (!teds_supported)
    {
        printf("Scanning AI%d... no support\n", nChannelIndex);
        nErrorCode = WARNING_TEDS_NOT_SUPPORTED;
    }
    else
    {
        char sTarget[256] = {0};
        char sXPath[256] = {0};

        printf("Reading TEDS on AI%d...\n", nChannelIndex);
        nErrorCode = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_READ, nChannelIndex);
        if (CheckError(nErrorCode))
        {
            printf("Reading TEDS FAILED!!\n\n");
            return nErrorCode;
        }

        snprintf(sTarget, sizeof(sTarget), "BoardID%d/aitedsex/AI%d", nBoardId, nChannelIndex);
        if (serial > 0)
        {
            char s[80];
            printf("Setting serial = %d\n", serial);
            snprintf(s, sizeof(s), "%d", serial);
            snprintf(sXPath, sizeof(sXPath), "TEDSInfo/@Serial");
            nErrorCode = DeWeSetParamXML_str(sTarget, sXPath, s);
            if (CheckError(nErrorCode))
            {
                printf("Failed to write parameter!!\n\n");
                return nErrorCode;
            }

        }
        while (parameter_list)
        {
            printf("Setting %s = %s\n", parameter_list->name, parameter_list->value);
            snprintf(sXPath, sizeof(sXPath), "TEDSData/TEDSInfo/Template/Property[@Name='%s']", parameter_list->name);
            nErrorCode = DeWeSetParamXML_str(sTarget, sXPath, parameter_list->value);
            if (CheckError(nErrorCode))
            {
                printf("Failed to write parameter!!\n\n");
                return nErrorCode;
            }
            parameter_list = parameter_list->next_parameter;
        }

        printf("Synchronizing TEDS on AI%d...\n", nChannelIndex);
        nErrorCode = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_SYNCHRONIZE, nChannelIndex);
        if (CheckError(nErrorCode))
        {
            printf("Synchronizing TEDS FAILED!!\n\n");
            return nErrorCode;
        }

        printf("Writing TEDS on AI%d...\n", nChannelIndex);
        nErrorCode = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_WRITE, nChannelIndex);
        if (CheckError(nErrorCode))
        {
            printf("Writing TEDS FAILED!!\n\n");
            return nErrorCode;
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
        if (strstr(argv[n], "ch=") == argv[n])
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
        else if (strstr(argv[n], "teds=") == argv[n])
        {
            strncpy(settings->teds_data, argv[n] + 5, sizeof(settings->teds_data) - 1);
        }
        else if (strstr(argv[n], "serial=") == argv[n])
        {
            settings->serial = atoi(argv[n] + 7);
        }
        else if (strstr(argv[n], "prop=") == argv[n])
        {
            const char* value_pos = strchr(argv[n] + 5, ':');
            if (value_pos)
            {
                struct CalParameter* param = calloc(1, sizeof(struct CalParameter));
                strncpy(param->name, argv[n] + 5, value_pos - (argv[n] + 5));
                strncpy(param->value, value_pos + 1, sizeof(param->value) - 1);

                if (NULL == settings->parameter_list)
                {
                    settings->parameter_list = param;
                }
                else
                {
                    struct CalParameter* last = settings->parameter_list;
                    while (last->next_parameter)
                    {
                        last = last->next_parameter;
                    }
                    last->next_parameter = param;
                }
            }
        }
    }

    return nErrorCode;
}

void cleanupSettings(struct ProgramSettings* s)
{
    struct CalParameter* cur_param = s->parameter_list;
    while (cur_param)
    {
        struct CalParameter* ptr = cur_param;
        cur_param = cur_param->next_parameter;
        free(ptr);
    }
    s->parameter_list = NULL;
}
