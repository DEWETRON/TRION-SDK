/**
 * Short example to describe how raw ADC data acquired
 * on analog channels can be scaled to measurement values
 * This example will use the first TRION-board in system
 * that offers analog channels
 *
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Query for the ADC delay
 *  - acquire data
 *  - printout the scaled values
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "trion_sdk_util.h"

//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24


// dumping the current bridge-settings to console
// just as visual feedback
void dumpAICurrentSettings (
                            int nBoardNo,
                            int nChannelNo
                            );


//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG",
                                    "TRION-2402-MULTI",
                                    NULL };


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nADCDelay = 0;
    int i = 0;
    int nBoardID = 0;
    const double fRangeMin = -100;   //[mA]
    const double fRangeMax = 100;    //[mA]
    char sErrorText[256] = {0};
    char sSettingStr[256]= {0};
    char sRangeStr[256]  = {0};
    char sBoardID[256]   = {0};
    ScaleInfo scaleinfo;
    RangeSpan rangespan;


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

    // Set configuration to use one board in standalone operation
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "SampleRate", "20000");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI3)
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AI3", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Used", "True");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Mode", "ExcCurrentMonitor");
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi(NULL);
    }


    //Set the desired range, so that the applied scaling is correct
    snprintf(sRangeStr, sizeof(sRangeStr),"%f..%f mA", fRangeMin, fRangeMax);
    printf( "Setting %s Range to %s.\n", sSettingStr, sRangeStr);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Range", sRangeStr);
    CheckError(nErrorCode);

    dumpAICurrentSettings(nBoardID, 3); // Channel AI3
    // Calculate Scaling Values with the really set values
    nErrorCode = GetAdjustedRange(sSettingStr, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)DATAWIDTH);
    CheckError(nErrorCode);


    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_AI, 3);
    CheckError(nErrorCode);


    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default samplerate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 2000);
    CheckError(nErrorCode);
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 1000);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ACQ_ALL, 0);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BOARD_ADC_DELAY, &nADCDelay);

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        const  char* Excitations[] = { "0.05 mA", "0.1 mA", "0.2 mA", "0.5 mA", "1 mA", "1.5 mA", "2 mA", "4 mA", "5 mA", "10 mA" };
        sint64 nBufEndPos=0;         // Last position in the ring buffer
        sint64 nBufSize=0;           // Total buffer size
        int runcount = 0;
        int ExcCount = 4;
        double expectedval = 0.0f;

        // Get detailed information about the ring buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
            int nAvailSamples=0;
            int i=0;
            char *ptr = NULL;
            sint32 nRawData=0;
            double fScaledVal=0.0;

            Sleep(20);

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            if ( nErrorCode > 0 )
            {
                int i = 42;
                CheckError(nErrorCode);
            }


            // Available samples has to be recalculated according to the ADC delay
            nAvailSamples = nAvailSamples - nADCDelay;

            // skip if number of samples is smaller than the current ADC delay
            if (nAvailSamples <= 0)
            {
                continue;
            }

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
            if ( nErrorCode > 0 )
            {
                int i = 42;
                CheckError(nErrorCode);
            }

            // recalculate nReadPos to handle ADC delay
            nReadPos = nReadPos + nADCDelay * sizeof(uint32);

            // Read the current samples from the ring buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Get the sample value at the read pointer of the ring buffer
                // The sample value is 24Bit (little endian, encoded in 32bit).
                nRawData = formatRawData( *(sint32*)nReadPos, (int)DATAWIDTH, 8 );
                fScaledVal = (((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd;

                // Print the sample value:
                if ( 0 == i%100 )
                {
                    printf("%8.8X =  %#10.6f mA   Exp/mease = %#10.6f\r", nRawData, fScaledVal, (expectedval != 0.0f) ? (double)(fScaledVal/expectedval) : 0.0f);
                }

                // Increment the read pointer
                nReadPos += sizeof(uint32);

                // Handle the ring buffer wrap around
                if (nReadPos > nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }

            }
            fflush(stdout);

            ++runcount;

            if ( 0 == runcount%10 )
            {
                snprintf(sSettingStr, sizeof(sSettingStr),"BoardID%d/AI3", nBoardID);
                nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Excitation", Excitations[ExcCount]);
                CheckError(nErrorCode);

                printf("\n\nSetting Excitation %s\n", Excitations[ExcCount]);
                expectedval = strtod(Excitations[ExcCount], &ptr);

                if ( nErrorCode <= 0)
                {
                    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_AI, 3 );
                    CheckError(nErrorCode);
                }

                ++ExcCount;
                if ( ExcCount > 9)
                {
                    ExcCount = 0;
                }
            }

            // Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            if ( nErrorCode > 0 )
            {
                int i = 42;
                CheckError(nErrorCode);
            }
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


static void dumpProperty(
    const char* sTarget,
    const char* sProperty
    )
{
    int     nErrorCode = 0;
    char    sStrVal[1024] = {0};

    nErrorCode = DeWeGetParamStruct_str( sTarget, sProperty, sStrVal, sizeof(sStrVal));
    if ( !CheckError(nErrorCode))
    {
        //only when retrieved error-free. otherwise the error is printed
        printf( "%s - %s: %s\n", sTarget, sProperty, sStrVal );
    }
}

void dumpAICurrentSettings (
    int nBoardNo,
    int nChannelNo
    )
{
    char sTargetString[256] ={0};

    if ( -1 == nChannelNo )
    {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AIAll", nBoardNo );
    }
    else
    {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AI%d", nBoardNo, nChannelNo );
    }

    dumpProperty(sTargetString, "Mode");
    dumpProperty(sTargetString, "LPFilter_Val");
    dumpProperty(sTargetString, "Range");
    dumpProperty(sTargetString, "Excitation");
    dumpProperty(sTargetString, "InputType");
    dumpProperty(sTargetString, "ShuntType");
    dumpProperty(sTargetString, "ShuntResistance");
    dumpProperty(sTargetString, "InputOffset");
}
