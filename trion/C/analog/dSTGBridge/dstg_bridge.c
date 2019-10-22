/**
 * Short example to showcase a bridge-meassuremente on a TRION-dSTG module
 *
 * This example should be used with a TRION-2402-dSTG-XXXX as board 0
 * 
 * Describes following:
 *  - Setup of all AI channel in bridge-mode
 *  - Setup the channel properties for bridge measurement
 *  - perform sensor-balancing
 *  - start acquisition, and print the measured values of channel 0
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24


//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG", 
                                    NULL };


// set the give channel on the given board to bridge mode
// all const char parameters are optional, and may be NULL (in this case the board-defaults are taken)
int setupAIBridge(  int nBoardNo, 
                    int nChannelNo, 
                    const char* Range, 
                    const char* Excitation, 
                    const char* InputType, 
                    const char* BridgeResistance 
                    );

int setupOffsetCompensation(
                            int nBoardNo,
                            int nChannelNo,
                            const char* raw_xml,
                            const char* unit_str
                            );

// dumping the current bridge-settings to console
// just as visual feedback
void dumpAIBridgeSettings (
                            int nBoardNo,
                            int nChannelNo
                            );



int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nBoardID=0;
    int nErrorCode = 0;
    int nADCDelay=0;
    int Scansize=0;
    //Range 1 mV/V, symmetrical
    const double fRangeMin = -3;  //[mV/V]
    const double fRangeMax = 3;   //[mV/V]
    const char* sUnitRange = "mV/V";
    const float fExcitation = 1;   //[V]
    const char* sUnitExcitation = "V";
    char sSettingStr[32*1024] = {0};  //The result-set will be an XML-Document, that may be rather large
    char sRangeStr[256] = {0};
    char sExcitationStr[256] = {0};
    char sErrorText[256] = {0};
    char sChannelStr[256] = {0};
    char sPropStr[256] = {0};
    char sBoardStr[256] = {0};
    char sBoardID[256] = {0};
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
       return UnloadTrionApi("No Trion cards found. Aborting...\nPlease configure a system using the DEWE2 Explorer.\n");
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
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AcqProp", sBoardID); 
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "ExtClk", "False");
    CheckError(nErrorCode);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI0)
    snprintf(sBoardStr, sizeof(sBoardStr),"%s/AI0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sBoardStr , "Used", "True");
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    if (CheckError(nErrorCode))
    {
        snprintf(sErrorText, sizeof(sErrorText), "CMD_UPDATE_PARAM_ALL FAILED: : %s", DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

    // perform balancing operations prior to bridge-measurement
    // the amplifier offset compensation should be less of an issue, as amplifier
    // drift is low. But to make the exmpel more complete, perform it on every
    // start of this example
    printf("Performing amplifier-offset-compensation...");
    nErrorCode = DeWeSetParamStruct_str( sBoardStr , "amplifieroffset", "500 msec" );
    CheckError(nErrorCode);
    if ( nErrorCode > 0 )
    {
        snprintf(sErrorText, sizeof(sErrorText), "failed [%s]. Aborting....\n", DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }
    printf("OK\n");

    // Also set up the channel properties
    // First the mode is selected. In this case it will be Bridge measurement mode
    //Set the desired range, so that the applied scaling is correct
    snprintf(sRangeStr, sizeof(sRangeStr),"%f..%f %s", fRangeMin, fRangeMax, sUnitRange);
    snprintf(sExcitationStr, sizeof(sExcitationStr),"%f %s", fExcitation, sUnitExcitation);

    //setup bridge prior sensor-balancing
    nErrorCode = setupAIBridge( nBoardID, -1, sRangeStr, sExcitationStr, "brquarter4w", "350" );
    if ( 0 != nErrorCode )
    {
        return UnloadTrionApi("Error setting AI to Bridge-Mode\n.Aborting.....\n");
    }

    // Calculate Scaling Values with the really set values
    nErrorCode = GetAdjustedRange(sBoardStr, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)DATAWIDTH);
    CheckError(nErrorCode);

    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("EUPDATE PARAM ALL FAIELD\n.Aborting.....\n");
    }

    dumpAIBridgeSettings(nBoardID, -1);

    snprintf(sPropStr, sizeof(sPropStr), "BoardID%d/Acqprop", nBoardID);
    nErrorCode = DeWeSetParamStruct_str( sPropStr , "Samplerate", "1000");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 100);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("CMD FAIL\n.Aborting.....\n");
    }
    // Set the ring buffer size to 10 blocks. So ring buffer can store samples
    // for 2 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 200);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("CMD FAIL\n.Aborting.....\n");
    }

    printf("Performing sensor-offset-compensation...");
    nErrorCode = DeWeSetParamStruct_str( sBoardStr , "SensorOffset", "100 msec" );
    if ( nErrorCode > 0 )
    {
        snprintf(sErrorText, sizeof(sErrorText), "failed [%s]. Aborting....\n", DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }
    printf("OK\n");

    nErrorCode = DeWeGetParamStruct_str( sBoardStr, "SensorOffset", sSettingStr, sizeof(sSettingStr));
    printf("%s\n", sSettingStr);

    setupOffsetCompensation(nBoardID, 0, sSettingStr, sUnitRange );

    nErrorCode = DeWeSetParamStruct_str( sPropStr , "Samplerate", "1000");
    CheckError(nErrorCode);
    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 100);
    if (CheckError(nErrorCode))
    {
       return UnloadTrionApi("CMD FAIL\n.Aborting.....\n");
    }
    // Set the ring buffer size to 10 blocks. So ring buffer can store samples
    // for 2 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 200);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("CMD FAIL\n.Aborting.....\n");
    }

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_ONE_SCAN_SIZE, &Scansize);
    CheckError(nErrorCode);

    // Data Acquisition - stopped with any key
    printf("Starting Acquisition...");
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("CMD FAIL\n.Aborting.....\n");
    }

    printf("running\n\n\n\n\n");
    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;         // Last position in the ring buffer
        sint64 nBufSize=0;           // Total buffer size

        // Get detailed information about the ring buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i64( nBoardID, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);
        printf("                           -100%%                           +100%%\n");

        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
            int nAvailSamples=0;
            int i=0;
            sint32 nRawData=0;
            double fScaledVal=0.0;

            // Sleep(100);

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_WAIT_AVAIL_NO_SAMPLE, &nAvailSamples );
            if (CheckError(nErrorCode))
            {
                return UnloadTrionApi("CMD FAIL\n.Aborting.....\n");
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
            CheckError(nErrorCode);

            // recalculate nReadPos to handle ADC delay
            nReadPos = nReadPos + nADCDelay * Scansize;
            // Handle the ring buffer wrap around
            if (nReadPos > nBufEndPos)
            {
                nReadPos -= nBufSize;
            }

            // Read the current samples from the ring buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Get the sample value at the read pointer of the ring buffer
                // The sample value is 24Bit (little endian, encoded in 32bit). 
                nRawData = formatRawData( *(sint32*)nReadPos, (int)DATAWIDTH, 8 );
                fScaledVal = (( (double)((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd);
                
                // Print the sample value:
                if ( 0 == i%10 ){
                    int mpos = (int)((fScaledVal/scaleinfo.fk) * 15) + 15;
                    printf("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
                    printf("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
                    printf("%8.8X =  %#10.6f %s  [%.*s%s%.*s]", nRawData, fScaledVal, sUnitRange , mpos, "----------------------------------------", "*", (30-mpos), "----------------------------------------");
                    fflush(stdout);
                }

                // Increment the read pointer
                nReadPos += Scansize;

                // Handle the ring buffer wrap around
                if (nReadPos > nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }
            }

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

int setupAIBridge(  
    int nBoardNo, 
    int nChannelNo, 
    const char* Range, 
    const char* Excitation, 
    const char* InputType, 
    const char* BridgeResistance )
{
    int nErrorCode=0;
    char sTargetString[256] ={0};

    if ( -1 == nChannelNo )
    {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AIAll", nBoardNo );
    } else {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AI%d", nBoardNo, nChannelNo );
    }


    //1st item, to be set is always the mode, as this will initialize all depending sub-properties (like range, etc..)
    nErrorCode = DeWeSetParamStruct_str( sTargetString, "Mode", "Bridge");
    if (CheckError(nErrorCode)) return 1;

    //now apply all of the passed parameters, unless the pointer is NULL
    //Range and excitation influence each other (Excitation might be [V] or [mA]
    if ( NULL != Range )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "Range", Range );
        if (CheckError(nErrorCode)) return 1;
    }

    if ( NULL != Excitation )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "Excitation", Excitation);
        if (CheckError(nErrorCode)) return 1;
    }

    if ( NULL != InputType )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "InputType", InputType);
        if (CheckError(nErrorCode)) return 1;
    }

    if ( NULL != BridgeResistance )
    {
        nErrorCode = DeWeSetParamStruct_str( sTargetString, "BridgeRes", BridgeResistance);
        if (CheckError(nErrorCode)) return 1;
    }

    //no aliasing
    nErrorCode = DeWeSetParamStruct_str( sTargetString, "LPFilter_Val", "auto");
    if (CheckError(nErrorCode)) return 1;


    //dump out the current configuration to the console to show the now-effective settings
    return 0;
}

int setupOffsetCompensation(
    int nBoardNo,
    int nChannelNo,
    const char* raw_xml,
    const char* unit_str
    )
{
    // "sloppy" code to process the xml-information regarding the sensor-offsets
    // parses the result xml-string to obtain the offset-value, and applies this information
    // to the "inputoffset" parameter of the channel.
    // in an c++ environment this would be done by "real" XML-processing
    // This simple code does not bother to find the "correct" channel, nor would it be able
    // to handle unexpected conditions in the XML-stream in a graceful way.

    int nErrorCode=0;
    char sTargetString[256] ={0};
    char* startpos = 0;
    char* endpos = 0;
    char offsetstr_raw[256] = {0};
    char offsetstr[256] = {0};

    if ( -1 == nChannelNo )
    {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AIAll", nBoardNo );
    } else {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AI%d", nBoardNo, nChannelNo );
    }

    startpos = strstr(raw_xml, "<Offset");
    startpos = strstr(startpos, ">") + 1;
    endpos = strchr(startpos, '<');
    memcpy( &offsetstr_raw[0], startpos, MIN((endpos-startpos), sizeof(offsetstr_raw)-1));
    printf(" Offset = %s\n", offsetstr_raw);
    snprintf( offsetstr, sizeof(offsetstr), "%s %s", offsetstr_raw, unit_str);

    snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AI0", nBoardNo);
    nErrorCode = DeWeSetParamStruct_str( sTargetString, "inputoffset", offsetstr);
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

void dumpAIBridgeSettings (
    int nBoardNo,
    int nChannelNo
    )
{
    char sTargetString[256] ={0};

    if ( -1 == nChannelNo )
    {
        snprintf( sTargetString, sizeof(sTargetString), "BoardId%d/AIAll", nBoardNo );
    } else {
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
