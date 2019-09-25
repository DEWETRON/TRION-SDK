/**
 * Example how the board-counter can be use to derive
 * information about the number of lost samples, and 
 * the thus the related time-period due to buffer-overrun
 *
 * This example should be used with any board that
 * offers analogue input channels (Trion dACC or Trion-dSTG)
 * This Board shall be installed as Board 0
 * all subsequent boards are ignored by the example
 *
 * This example omits the wrap-around code of the sample-counter
 * (32Bit, so 2^(32) Samples can be processed error-free with
 * this example. Wrap around and promotion to 64 bit
 * can easily be handled within the data-readout-loop
 *
 * Describes:
 *  - How to detect buffer-overrun
 *  - how to clear the error-condition
 *  - how to determine the duration of lost data
 *
 * THIS EXAMPLE CAN NOT BE RUN IN SIMULATION MODE
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"
#include <time.h>


//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24

static const int nPollIntervall = 100;  //ms


int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nErrorCode = 0;
    int nADCDelay=0;
    int nNumAIChannels = 0;
    int nNumBrdCNTChannels = 0;
    int nBoardID = 0;
    int nSizeScan=0;
    int nNoOfActiveChannelsAI = 0;
    char sBoardCNT[256]={0};
    char sBoardID[256]={0};
    char sPropStr[256]={0};
    char sChannelStr[256]={0};
    char sGetResultString[256]={0};
    char sformatString[256] = {0};
    char sErrorText[256]  = {0};
    float fSampleRate = 0.0f;
    int nBlockSize=0;
    uint32 uLastSampleCount = 0;
    int nBdCNTOffset = 0;
    int nSampleCount = 0;   

    // initialize random generator
    srand ( (int)(time(NULL)) );

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
        return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using the DEWE2 Explorer.\n");
    }

    // Build BoardId -> Either comming from command line (arg 1) or default "0"
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardID) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardID, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // test if Boardx offers:
    // more than 0 AI channels
    // more than 0 BoardCNT channels
    {
        char sBuffer[256] = {0};

        // Open & Reset the boards
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_OPEN_BOARD, 0 );
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_RESET_BOARD, 0 );
        CheckError(nErrorCode);

        // Build a string in the format: "BoardID0", "BoardID1", ...
        snprintf(sBoardID, sizeof(sBoardID),"BoardID%d", nBoardID);

        // Request the number of AI Channels
        snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AI", nBoardID);   
        nErrorCode = DeWeGetParamStruct_str(sChannelStr, "Channels", sBuffer, sizeof(sBuffer));
        CheckError(nErrorCode);
        if ( nErrorCode > 0 )
        {
            return UnloadTrionApi("Error obtaining number of AI channels.\nAborting...\n");
        }

        sscanf(sBuffer, "%d", &nNumAIChannels); 
        if ( nNumAIChannels == 0 )
        {
            return UnloadTrionApi("This Trion-Board does not offer analogue input channels.\nAborting...");
        }

        // Request the number of Board-Counter Channels
        // One of them will be used for data-lost information
        snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/BoardCNT", nBoardID);   
        nErrorCode = DeWeGetParamStruct_str(sChannelStr, "Channels", sBuffer, sizeof(sBuffer));
        CheckError(nErrorCode);
        if ( nErrorCode > 0 )
        {
            return UnloadTrionApi("Error obtaining number of Board-Counter channels.\nAborting...\n");
        }

        sscanf(sBuffer, "%d", &nNumBrdCNTChannels); 
        if ( nNumBrdCNTChannels == 0 )
        {
            return UnloadTrionApi("This Trion-Board does not offer an board-counter channel.\nAborting...");
        }
    }

    // prepare for output the format-string to show:
    // the raw, unscaled ADC - Values for
    // first AI: XXXXXXXXX last AI: xxxxxxxxxx currentSampleCount:
    // On a theoretical Board with one channel, both will be the same obvious
    printf( "Number of AI-channels:     %d\n", nNumAIChannels);
    printf( "Number of BrdCNT-channels: %d\n", nNumBrdCNTChannels);
    sprintf(sformatString, "AI0: %s AI%d: %s SampleCount: %s", "%12d", (nNumAIChannels-1), "%12d", " %10d");
   
    // Set configuration to use one board in standalone operation
    snprintf(sPropStr, sizeof(sPropStr), "BoardID%d/AcqProp",nBoardID);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // After reset all channels are disabled.
    // Enable all AI channels on this board, by using the ALL-Command
    snprintf(sBoardID, sizeof(sBoardID), "BoardID%d/AIALL",nBoardID);
    nErrorCode = DeWeSetParamStruct_str( sBoardID , "Used", "True");
    CheckError(nErrorCode);
    if ( nErrorCode <= 0 )
    {
        nNoOfActiveChannelsAI = nNumAIChannels;
    }
    else
    {
        return UnloadTrionApi("Error activating AI channels.\nAborting...");
    }

    // Enable the Board-Counter - Channel
    // set the input-source to ACQ_CLK
    // this way, this channel will provide 'timestamp-like'
    // information within the data stream (granularity = samples)
    // set to aut-reset on start-acquisition
    snprintf(sBoardCNT, sizeof(sBoardCNT), "BoardID%d/BoardCnt0",nBoardID);
    nErrorCode = DeWeSetParamStruct_str( sBoardCNT , "Used", "True");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sBoardCNT , "Source_A", "ACQ_CLK");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sBoardCNT , "Reset", "OnRestart");
    CheckError(nErrorCode);

    // pre-determine the offset of Board-Counter-Data within one Scan (it's position within data-stream)
    // on a real world application one would use different mechanisms to do this (like the scan-descriptor)
    // but for the sake of example-code-readability use the internal knowledge, that the boardcounter-data
    // is locate directly behind the data of all analogue channels, and that each analogue channel consumes
    // 32 Bit
    nBdCNTOffset = 4 * nNoOfActiveChannelsAI;

    // get the Sample-rate
    // Calculate a feasible Blocksize for this Sample-rate
    // BlockSize[Scans] := SampleRate * nPollIntervall[ms] / 1000
    snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AcqProp", nBoardID );   
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "SampleRate", "10000" );
    CheckError(nErrorCode);
    nErrorCode = DeWeGetParamStruct_str( sChannelStr, "SampleRate", sGetResultString, sizeof(sGetResultString));
    CheckError(nErrorCode);
    sscanf(sGetResultString, "%f", &fSampleRate);
    nBlockSize = (int)((fSampleRate * nPollIntervall) / 1000);


    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, nBlockSize);
    CheckError(nErrorCode);
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);

    // Determine the size of a sample scan
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan);
    CheckError(nErrorCode);

    printf( "Samplerate:                : %u\n", (uint32)(fSampleRate) );
    printf( "Offset of BoardCounter Data: %d\n", nBdCNTOffset );
    printf( "Scansize                   : %d\n", nSizeScan );
    printf( "ADC-Delay                  : %d\n", nADCDelay );

    //give the user time, to see the information
    printf("Press Key to start Acquisition\n");
    while( !kbhit() );
    getch();           //flush KB

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;        // Last position in the ring buffer
        int nBufSize=0;             // Total buffer size
        int nLoopCounter = 0;       // only used to force a data-lost with a really long sleep
        BOOLEAN bDataLost = FALSE;
        BOOLEAN bFirstReadAfterDataLost = FALSE;

        // Get detailed information about the ring buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        while( !kbhit() )
        {
            sint64 nReadPos=0; 
            int nAvailSamples=0;
            int i=0;
            sint32 nRawData0=0;
            sint32 nRawDataLast=0;
            uint32 uSampleCount=0;

            // wait for 100ms
            // here with data-lost forcing long sleep on every 10th run
            if ( ((++nLoopCounter) % 10) == 0 )
            {
                int dice = rand() % 5000;  //add anything up to 5000ms to the datalost
                int waittime = 8000 + dice;
                printf("now enforcing data-lost for %d ms\n", waittime);
                Sleep(waittime);
            }
            else
            {
                Sleep(nPollIntervall);
            }

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);

            // detect data lost situation
            if ( nErrorCode == ERR_BUFFER_OVERWRITE )
            {
                bDataLost = TRUE;
            }
            else
            {
                // Available samples has to be recalculated according to the ADC delay
                nAvailSamples = nAvailSamples - nADCDelay;

                // skip if number of samples is smaller than the current ADC delay
                if (nAvailSamples <= 0)
                {
                    continue;
                }

                // Get the current read pointer
                nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
                CheckError(nErrorCode);

                //between the first question and this question, also an overrun may occure
                // so we have to check here again
                if ( nErrorCode == ERR_BUFFER_OVERWRITE )
                {
                    bDataLost = TRUE;
                }
            }

            // acknowledge the data lost in the driver, so that
            // it resumes gathering data
            if ( bDataLost )
            {
                printf( "Acknowledge Data-lost condition\n");
                nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_CLEAR_ERROR, 0 );
                CheckError(nErrorCode);
    
                bDataLost = FALSE;
                bFirstReadAfterDataLost = TRUE;
                //omit any read-operation for now (as new data is not gathered yet)
                continue;
            }

            // recalculate nReadPos to handle ADC delay
            nReadPos = nReadPos + nADCDelay * nSizeScan;

            // Read the current samples from the ring buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                ++nSampleCount;
                // Get the sample value at the read pointer of the ring buffer
                // Here the complete scan is 4 * 32 bit : 3 analog values and 1 CNT Value
                nRawData0 = formatRawData( *(sint32*)nReadPos, (int)DATAWIDTH, 8);
                if ( nNoOfActiveChannelsAI > 1 )
                    nRawDataLast = formatRawData(*(sint32*)(nReadPos + (nBdCNTOffset - 4 )), (int)DATAWIDTH, 8);
                uSampleCount = *(uint32*)(nReadPos + nBdCNTOffset );

                if ( (0 == i) && (bFirstReadAfterDataLost))
                {
                    //determine, how many samples had been lost
                    int     nLostSamples = uSampleCount - uLastSampleCount;
                    float   fduration = nLostSamples / fSampleRate;
                                                                        
                    printf( "Lost %d samples (duration = %f sec)\n", nLostSamples, fduration);
                }

                // Print the sample value: (only every 100th cycle, so we can see the data-lost message better)
                // make sure, toprint also on last processed sample, so the results can be evaluated
                if ( bFirstReadAfterDataLost || (0 == (nSampleCount % 50)) || ( nAvailSamples-1 == i) )
                {
                    printf(sformatString, nRawData0, nRawDataLast, uSampleCount);
                    printf("\n");
                    fflush(stdout);
                }
                bFirstReadAfterDataLost = FALSE;

                // Increment the read pointer
                nReadPos += nSizeScan;

                // Handle the ring buffer wrap around
                if (nReadPos >= nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }
            }
            //remember the last position that has been processed
            uLastSampleCount = uSampleCount;

            // Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);
        }
    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}
