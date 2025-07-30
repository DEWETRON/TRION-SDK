/**
 * Short example to showcase using a MSI-adapter measurement on a TRION-MULTI Module
 *
 * The Board is selectable over cmd line parameter 1. Default Board Nr. = 0
 * The AIChannel is selectable over cmd line parameter 2. Default Channel Nr. = 0
 * This example should be used with a TRION-2402-MULTI
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
#define DATAWIDTH   24

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-MULTI",
                                    "TRION-1850-MULTI",
                                    "TRION3-1820-MULTI",
                                    "TRION3-1850-MULTI",
                                    NULL};


// Example takes 2 parameters:
// 1st BoardID
// 2nd Channel Number
int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nADCDelay = 0;
    int Scansize = 0;
    int nBoardId = 0;
    int nChannelNo=0;
    int input=0;
    char sPropertyStr[256] = {0};
    char sSensorStr[256] = {0};
    char sTarget[256] = {0};
    char sRangeStr[256] = {0};
    char sExcitationStr[256] = {0};
    char sStrVal[256] = {0};
    char sErrorText[256]={0};
    char sBoardId[256]={0};
    char sResultStr[256]={0};
    char TEDS_DATA[32 * 1024] = { 0 };       //large enough for even the 20kbit E2Prom
    char *punit=NULL;
    sint64 nBufStartPos=0;
    sint64 nBufEndPos=0;
    int nBufSize=0;
    double fVal=0;
    int nSizeScan=0;
    ScaleInfo scaleinfo;
    RangeSpan rangespan;

    int duration =0;
    // To be adopted to user's demands ..
    const double fRangeMin = 0;       // MAX MIN VALUE: -200�C
    const double fRangeMax = 1820;       // MAX MAX VALUE: 1370�C

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
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %u\nNumber of found boards: %u", nBoardId, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }

    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardId, sizeof(sBoardId),"BoardID%d", nBoardId);

   // Get channel number form command line; Default number is "0"
    if ( TRUE != ARG_GetChannelNo(argc, argv, nNoOfBoards, &nChannelNo) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid Channel Number: Allowed channel numbers are from [0..7]\n");
        return UnloadTrionApi(sErrorText);
    }

    // Open & Reset the board
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_OPEN_BOARD, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_RESET_BOARD, 0 );
    CheckError(nErrorCode);

    // Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardId, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    ////////////////////////////
    // Get TEDS Complete Data //
    ////////////////////////////
    snprintf(sTarget, sizeof(sTarget), "%s/AI%d", sBoardId, nChannelNo);
    nErrorCode = DeWeGetParamStruct_str(sTarget, "TedsReadEx", TEDS_DATA, sizeof(TEDS_DATA));
    if (CheckError(nErrorCode))
    {
        printf("ROM CODE DATA READ FAILED!!\n\n");
    }
    else
    {
        printf("ROM CODE: %s\n\n", TEDS_DATA);
    }


    // Set Acquisition Properties for Board
    snprintf(sPropertyStr, sizeof(sPropertyStr),"BoardID%d/AcqProp", nBoardId);
    nErrorCode = DeWeSetParamStruct_str( sPropertyStr , "Samplerate", "2000");
    CheckError(nErrorCode);
    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_SIZE, 200);
    CheckError(nErrorCode);
    // Set the circular buffer size to 10 blocks. So the circular buffer can store samples
    // for 2 seconds
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    // Set Properties for first channel AIx
    snprintf(sRangeStr, sizeof(sRangeStr), "%f..%f degC", fRangeMin, fRangeMax);

    snprintf(sTarget, sizeof(sTarget), "%s/AI%d", sBoardId, nChannelNo);
    //snprintf(sTarget, sizeof(sTarget), "%s/AIALL", sBoardId);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "Mode", "MSI-BR-LVDT");
    //nErrorCode = DeWeSetParamStruct_str(sTarget, "Mode", "MSI-BR-CH-5-V2");
    CheckError(nErrorCode);
    // Enable Linearization with desired LinTable
    //"PTX" Default PT100 Tanble;   "PT_TEST" -> Custom Table;  "NONE" -> Linearization OFF;
    //nErrorCode = DeWeSetParamStruct_str( sTarget, "LinearisationTable", "TypeB");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sTarget, "Used", "True");
    CheckError(nErrorCode);
    //nErrorCode = DeWeSetParamStruct_str( sTarget, "Range", sRangeStr);
    //CheckError(nErrorCode);

#if 0
    nErrorCode = DeWeSetParamStruct_str(sTarget, "SensorRes0", "200 Ohm");
    CheckError(nErrorCode);
#endif


    //Reduce noise on Temperature measurement
    //LPFilter_Val, 10000 Hz
    nErrorCode = DeWeSetParamStruct_str(sTarget, "LPFilter_Val", "10000 Hz");
    nErrorCode = DeWeSetParamStruct_str( sTarget, "IIRFilter_VAL", "10 Hz");
    CheckError(nErrorCode);

    // Calculate Scaling Values with the really set values
    nErrorCode = GetAdjustedRange(sTarget, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)DATAWIDTH);
    CheckError(nErrorCode);

    // Update Properties and Start Acquisition ..

    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Just some read-outs ..
    printf("\nINFO Print-Outs:\n");

    nErrorCode = DeWeGetParamStruct_str( sTarget , "Range", sResultStr, sizeof(sResultStr));
    CheckError(nErrorCode);
    printf("Adjusted Range:    %s\n", sResultStr);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);

    // Determine the size of a sample scan
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan);
    CheckError(nErrorCode);

    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_START_ACQUISITION, 0);
    if (nErrorCode != 0)
    {
        snprintf(sErrorText, sizeof(sErrorText), "%s", DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

    printf("\nMeasurement started on %s/AI%d ..\n\n\n",sBoardId, nChannelNo);

    // Get detailed information about the circular buffer
    // to be able to handle the wrap around
    nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_START_POINTER, &nBufStartPos);
    CheckError(nErrorCode);
    nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_END_POINTER, &nBufEndPos);
    CheckError(nErrorCode);
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
    CheckError(nErrorCode);

    while( !kbhit() )
    {
        sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
        int nAvailSamples=0;
        sint32 nRawData=0;
        int i=0;

        Sleep(10);

        // Get the number of samples already stored in the circular buffer
        nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
        CheckError(nErrorCode);
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
        nReadPos = nReadPos + nADCDelay * nSizeScan;

        // Read the current samples from the circular buffer
        for (i = 0; i < nAvailSamples; ++i)
        {
            // Handle the circular buffer wrap around
            if (nReadPos >= nBufEndPos)
            {
                nReadPos -= nBufSize;
            }

            // Get the sample value at the read pointer of the circular buffer
            // The sample value is 24Bit (little endian, encoded in 32bit).
            nRawData = formatRawData( *(uint32*)nReadPos, (int)DATAWIDTH, 8);
            fVal = (((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd;

            if (0 == i%10)
            {
                printf("\rRaw: %8.8x --> Scaled: %4.3f[degC]          ",nRawData, (float)fVal);
                fflush(stdout);
            }

            // Increment the read pointer
            nReadPos += nSizeScan;
        }

        // Free the circular buffer after read of all values
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
        CheckError(nErrorCode);

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