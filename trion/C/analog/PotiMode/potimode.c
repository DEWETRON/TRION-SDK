/**
 * Short example to showcase a poti-meassuremente on a TRION-Module module
 *
 * This example should be used with a TRION-2402-MULTI-XXXX as board 0
 *
 * Describes following:
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH        24

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-MULTI",
                                    NULL};

// set the give channel on the given board to poti mode
// all const char parameters are optional, and may be NULL (in this case the board-defaults are taken)
int setupAIPoti (
                int nBoardNo,
                int nChannelNo,
                const char* Range,
                const char* Excitation
                );

// dumping the current bridge-settings to console
// just as visual feedback
void dumpAIPotiSettings (
                        int nBoardNo,
                        int nChannelNo
                        );


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nADCDelay = 0;
    int Scansize = 0;
    int nBoardId = 0;

    // Range Poti Mode (Full Range 0..100% -> -Exc .. +Exc)
    const double fRangeMin = 0;      // %
    const double fRangeMax = 100;    // %
    const float fExcitation = 5000;
    const char* sUnitRange = "%";
    const char* sUnitExc   = "mV"; //mA(current excitation) not allowed in Poti Mode
    ScaleInfo scaleinfo;
    RangeSpan rangespan;

    char sPropertyStr[256] = {0};  //The result-set will be an XML-Document, that may be rather large
    char sBoardStr[256] = {0};
    char sRangeStr[256] = {0};
    char sExcitationStr[256] = {0};
    char sStrVal[256] = {0};
    char sErrorText[256]={0};
    char sBoardId[256]={0};
    char sTarget[256] = { 0 };

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
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardId) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d", nBoardId);

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


    // Set configuration to use one board in standalone operation
    snprintf(sPropertyStr, sizeof(sPropertyStr),"%s/AcqProp", sBoardId );
    nErrorCode = DeWeSetParamStruct_str( sPropertyStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropertyStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sPropertyStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI0)
    snprintf(sBoardStr, sizeof(sBoardStr),"%s/AI0", sBoardId );
    nErrorCode = DeWeSetParamStruct_str( sBoardStr, "Used", "True");
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    CheckError(nErrorCode);
    // Also set up the channel properties
    // First the mode is selected. In this case it will be Bridge measurement mode
    //Set the desired range, so that the applied scaling is correct
    snprintf(sRangeStr, sizeof(sRangeStr),"%f..%f %s", fRangeMin, fRangeMax, sUnitRange);
    snprintf(sExcitationStr, sizeof(sExcitationStr),"%f %s", fExcitation, sUnitExc);

    // setup properties ..
    nErrorCode = setupAIPoti(nBoardId, 0, sRangeStr, sExcitationStr);
    if ( 0 != nErrorCode )
    {
        return UnloadTrionApi("Error setting AI to Poti-Mode\n.Aborting.....\n");
    }

    // Calculate Scaling Values with the really set values
    snprintf(sTarget, sizeof(sTarget), "BoardId%d/AI0", nBoardId);
    nErrorCode = GetAdjustedRange(sTarget, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)DATAWIDTH);
    CheckError(nErrorCode);


    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    dumpAIPotiSettings(nBoardId, 0);

    nErrorCode = DeWeSetParamStruct_str( sPropertyStr , "Samplerate", "1000");
    CheckError(nErrorCode);
    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_SIZE, 100);
    CheckError(nErrorCode);
    // Set the circular buffer size to 10 blocks. So the circular buffer can store samples
    // for 2 seconds
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_COUNT, 200);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_ONE_SCAN_SIZE, &Scansize);
    CheckError(nErrorCode);

   // Display adjused range ...
    nErrorCode = DeWeGetParamStruct_str( sBoardStr, "Range", sStrVal, sizeof(sStrVal));
    CheckError(nErrorCode);
    if (0 != nErrorCode)
    {
        printf("Failed to get adjusted range. Error Code:%X", nErrorCode);
    }

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32(nBoardId, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);

    printf("...running\n\n\n");
    printf("                         0%%                                                  100%%\n");

    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;         // Last position in the circular buffer
        sint64 nBufSize=0;           // Total buffer size

        // Get detailed information about the circular buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
            int nAvailSamples=0;
            int i=0;
            sint32 nRawData=0;
            double fScaledVal=0.0f;

            Sleep(100);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);

            // Available samples has to be recalculated according to the ADC delay
            nAvailSamples = nAvailSamples - nADCDelay;

            // skip if number of samples is smaller than the current ADC delay
            if (nAvailSamples <= 0)
            {
                continue;
            }

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
            CheckError(nErrorCode);

            // recalculate nReadPos to handle ADC delay
            nReadPos = nReadPos + nADCDelay * Scansize;
            // Handle the circular buffer wrap around
            if (nReadPos > nBufEndPos)
            {
                nReadPos -= nBufSize;
            }

            // Read the current samples from the circular buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Get the sample value at the read pointer of the circular buffer
                // The sample value is 24Bit (little endian, encoded in 32bit).
                nRawData = formatRawData( *(sint32*)nReadPos, (int)DATAWIDTH, 8);
                fScaledVal = ((((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd);

                // Print every 10th sample
                if ( 0 == i%10 )
                {
                    // Factor 2 to display a smaller range
                    int mpos = (int)(fScaledVal/2);

                    printf("\r%8.8X =  %#10.6f%s  [%.*s%s%.*s]", nRawData, fScaledVal, sUnitRange , mpos, "--------------------------------------------------", "*", (50-mpos), "--------------------------------------------------");
                    fflush(stdout);
                }

                // Increment the read pointer
                nReadPos += Scansize;

                // Handle the circular buffer wrap around
                if (nReadPos > nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }
            }

            // Free the circular buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);
        }
    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}


int setupAIPoti(
                int nBoardNo,
                int nChannelNo,
                const char* Range,
                const char* Excitation
                )
{
    int nErrorCode=0;
    char sTargetString[256] ={0};

    if ( -1 == nChannelNo )
    {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AIAll", nBoardNo );
    }
    else
    {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AI%d", nBoardNo, nChannelNo );
    }

    //1st item, to be set is always the mode, as this will initialize all depending sub-properties (like range, etc..)
    nErrorCode = DeWeSetParamStruct_str( sTargetString, "Mode", "Potentiometer");
    if (CheckError(nErrorCode)) return 1;

    //now apply all of the passed parameters, unless the pointer is NULL
    //Range and excitation influence each other (Excitation might be [V] or [mA]
    if ( NULL != Range )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "Excitation", Excitation);
        if (CheckError(nErrorCode)) return 1;
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "Range", Range );
        if (CheckError(nErrorCode)) return 1;
    }

    //no aliasing
    nErrorCode = DeWeSetParamStruct_str( sTargetString, "LPFilter_Val", "auto");
    if (CheckError(nErrorCode)) return 1;

    return 0;
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

void dumpAIPotiSettings (
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
    dumpProperty(sTargetString, "HPFilter_Val");
    dumpProperty(sTargetString, "Range");
    dumpProperty(sTargetString, "Excitation");
    dumpProperty(sTargetString, "InputType");
    dumpProperty(sTargetString, "BridgeRes");
    dumpProperty(sTargetString, "ShuntType");
    dumpProperty(sTargetString, "ShuntResistance");
    dumpProperty(sTargetString, "InputOffset");
}


