/**
 * Example to showcase using MSI-adapter measurement on a CB16D Module
 *
 * The Board is selectable over cmd line parameter 1. Default Board Nr. = 0
 * The AIChannel is selectable over cmd line parameter 2. Default Channel Nr. = 0
 * This example should be used with a TRION-1600-dLV, TRION-1802-dLV or PUREC.
 *
 * Note: CGB16D has to be connected before running this example.
 * The example has to process the MSI adapters TEDS respoonse.
 * The example hat to now the supportes MSI adapters or process the accompanied
 * msib.xml. 
 *
 * Features:
 *  - Scan and show MSI Adapter on given channel
 *  - Read MSI configuration
 *  - Configure channel
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"
#include "math.h"


//either 16 or 24
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-1802-dLV",
                                    "TRION-1600-dLV",
                                    "PUREC-",
                                    NULL};


int configureAcquisition(int nBoardId);
int configureChannel(int nBoardId, int nChannelNo, const char* teds_xml, ScaleInfo* scaleinfo);
int runAcquisition(int nBoardId, int nChannelNo, ScaleInfo* scaleinfo);



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
    double fVal=0;
    int nSizeScan=0;
    ScaleInfo scaleinfo;


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
        snprintf(sErrorText, sizeof(sErrorText), "Invalid Channel Number: Allowed channel numbers are from [0..15]\n");
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
        printf("ROM CODE:\n");
        DumpXmlTree(TEDS_DATA);
    }

    // configure sample rate and buffer
    nErrorCode = configureAcquisition(nBoardId);
    if (CheckError(nErrorCode))
    {
        snprintf(sErrorText, sizeof(sErrorText), "configureAcquisition failed.");
        return UnloadTrionApi(sErrorText);
    }

    // configure channel
    nErrorCode = configureChannel(nBoardId, nChannelNo, TEDS_DATA, &scaleinfo);
    if (CheckError(nErrorCode))
    {
        snprintf(sErrorText, sizeof(sErrorText), "configureChannel failed.");
        return UnloadTrionApi(sErrorText);
    }

    // run acquisition loop
    nErrorCode = runAcquisition(nBoardId, nChannelNo, &scaleinfo);
    if (CheckError(nErrorCode))
    {
        snprintf(sErrorText, sizeof(sErrorText), "runAcquisition failed.");
        return UnloadTrionApi(sErrorText);
    }


    // Close the board connection
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}


int configureAcquisition(int nBoardId)
{
    int nErrorCode = 0;
    char sPropertyStr[256] = { 0 };

     // Set Acquisition Properties for Board
    snprintf(sPropertyStr, sizeof(sPropertyStr), "BoardID%d/AcqProp", nBoardId);
    nErrorCode = DeWeSetParamStruct_str(sPropertyStr, "Samplerate", "200");
    CheckError(nErrorCode);
    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    nErrorCode = DeWeSetParam_i32(nBoardId, CMD_BUFFER_BLOCK_SIZE, 20);
    CheckError(nErrorCode);
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32(nBoardId, CMD_BUFFER_BLOCK_COUNT, 50);
    CheckError(nErrorCode);

    return nErrorCode;
}


int configureChannel(int nBoardId, int nChannelNo, const char* teds_xml, ScaleInfo* scaleinfo)
{
    int nErrorCode = 0;
    char sTarget[256] = { 0 };
    char sErrorText[256] = { 0 };
    char sXPath[256] = { 0 };
    char sMSIData[256] = { 0 };
    double min_range = 0;
    double max_range = 0;
    double min_phys_val = 0;
    double max_phys_val = 0;
    double two_point_factor = 1;
    double two_point_offset = 0;
    RangeSpan rangespan;

    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI)
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/AI%d", nBoardId, nChannelNo);
    nErrorCode = DeWeSetParamStruct_str(sTarget, "Used", "True");
    if (nErrorCode)
    {
        snprintf(sErrorText, sizeof(sErrorText), "Could not enable AI channel on board %d: %s\n", nBoardId, DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

    // apply channel requirements

    // For CB16 the 10V range has to be selected
    // Voltage mode is the only supported mode - so can be omitted
    nErrorCode = DeWeSetParamStruct_str(sTarget, "Range", "10V");
    CheckError(nErrorCode);

    // Get range back (just to be sure, it could be adjusted)
    nErrorCode = GetAdjustedRange(sTarget, &rangespan);
    CheckError(nErrorCode);

    // INFO: Here the API is used to read the XML properties.
    // You can use any XML library you prefer instead!

    // Bare minimum MSI processing using TRION API internal functions
    // symmetric ranges & linear scaling only

    // MSIType
    snprintf(sTarget, sizeof(sTarget), "BoardID%d/aitedsex/AI%d", nBoardId, nChannelNo);
    snprintf(sXPath, sizeof(sXPath), "TEDSData/MSIInfo/@MSIType");
    nErrorCode = DeWeGetParamXML_str(sTarget, sXPath, sMSIData, sizeof(sMSIData));
    CheckError(nErrorCode);

    // Access special msi type information
    // MinMsiRange & MaxMsiRange 
    min_range = MSI_GetMinRange(sMSIData);
    max_range = MSI_GetMaxRange(sMSIData);
    scaleinfo->unit = MSI_GetMaxRangeUnit(sMSIData);

    // MinPhysVal & MaxPhysVal
    snprintf(sXPath, sizeof(sXPath), "TEDSData/TEDSInfo/Template/Property[@Name='MinPhysVal']");
    nErrorCode = DeWeGetParamXML_str(sTarget, sXPath, sMSIData, sizeof(sMSIData));
    CheckError(nErrorCode);
    sscanf(sMSIData, "%lf", &min_phys_val);

    snprintf(sXPath, sizeof(sXPath), "TEDSData/TEDSInfo/Template/Property[@Name='MaxPhysVal']");
    nErrorCode = DeWeGetParamXML_str(sTarget, sXPath, sMSIData, sizeof(sMSIData));
    CheckError(nErrorCode);
    sscanf(sMSIData, "%lf", &max_phys_val);


    // Calculate 2point scaling
    two_point_factor = (fabs(min_range) + fabs(max_range)) / (fabs(min_phys_val) + fabs(max_phys_val));
    two_point_offset = min_range - two_point_factor * min_phys_val;

    // Apply scaling
    rangespan.rmin = rangespan.rmin * two_point_factor + two_point_offset;
    rangespan.rmax = rangespan.rmax * two_point_factor + two_point_offset;

    nErrorCode = CalcScaling(scaleinfo, rangespan.rmin, rangespan.rmax, (int)DATAWIDTH);
    CheckError(nErrorCode);

    return nErrorCode;
}


int runAcquisition(int nBoardId, int nChannelNo, ScaleInfo* scaleinfo)
{
    int nErrorCode = 0;
    char sErrorText[256] = { 0 };
    int nADCDelay = 0;
    int nSizeScan = 0;
    sint64 nBufStartPos = 0;
    sint64 nBufEndPos = 0;
    int nBufSize = 0;
    double fVal = 0;

    // Update Properties and Start Acquisition ..
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Get the ADC delay. The typical conversion time of the ADC.
    // The ADCDelay is the offset of analog samples to digital or counter samples.
    // It is measured in number of samples,
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BOARD_ADC_DELAY, &nADCDelay);
    CheckError(nErrorCode);

    // Determine the size of a sample scan
    nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan);
    CheckError(nErrorCode);

    nErrorCode = DeWeSetParam_i32(nBoardId, CMD_START_ACQUISITION, 0);
    if (nErrorCode != 0)
    {
        snprintf(sErrorText, sizeof(sErrorText), "%s", DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }


    printf("\nMeasurement started on Board%d/AI%d ..\n\n\n", nBoardId, nChannelNo);

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
        sint32 nRawData=0;
        int i=0;

        Sleep(10);

        // Get the number of samples already stored in the ring buffer
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
            nRawData = formatRawData( *(uint32*)nReadPos, (int)DATAWIDTH, 8);
            fVal = (((double)(nRawData) * scaleinfo->fScaling)) - scaleinfo->fd;

            //if (0 == i%10)
            {
                printf("\nRaw: %8.8x --> Scaled: %4.3lf %s        ", nRawData,fVal, scaleinfo->unit);
                fflush(stdout);
            }

            // Increment the read pointer
            nReadPos += nSizeScan;
        }

        // Free the ring buffer after read of all values
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
        CheckError(nErrorCode);

    }

    // Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_STOP_ACQUISITION, 0);
    CheckError(nErrorCode);

    return nErrorCode;
}
