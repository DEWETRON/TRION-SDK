/**
* Short example to describe how to rad out CAN data as decoded frames
*
* This example should be used with a TRION-CAN board installed
* or configured in the simulated system
*
* Describes following:
*  - Setup of 1 CAN channel
*  - Print raw CAN frames + Timestamp
*/

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "trion_sdk_util.h"

#define GPSBUFFER   4096
#define GPSSENTENCE 4096 //only used for this example; actual sentence lengths vary by receiver type and are usually much lower

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {
    "TRION-TIMING",
    "TRION-VGPS",
    NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nBoardID = 0;
    char sBoardID[256] = { 0 };
    char sChannelStr[256] = { 0 };
    char sErrorText[256] = { 0 };
    BOARD_UART_FRAME aUartFrame[GPSBUFFER];
    BOOL bInGpsSentence = 0;
    char sGpsSentence[GPSSENTENCE];
    char* sGpsSentencePosition = sGpsSentence;
    char* sGpsSentenceEnd = sGpsSentence + GPSSENTENCE - 1;
    uint64 nGpsSentenceSyncCounter = 0;
    uint64 nGpsSentenceLastPPS = 0;
    int loop_cnt = 0;

    // Load pxi_api.dll
    if (0 != LoadTrionApi())
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
    if (TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardID))
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardID, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardID, sizeof(sBoardID), "BoardID%d", nBoardID);

    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_OPEN_BOARD, 0);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_RESET_BOARD, 0);
    CheckError(nErrorCode);

    /// Check if selected board is suitable for test
    if (FALSE == TestBoardType(nBoardID, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    // Set configuration to use one board in standalone operation
    snprintf(sChannelStr, sizeof(sChannelStr), "%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // configure the BoardCoutner-channel
    // for HW - timestamping to work it is necessary to have
    // at least one synchronous channel active. All TRION
    // boardtypes support a channel called Board-Counter (BoardCNT)
    // this is a basic counter channel, that usually has no
    // possibility to feed an external signal, and is usually
    // used to route internal signals to its input
    snprintf(sChannelStr, sizeof(sChannelStr), "%s/BoardCNT0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Used", "True");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default sample-rate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the circular buffer size to 50 blocks. So the circular buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // configure the GPS
    snprintf(sChannelStr, sizeof(sChannelStr), "%s/AcqProp/Timing", sBoardID);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "OptionGPSUart", "115200");
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring BaudRate\nAborting.....\n");
    }


#if 0
    // Deprecated

    // Configure the ASYNC-Polling Time to 100ms
    // Configure the Frame-Size (GPS == 2)
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_ASYNC_POLLING_TIME, 200);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal polling time\nAborting.....\n");
    }


    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_ASYNC_FRAME_SIZE, 2);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal frame size\nAborting.....\n");
    }
