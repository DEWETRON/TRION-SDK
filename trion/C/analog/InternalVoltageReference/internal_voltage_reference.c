/**
 * Short example to describe how to setup the internal voltage reference
 * and utilize one channel to measure it back
 *
 * This example should be used with a TRION-2402-dACC, TRION-2402-dSTG, 
 * TRION-16XX-LV or TRION-1620-ACC as board 0
 * 
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Setup of Aref (Voltage Reference channel)
 *  - Print of analog values.
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
const char* sBoardNameNeeded[] = {  "TRION-2402-dACC", 
                                    "TRION-2402-dSTG", 
                                    "TRION-1620-LV", 
                                    "TRION-1620-ACC",
                                    "TRION-1603-LV", 
                                    "TRION-2402-MULTI",
                                    NULL };

//same order as BoardName(needed), sBoardNameNeeded is PK
const double maxArefVal[] =  { 11.0f, 10.0f, 2.45f, 2.45f, 2.45f, 4.9f, 0.0f };


int main(int argc, char* argv[])
{
    int nBoardId = 0;
    char sBoardId[256]   = {0};
    char sTarget[256]    = {0};
    char sErrorText[256] = {0};
    char sSetting[256]   = {0};
    int nNoOfBoards = 0;
    int nErrorCode  = 0;
    int nADCDelay   = 0;
    sint64 nBufStartPos = 0;
    double CalSourceVal = 0.0f;
    double MaxCalSourceVal = 0.0f;
    double targetrange = 10.0f;
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
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardId) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }
    
    // Build BoardIDX string for _str functions
    snprintf(sBoardId, sizeof(sBoardId), "BoardID%d", nBoardId);
    
    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    if ( FALSE == TestBoardType( nBoardId, sBoardNameNeeded) )
    {
        return UnloadTrionApi(NULL);
    }

    MaxCalSourceVal = GetMaxARef(nBoardId, sBoardId, sBoardNameNeeded, maxArefVal );

    // Set configuration to use one board in standalone operation
    snprintf(sTarget, sizeof(sTarget),"%s/%s", sBoardId, "AcqProp");
    nErrorCode = DeWeSetParamStruct_str( sTarget, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtClk", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "SampleRate", "2000");
    CheckError(nErrorCode);

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI)
    snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "AI0");
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Used", "True");
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Could not enable AI channel\n");
    }

    //Mode to calibration, to have access to calibtration source
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Mode", "Calibration");
    CheckError(nErrorCode);

    // Set 10V range
    snprintf(sSetting, sizeof(sSetting), "%f V", targetrange);
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Range", sSetting);
    CheckError(nErrorCode);
    // Calculate Scaling Values with the really set values
    nErrorCode = GetAdjustedRange(sTarget, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)DATAWIDTH);
    CheckError(nErrorCode);


    //Input CAL_Source, to measure the voltage reference
    nErrorCode = DeWeSetParamStruct_str( sTarget , "InputType", "CalSource");
    CheckError(nErrorCode);

    //Set Initial Value of Internal-Reference to 0.1V
    CalSourceVal = 0.01f;

    //Setup Internal Reference to RefPosOutDis
    // = Reference positive, Output (AUX) disabled
    snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "ARef0");
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Mode", "RefPosOutDis");
    CheckError(nErrorCode);

    snprintf(sSetting, sizeof(sSetting), "%f V", CalSourceVal);
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Value", sSetting);
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default samplerate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_START_ACQUISITION, 0);
    if (CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error on starting of acquisition\n");
    }

    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos=0;         // Last position in the ring buffer
        int nBufSize  = 0;          // Total buffer size
        double fVal   = 0;
        int loopcount = 0;

        // Get detailed information about the ring buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_START_POINTER, &nBufStartPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);

        while( !kbhit() )
        {
            sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
            int nAvailSamples=0;
            int i=0;
            sint32 nRawData=0;

            Sleep(100);

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            if (ERR_BUFFER_OVERWRITE == nErrorCode)
            {
                printf("Measurement Buffer Overflow happened - stopping measurement\n");
                break;
            }

            // Available samples has to be recalculated according to the ADC delay
            nAvailSamples = nAvailSamples - nADCDelay;

            // skip if number of samples is smaller than the current ADC delay
            if (nAvailSamples <= 0)
            {
                continue;
            }

            ++loopcount;

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );

            // recalculate nReadPos to handle ADC delay
            nReadPos = nReadPos + nADCDelay * sizeof(uint32);

            // Read the current samples from the ring buffer
            for (i = 0; i < nAvailSamples; ++i)
            {
                // Handle the ring buffer wrap around
                if (nReadPos >= nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }

                // Get the sample value at the read pointer of the ring buffer
                // The sample value is 24Bit (little endian, encoded in 32bit). 
                nRawData = formatRawData( *(sint32*)nReadPos, (int)DATAWIDTH, 8 );    
                fVal = (( (double)((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd);
                
                // Print the sample value:
                printf("Raw %8.8x,   Scaled %12.12f", nRawData, fVal);
                printf("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b");
                fflush(stdout);

                // Increment the read pointer
                nReadPos += sizeof(uint32);

            }

            // Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);
      
            //every 8th loop increase the internal reference by 0.2V
            //depending of type of hardware, the result might differ
            if( loopcount > 7 )
            {
                loopcount = 0;
                if ( CalSourceVal < 3.0f )
                {
                    CalSourceVal = CalSourceVal + 0.2f;
                } else {
                    CalSourceVal = CalSourceVal + 1.0f;
                }

                if ( CalSourceVal > MaxCalSourceVal )
                {
                    CalSourceVal = 0.01f;
                }
                printf("\n\nSetting ARef to %f V ", CalSourceVal);
                snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "ARef0");
                snprintf(sSetting, sizeof(sSetting), "%f V", CalSourceVal);
                nErrorCode = DeWeSetParamStruct_str( sTarget , "Value", sSetting);
                CheckError(nErrorCode);
                nErrorCode = DeWeGetParamStruct_str( sTarget , "Value", sSetting, sizeof(sSetting));
                CheckError(nErrorCode);
                printf("(resulting in %s)\n", sSetting);

                nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_AREF, 0);
                CheckError(nErrorCode);
            }
        }
    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);

    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd of Example\n");

    return nErrorCode;
}

