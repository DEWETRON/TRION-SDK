/**
 * Short example to describe how to trigger an sensor balance
 *
 * for analogue channels of a TRION-dSTG or TRION-MULTI board
 *
 *
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"


int startblancing(int boardno);
int process_samples(int boardno, int nosamples, const ScaleInfo* scaleinfo, BOOL show);

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-dSTG",
                                    "TRION-2402-MULTI",
                                    NULL};

//just to ilustrate, that this command can be issued, at any time after starting acquisition, during acquisition
#define START_SENSORBALANCE_ON_ITERATION    1000


int main(int argc, char* argv[])
{
    int nNoOfBoards = 0;
    int nErrorCode = 0;
    int nBoardID = 0;
    int nSizeScan=0;
    char sBoardID[256]={0};
    char sChannelStr[256]={0};
    char sErrorText[256] = {0};
    char sSettingStr[32*1024]={0};  //The result-set will be an XML-Document, that may be rather large
    int loopcounter = 0;
    //for "benchmarking"
    int minspls = 9999999;
    int maxspls = 0;

    const double fRange = 10;  //mV/v
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

    // Enable All AI Channels on the given board
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AIALL", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Used", "True");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Mode", "Bridge");
    CheckError(nErrorCode);
    // switch to one of the lower Ranges, to make effect more obvious
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Range", "10 mV/V");
    CheckError(nErrorCode);
    // Calculate Scaling Values with the really set values
    nErrorCode = GetAdjustedRange(sChannelStr, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)24);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_CHN_ALL, 0);
    CheckError(nErrorCode);

    //prepare the acquisition parameters
    // Set configuration to use one board in standalone operation
    snprintf(sSettingStr, sizeof(sSettingStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "OperationMode", "Slave");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "ExtClk", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "Samplerate", "500");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the samplerate 200000 samples per second, 20000 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 1);
    CheckError(nErrorCode);
    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 2000);
    CheckError(nErrorCode);

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    CheckError(nErrorCode);

    // Determine the size of a sample scan
    nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan);
    CheckError(nErrorCode);

    // Start the Measurement
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        BOOL didString = FALSE;
        do
        {
            int nAvailSamples;
            int presamp = 0;
            //Sleep(3);

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);

            if ( (FALSE == didString) && (nAvailSamples > 0) && (0 != loopcounter) )
            {
                minspls = MIN(minspls, nAvailSamples);
                maxspls = MAX(maxspls, nAvailSamples);
            }

            didString = FALSE;

            if ( nAvailSamples > 0 )
            {
                ++loopcounter;
            } else {
                continue;
            }

            if ( loopcounter == START_SENSORBALANCE_ON_ITERATION ) {
                if (startblancing(nBoardID) < 0 )
                {
                    break;
                }
                didString = TRUE;
            } else {
                printf ("doing some arbitrary processing of %d samples on iteration %d", nAvailSamples, loopcounter);
                //show the real data-values to show, that measurement is still running during the balancing operation
                if (process_samples( nBoardID, nAvailSamples, &scaleinfo, (loopcounter%50 == 0)) < 0)
                {
                    break;
                }
            }

            // normally here some form of data-processing would take place

            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &presamp );
            CheckError(nErrorCode);

            // Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            CheckError(nErrorCode);

            if ( loopcounter < START_SENSORBALANCE_ON_ITERATION ){
                continue;
            }

            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            CheckError(nErrorCode);
            if ( (nAvailSamples - presamp) > 10 )
            {
                printf("\n\n\n\nHigh Latency on CMD_BUFFER_AVAIL_NO_SAMPLE of %d\n\n", nAvailSamples);
            }


            // in parallel the application can poll about the state of the balancing process
            // limit to every 10th cycle (expensive str-operation)
            if ( (loopcounter % 10 )== 0)
            {
                didString = TRUE;
                snprintf(sBoardID, sizeof(sBoardID),"BoardID%d/AIAll", nBoardID);
                nErrorCode = DeWeGetParamStruct_str( sBoardID, "SensorOffset", sSettingStr, sizeof(sSettingStr));
                CheckError(nErrorCode);
                if ( nErrorCode > 0 ) {
                    //Error - Trap
                    printf("Error during Balancing: %s\nAborting.....\n", DeWeErrorConstantToString(nErrorCode));
                    break;
                }
                // Test, if the result contains the substring "EstDuration"
                // this will also hold the estimated runtime for the wohol test in ms
                // please not, that this estaimation depends on several details
                // how the application handles acquisition - so it should
                // only be used as a rough estimation - or as used here in this
                // example as a stop-condition
                if ( NULL != strstr(sSettingStr, "EstDuration") )
                {
                    printf("%s\n", sSettingStr);
                } else {
                    DumpXmlTree(sSettingStr);
                    printf("\n\n   Done Balancing\n\n");
                    printf("Min Latency: %d Samples\n", minspls);
                    printf("Max Latency: %d Samples\n", maxspls);
                    printf("\n\n\n");
                    loopcounter = 0;
                    minspls = 9999999;
                    maxspls = 0;
                    break;
                }
            }

        } while (1);

        //stop the Acquisition again
        nErrorCode = DeWeSetParam_i32( nBoardID, CMD_STOP_ACQUISITION, 0);
        CheckError(nErrorCode);
    }

    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_CLOSE_BOARD, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");

    return nErrorCode;
}

int startblancing(int boardno)
{
    char sBoardID[256]={0};
    int nErrorCode = 0;

    printf ("\n\n  starting SensorOffset on Board %d\n", boardno);


    //define the group of channels, the balancing shall be perfomred on
    //for this example balance on channels 1,2,4
    snprintf(sBoardID, sizeof(sBoardID),"BoardID%d", boardno);
    nErrorCode = DeWeSetParamStruct_str( sBoardID, "groupai", "1;2;4;");
    if (CheckError(nErrorCode)) return -1;

    //execute the command on the given group
    //the command is non-blocking, and will immedatly return to allow measurement, as the board is
    //already started
    snprintf(sBoardID, sizeof(sBoardID),"BoardID%d/AIGrp", boardno);
    //nErrorCode = DeWeSetParamStruct_str( sBoardID, "SensorOffset", "100msec");

    //make it balance really long to make it easier to see. In real world this would rathe be 100ms
    nErrorCode = DeWeSetParamStruct_str( sBoardID, "SensorOffset", "1000msec");
    if (CheckError(nErrorCode)) return -1;

    return 0;

}


int process_samples(int boardno, int nosamples, const ScaleInfo* scaleinfo, BOOL show)
{
    int nErrorCode = 0;
    sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
    int i = 0;
    int j = 0;

    sint64 nBufStartPos = 0;
    sint64 nBufEndPos = 0;         // Last position in the ring buffer
    int nBufSize = 0;              // Total buffer size
    int nSizeScan = 0;
    sint32 nRawData = 0;
    double fVal = 0;


    // Get detailed information about the ring buffer
    // to be able to handle the wrap around
    // would be done before start_acq; but to make example easier to read processed here
    nErrorCode = DeWeGetParam_i64( boardno, CMD_BUFFER_START_POINTER, &nBufStartPos);
    if (CheckError(nErrorCode)) return -1;
    nErrorCode = DeWeGetParam_i64( boardno, CMD_BUFFER_END_POINTER, &nBufEndPos);
    if (CheckError(nErrorCode)) return -1;
    nErrorCode = DeWeGetParam_i32( boardno, CMD_BUFFER_TOTAL_MEM_SIZE, &nBufSize);
    if (CheckError(nErrorCode)) return -1;

    nErrorCode = DeWeGetParam_i64( boardno, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
    if (CheckError(nErrorCode)) return -1;

    // Determine the size of a sample scan
    nErrorCode = DeWeGetParam_i32( boardno, CMD_BUFFER_ONE_SCAN_SIZE, &nSizeScan);
    if (CheckError(nErrorCode)) return -1;


    for (i = 0; i < nosamples; ++i)
    {

        // Handle the ring buffer wrap around
        if (nReadPos >= nBufEndPos)
        {
            nReadPos -= nBufSize;
        }

        //24 Bit dataformat assumed here for readability
        //iterate over all channels
        for ( j = 0; j < nSizeScan; j+=4 )
        {
            // Get the sample value at the read pointer of the ring buffer
            // The sample value is 24Bit (little endian, encoded in 32bit).
            nRawData = formatRawData( *(sint32*)(nReadPos + j), (int)24, 8 );
            fVal = ((((double)(nRawData) * scaleinfo->fScaling)) - scaleinfo->fd);

            if ( 0 == j )
            {
                printf("\r Spls: %5.5d AI%d: %6.6f", nosamples, (int)(j/4), fVal);
            } else {
                printf("  AI%d: %6.6f", (int)(j/4), fVal);
            }
        }

        nReadPos += nSizeScan;
    }

    return 0;

}


