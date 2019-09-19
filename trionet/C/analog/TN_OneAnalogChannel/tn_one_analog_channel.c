/**
 * Short example to describe how an analog channel is read.
 *
 * This example should be used with a TRION board featuring analog channels
 *
 * Describes following:
 *  - Setup of 1 AI channel
 *  - Query for the ADC delay
 *  - Print of analog values.
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

//either 16 or 24, 32
// 16Bitness should only be used with one odd channel (1,3,5,7) - otherwise
// the example code would become rather bloated
#define DATAWIDTH   24


void configureNetwork();


//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-dACC",
                                    "TRION-2402-dSTG",
                                    "TRION-1620-LV",
                                    "TRION-1620-ACC",
                                    "TRION-1603-LV",
                                    "TRION-2402-MULTI",
                                    "TRION-2402-V",
                                    "TRION-1600-dLV",
                                    "TRION-1802-dLV",
                                    "TRION-1820-POWER",
                                    "TRION-1810M-POWER",
                                    "TRION-AVALON-DEV",
                                    NULL };


int main(int argc, char* argv[])
{
    int nBoardId = 0;
    char sBoardId[256]   = {0};
    char sTarget[256]    = {0};
    char sErrorText[256] = {0};
    char sSetingBuff[1024] = { 0 };
    int nNoOfBoards = 0;
    int nErrorCode  = 0;
    int nADCDelay   = 0;
    const double fRange = 10;  //V
    char sRangeStr[48] = {0};
    ScaleInfo scaleinfo;
    RangeSpan rangespan;
    int sample_offset = 8;

    ScaleInfo api_scaleinfo;

    // Load pxi_api.dll (the TRIONET Wrapper API)
    if ( 0 != LoadTrionApi() )
    {
        return 1;
    }

    // get access to trionet
    configureNetwork();

    // Attention:
    // sample_offset: legacy boards and 24 bit -> 8
    //                TRION-1802-dLV and 24 bit 0
    // -> Better use scan descriptor
    //sample_offset = 0; // TRION-1802-dLV
    sample_offset = 8; // eg -STG


    // Initialize driver and retrieve the number of TRION boards
    // nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode = DeWeDriverInit(&nNoOfBoards);
    CheckError(nErrorCode);
    nNoOfBoards=abs(nNoOfBoards);

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

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardId, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

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
    // Set AI Resolution
    memset(sRangeStr, 0, sizeof(sRangeStr));
    snprintf(sRangeStr, sizeof(sRangeStr), "%d", DATAWIDTH);
    nErrorCode = DeWeSetParamStruct_str(sTarget, "ResolutionAI", sRangeStr);
    CheckError(nErrorCode);



    // After reset all channels are disabled.
    // So here 1 analog channel will be enabled (AI)
    snprintf(sTarget, sizeof(sTarget), "%s/%s", sBoardId, "AI0");
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Used", "True");
    if (nErrorCode)
    {
        snprintf(sErrorText, sizeof(sErrorText), "Could not enable AI channel on board %d: %s\n", nBoardId, DeWeErrorConstantToString(nErrorCode));
        return UnloadTrionApi(sErrorText);
    }

    // Set 10V range
    snprintf(sRangeStr, sizeof(sRangeStr), "%f V", fRange);
    nErrorCode = DeWeSetParamStruct_str( sTarget , "Range", sRangeStr);
    CheckError(nErrorCode);


    // Calculate Scaling Values with the really set values
    nErrorCode = GetAdjustedRange(sTarget, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)DATAWIDTH);
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


    // Get API Scale Value
    SetScaling(&api_scaleinfo, sTarget);


    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        sint64 nBufStartPos = 0;
        sint64 nBufEndPos = 0;         // Last position in the ring buffer
        int nBufSize = 0;              // Total buffer size
        double fVal = 0;
        double fApiVal = 0;
        double fMinVal = 0;
        double fMaxVal = 0;

        printf("\nMeasurement Started on Brd: %s/AI0:..\n\n\n",sBoardId);
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

            //Sleep(100);

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
                nRawData = formatRawData(*(sint32*)nReadPos, (int)DATAWIDTH, sample_offset);
                fVal = ((((double)(nRawData) * scaleinfo.fScaling)) - scaleinfo.fd);
                fApiVal = ((((double)(nRawData)* api_scaleinfo.fScaling)) - api_scaleinfo.fd);

                //if (0 == i%99)
                {
                    //fVal = fVal - 10;
                    //fApiVal = fApiVal - 10;
                    if (fVal < fMinVal) fMinVal = fVal;
                    if (fVal > fMaxVal) fMaxVal = fVal;
                    printf("\nRaw: %8.8x   Scaled: %12.12f    ApiScaled: %12.12f   Min:  %12.3f  Max:  %12.3f", nRawData, fVal, fApiVal, fMinVal, fMaxVal);
                    fflush(stdout);
                }

                // Increment the read pointer
                nReadPos += sizeof(uint32);
            }

            // Free the ring buffer after read of all values
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


void configureNetwork()
{
    int nErrorCode;

    // Optional: Prints available network interfaces
    // ListNetworkInterfaces();

    char* address = "127.0.0.1";
    char* netmask = "255.255.255.0";
    printf("Example is listening for TRIONET devices on: %s (%s)\n", address, netmask);

    // TODO: Configure the network interface to access TRIONET devices
    nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/LocalIP", address);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/NetMask", netmask);
    CheckError(nErrorCode);
}