#endif

    // Update the timing settings (which is not done when using UPDATE_PARAM_ALL below)
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_UPDATE_PARAM_ACQ_TIMING, 0);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error on updating timing configuration\nAborting.....\n");
    }

    // configure the UART
    snprintf(sChannelStr, sizeof(sChannelStr), "BoardID%d/UART0", nBoardID);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Used", "True");
    if (CheckError(nErrorCode))
    {
        printf("Could not enable UART\n");
        return 1;
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "GGA_MSG", "Enabled");
    if (CheckError(nErrorCode))
    {
        printf("Could not enable GGA\n");
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "GGA_UpdateRate", "1Hz");
    if (CheckError(nErrorCode))
    {
        printf("Could not set GGA_UpdateRate\n");
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "GSA_MSG", "Enabled");
    if (CheckError(nErrorCode))
    {
        printf("Could not enable GSA\n");
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "GSA_UpdateRate", "1Hz");
    if (CheckError(nErrorCode))
    {
        printf("Could not set GSA_UpdateRate\n");
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "RMC_MSG", "Enabled");
    if (CheckError(nErrorCode))
    {
        printf("Could not enable RMC\n");
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "RMC_UpdateRate", "1Hz");
    if (CheckError(nErrorCode))
    {
        printf("Could not set RMC_UpdateRate\n");
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "GSV_MSG", "Enabled");
    if (CheckError(nErrorCode))
    {
        printf("Could not enable GSV\n");
    }

    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "GSV_UpdateRate", "1Hz");
    if (CheckError(nErrorCode))
    {
        printf("Could not set GSV_UpdateRate\n");
    }

    // Update the hardware with channel settings
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error on updating channel configuration\nAborting.....\n");
    }

    // Open the UART - Interface to this Board
    nErrorCode = DeWeOpenDmaUart(nBoardID);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at opening UART-Interface\nAborting.....\n");
    }

    // Start GPS capture, before start sync-acquisition
    // the sync - acquisition will synchronize the async data
    nErrorCode = DeWeStartDmaUart(nBoardID, 0);
    if (CheckError(nErrorCode))
    {
        printf("Error at starting GPS-UART\nAborting.....\n");
        return 1;
    }

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        // the synchronous data won't be evaluated at all
        // the samples will immediately being freed - just
        // to prevent an overrun - error (cosmetic)

        while (!kbhit())
        {
            int nAvailSamples = 0;
            int nAvailGpsChars = 0;
            int i = 0;

            loop_cnt++;

            // any longer or shorter timespan is also feasible
            Sleep(500);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32(nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples);
            CheckError(nErrorCode);

            // Free the circular buffer
            nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples);
            CheckError(nErrorCode);

            /*if (loop_cnt == 10)
            {
                printf("change GGA&RMC update rate!\n");

                nErrorCode = DeWeSetParamStruct_str(sChannelStr, "GGA_UpdateRate", "10Hz");
                if (CheckError(nErrorCode))
                {
                    printf("Could not set GGA update rate\n");
                }

                nErrorCode = DeWeSetParamStruct_str(sChannelStr, "RMC_UpdateRate", "10Hz");
                if (CheckError(nErrorCode))
                {
                    printf("Could not set RMC update rate\n");
                }

                nErrorCode = DeWeSetParam_i32(nBoardID, CMD_UPDATE_PARAM_UART, 0);
            }*/

            // now obtain all GPS - frames that have been collected in this timespan
            do {
                nAvailGpsChars = 0;
                nErrorCode = DeWeReadDmaUart(nBoardID, aUartFrame, GPSBUFFER, &nAvailGpsChars);
                if (CheckError(nErrorCode))
                {
                    printf("Error at obtaining UART-Frames\n");
                    nErrorCode = 0;
                    break;
                }

                for (i = 0; i < nAvailGpsChars; ++i)
                {
                    if (!bInGpsSentence)
                    {
                        if (aUartFrame[i].Data == '$')
                        {
                            bInGpsSentence = 1;
                            memset(sGpsSentence, 0, GPSSENTENCE);
                            *(sGpsSentencePosition++) = aUartFrame[i].Data;
                            nGpsSentenceSyncCounter = aUartFrame[i].SyncCounter;
                            nGpsSentenceLastPPS = aUartFrame[i].LastPPS;
                        }
                    }
                    else
                    {
                        if (aUartFrame[i].Data == '\r' || aUartFrame[i].Data == '\n')
                        {
                            // Show SyncCounter/LastPPS value of SyncCounter
                            //printf("%llu/%llu\n", nGpsSentenceSyncCounter, nGpsSentenceLastPPS);
                            printf("GPS @ %12fs: '%s' \n", nGpsSentenceSyncCounter / 1000000.0, sGpsSentence);
                            bInGpsSentence = 0;
                            sGpsSentencePosition = sGpsSentence;
                        }
                        else
                        {
                            if (sGpsSentencePosition<sGpsSentenceEnd)
                            {
                                *(sGpsSentencePosition++) = aUartFrame[i].Data;
                            }
                        }
                    }
                }
            } while (nAvailGpsChars >(GPSBUFFER / 2));

            if (nErrorCode > 0)
            {
                break;
            }
        }
    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);
    // Stop GPS DMA
    nErrorCode = DeWeStopDmaUart(nBoardID, 0);
    CheckError(nErrorCode);
    nErrorCode = DeWeCloseDmaUart(nBoardID);
    CheckError(nErrorCode);
    // Close the board connection
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
