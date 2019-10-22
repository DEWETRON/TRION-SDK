/**
 * Short example to demonstrate how to update channel properties during a measurement
 *
 * This example is suitable for all TRION boards featuring analog channels.
 * 
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Query for the ADC delay
 *  - Start of measurement 
 *  - Print every 10th analog value
 *  - Adjust a random range between [-3..7V] and [4..10V]
 *  - Print every 10th analog value
 *  - (Whole measurement is in a loop, where the range changes are done every 3 seconds ..)
 *
 *      Note: The Scale Values can be calculated in the example, or read from api.dll by
 *            the define USE_API_SCALING
 */

#include <time.h>
#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


#define RANGE_CHANGE_TIMEOUT    3000 //[ms]

//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24

//if USE_API_SCALING set
// the string-commands to obtain API-Sclaing will be used
// if not set, scaling is calculated on application level
#define USE_API_SCALING
#undef  USE_API_SCALING


//needed Board-Type for this example
const  char* sBoardNameNeeded[] = { "TRION-2402-dSTG", 
                                    "TRION-2402-dACC", 
                                    "TRION-2402-V",
                                    "TRION-2402-MULTI",
                                    "TRION-1620-ACC",
                                    NULL };


int main(int argc, char* argv[])
{
    int  nBoardId = 0;
    char sErrorText[256]= {0};
    char sBoardId[256]  = {0};
    char sTarget[256]   = {0};
    char sXMLTarget[256]= {0};
    char FromBd[256]    = {0};
    char range_str[256] = {0};
    char sRangeMax[8]   = {0};
    char sRangeMin[8]   = {0};
    sint64 nBufStartPos = 0;
    int nNoOfBoards = 0;
    int nErrorCode  = 0;
    int nADCDelay   = 0;
    double new_range_count = 0.0;
    int msec=0;
    clock_t start;
    const int blocksize=2000;
    ScaleInfo scaleinfo;
    RangeSpan rangespan;

    //Load PXI API DLL
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
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardId) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build BoardIDX string for _str functions
    snprintf(sBoardId, sizeof(sBoardId), "BoardID%u", nBoardId);
   
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

    // Set acquisition properties for board 
    snprintf(sTarget, sizeof(sTarget),"%s/%s", sBoardId, "AcqProp");
    nErrorCode = DeWeSetParamStruct_str( sTarget, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "ExtClk", "False");
    CheckError(nErrorCode);
    

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default samplerate 2000 samples per second, 200 is a buffer for 0.1 seconds
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples for 5 seconds
    nErrorCode = DeWeSetParamStruct_str( sTarget, "SampleRate", "20000");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_SIZE, blocksize);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);
  
    // After reset all channels are disabled.
    // So we have to enable at least one channel to start a measurement 
    snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "AI0");
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Used", "True");
    if (nErrorCode) 
    {
        snprintf(sErrorText, sizeof(sErrorText), "Could not enable AI channel on board %d: %s\n", nBoardId, DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

    // Set channel properties (initial values) 
    nErrorCode = DeWeSetParamStruct_str( sTarget,  "Mode",  "Voltage");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Range", "10 V");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget , "LPFilter_Val", "Auto");
    CheckError(nErrorCode);
   

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);

    // Get the adjusted Range 
    GetAdjustedRange( sTarget, &rangespan);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, DATAWIDTH);

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_START_ACQUISITION, 0);
    if (nErrorCode <= 0)
    {
        sint64 nBufEndPos;         // Last position in the ring buffer
        int nBufSize = 0;          // Total buffer size
        double fVal = 0.0;
        
        printf("\n-> Analog measurement on channel AI0 started\n");
        printf("-> Adjusting a new range every %d[ms]\n\n", RANGE_CHANGE_TIMEOUT);

        // Get detailed information about the ring buffer
        // to be able to handle the wrap around
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_START_POINTER, &nBufStartPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_END_POINTER, &nBufEndPos);
        CheckError(nErrorCode);
        nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
        CheckError(nErrorCode);
        
        start = clock();

        while( !kbhit() )
        {
            sint64 nReadPos = 0;       // Pointer to the ring buffer read pointer
            int nAvailSamples = 0;
            int i = 0;
            sint32 nRawData = 0;

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

            // Get the current read pointer
            nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
            CheckError(nErrorCode);

            // recalculate nReadPos to handle ADC delay
            nReadPos = nReadPos + nADCDelay * sizeof(uint32);

            // Read the current samples from the ring buffer
            for (i=0; i < nAvailSamples; ++i)
            {
                // Handle the ring buffer wrap around
                if (nReadPos >= nBufEndPos)
                {
                    nReadPos -= nBufSize;
                }

                // Print the sample value:
                if (0 == i%10)
                {
                    // Get the sample value at the read pointer of the ring buffer
                    // The sample value is 24Bit (little endian, encoded in 32bit). 
                    nRawData = formatRawData( *(sint32*)nReadPos, (int)DATAWIDTH, 8);
                    fVal = (((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd;
                    
                    printf("\rAdj.Range:%2.2G .. %2.2G[V]  RawData:%8.8X  ScaledData:%6.6f[V]", rangespan.rmin, rangespan.rmax, nRawData, fVal);
                    fflush(stdout);
                }

                // Increment the read pointer
                nReadPos += sizeof(uint32);
            }
            
            nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);

            // Write new range and LPFilter values to board -> every ~3000ms
            // LPFilter depending on range
            if (((clock() - start) * 1000 / CLOCKS_PER_SEC) > RANGE_CHANGE_TIMEOUT )
            {
                printf("\n\n");
                // Generate new range
                int new_range_max;
                int new_range_min;
                int ec_range = 0;
                
                do {
                    new_range_max = (int)(rand() % 7 + 3);
                    new_range_min = (int)(rand() % 7 - 3);
                } while (new_range_min > new_range_max);
                
                snprintf(range_str,sizeof(range_str), "%i..%i V", new_range_min, new_range_max );
                ec_range = DeWeSetParamStruct_str( sTarget , "Range", range_str);
                CheckError(ec_range);

#ifdef SETFILTER
                if ( new_range_max < 5 )
                {
                    nErrorCode = DeWeSetParamStruct_str( sTarget , "LPFilter_Val", "19000");
                }
                else
                {
                    nErrorCode = DeWeSetParamStruct_str( sTarget , "LPFilter_Val", "Auto");
                }
                CheckError(nErrorCode);
#endif

                nErrorCode = DeWeSetParam_i32(nBoardId, CMD_UPDATE_PARAM_AI, UPDATE_ALL_CHANNELS);
                CheckError(nErrorCode);

                if ( nErrorCode > 0 )
                {
                    snprintf(sErrorText, sizeof(sErrorText), "PARAM_UPDATE during measurement failed %d: %s\n", nBoardId, DeWeErrorConstantToString(nErrorCode));
                    return UnloadTrionApi(sErrorText);
                }

                // Get Adjusted Range 
                printf("tried to set : %s\n", range_str);
                nErrorCode = GetAdjustedRange(sTarget, &rangespan);
                if (ec_range != 0)
                {
                    snprintf(range_str, sizeof(range_str), "%2G..%2G V", rangespan.rmin, rangespan.rmax);
                    printf(" Adjusted to  : %s\n", range_str);
                }

#ifdef USE_API_SCALING
                // Read Scale values from API for adj. range 
                nErrorCode = SetScaling(&scaleinfo, sTarget);
#else
                nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, DATAWIDTH);
#endif
                nAvailSamples = blocksize;

                //throw away the first block, after changing AI-properties to have clean results again
                nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_WAIT_AVAIL_NO_SAMPLE, &nAvailSamples);
                CheckError(nErrorCode);
                nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
                CheckError(nErrorCode);

                start = clock();
            }

            // Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
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


