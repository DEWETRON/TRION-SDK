/**
 * Short example to describe how raw ADC data acquired
 * on analog channels can be scaled to measurement values
 * This example will use the first TRION-dSTG-board in system
 * that offers analog channels and will configure the 1st
 * channel to bridge-mode
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

// set the give channel on the given board to bridge mode
// all const char parameters are optional, and may be NULL (in this case the board-defaults are taken)
int setupAIBridge(  int nBoardNo,
                    int nChannelNo,
                    const char* Range,
                    const char* Excitation,
                    const char* InputType,
                    const char* BridgeResistance
                    );

// dumping the current bridge-settings to console
// just as visual feedback
void dumpAIBridgeSettings ( int nBoardNo,
                            int nChannelNo
                            );


//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG",
                                    NULL };


int main(int argc, char* argv[])
{
    int nErrorCode = 0;
    int nADCDelay = 0;
    int i = 0;
    int nNoOfBoards = 0;
    int nBoardID = 0;
    //Range 1000 mV/V, symmetrical
    const double fRangeMin = -1000;  //[mV/V]
    const double fRangeMax = 1000;   //[mV/V]
    const char* sUnitRange = "mV/V";
    const float fExcitation = 10;   //[V]
    const char* sUnitExcitation = "V";
    char sErrorText[256]    = {0};
    char sSettingStr[256]   = {0};
    char sRangeStr[256]     = {0};
    char sExcitationStr[256]= {0};
    char sBoardID[256] = {0};
    ScaleInfo scaleinfo;

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

    // Calculate Scaling Values
    nErrorCode = CalcScaling(&scaleinfo, fRangeMin, fRangeMax, (int)DATAWIDTH);
    CheckError(nErrorCode);

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
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI)
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AI0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr , "Used", "True");
    CheckError(nErrorCode);

    //Set the desired range, so that the applied scaling is correct
    snprintf(sRangeStr, sizeof(sRangeStr),"%f..%f %s", fRangeMin, fRangeMax, sUnitRange);
    snprintf(sExcitationStr, sizeof(sExcitationStr),"%f %s", fExcitation, sUnitExcitation);

    nErrorCode = setupAIBridge( nBoardID, 0, sRangeStr, sExcitationStr, "brquarter3w", "120" );
    if ( 0 != nErrorCode )
    {
        return UnloadTrionApi("Error setting AI to Bridge-Mode\n.Aborting.....\n");
    }

    dumpAIBridgeSettings(nBoardID, 0);
    Sleep(5000);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default samplerate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the circular buffer size to 50 blocks. So circular buffer can store samples
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

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        int nBufEndPos=0;         // Last position in the circular buffer
        int nBufSize=0;           // Total buffer size

        // Get detailed information about the circular buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
            int nAvailSamples=0;
            int i=0;
            sint32 nRawData =0;
            double fScaledVal=0;

            Sleep(100);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);

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

            // recalculate nReadPos to handle ADC delay
            nReadPos = nReadPos + nADCDelay * sizeof(uint32);

            // Read the current samples from the circular buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Get the sample value at the read pointer of the circular buffer
                // The sample value is 24Bit (little endian, encoded in 32bit).
                nRawData = formatRawData( *(sint32*)nReadPos, (int)DATAWIDTH, 8);
                fScaledVal = ((((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd);

                // Print the sample value:
                printf("%8.8X =  %#10.6f %s\n", nRawData, fScaledVal, sUnitRange );

                // Increment the read pointer
                nReadPos += sizeof(uint32);

                // Handle the circular buffer wrap around
                if (nReadPos > nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }

            }
            fflush(stdout);

            // Free the circular buffer after read of all values
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


int setupAIBridge(
                    int nBoardNo,
                    int nChannelNo,
                    const char* Range,
                    const char* Excitation,
                    const char* InputType,
                    const char* BridgeResistance )
{
    int  nErrorCode=0;
    char sTargetString[256] ={0};

    snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AI%d", nBoardNo, nChannelNo );

    //1st item, to be set is always the mode, as this will initialize all depending sub-properties (like range, etc..)
    nErrorCode = DeWeSetParamStruct_str( sTargetString, "Mode", "Bridge");
    if (CheckError(nErrorCode))
        return 1;

    //now apply all of the passed parameters, unless the pointer is NULL
    //Range and excitation influence each other (Excitation might be [V] or [mA]
    if ( NULL != Range )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "Range", Range );
        if (CheckError(nErrorCode))
            return 1;
    }

    if ( NULL != Excitation )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "Excitation", Excitation);
        if (CheckError(nErrorCode))
            return 1;
    }

    if ( NULL != InputType )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "InputType", InputType);
        if (CheckError(nErrorCode))
            return 1;
    }

    if ( NULL != BridgeResistance )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "BridgeRes", BridgeResistance);
        if (CheckError(nErrorCode))
            return 1;
    }

    return 0;
}


static void dumpProperty(
                        const char* sTarget,
                        const char* sProperty )
{
    int     nErrorCode = 0;
    char    sStrVal[256] = {0};

    nErrorCode = DeWeGetParamStruct_str( sTarget, sProperty, sStrVal, sizeof(sStrVal));
    if ( !CheckError(nErrorCode))
    {
        //only when retrieved error-free. otherwise the error is printed
        printf( "%s - %s: %s\n", sTarget, sProperty, sStrVal );
    }
}


void dumpAIBridgeSettings (
                            int nBoardNo,
                            int nChannelNo )
{
    char sTargetString[256] ={0};

    snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AI%d", nBoardNo, nChannelNo );

    dumpProperty(sTargetString, "Mode");
    dumpProperty(sTargetString, "LPFilter_Val");
    dumpProperty(sTargetString, "HPFilter_Val");
    dumpProperty(sTargetString, "Range");
    dumpProperty(sTargetString, "Excitation");
    dumpProperty(sTargetString, "InputType");
    dumpProperty(sTargetString, "BridgeRes");
    dumpProperty(sTargetString, "ShuntType");
    dumpProperty(sTargetString, "ShuntResistance");
}
