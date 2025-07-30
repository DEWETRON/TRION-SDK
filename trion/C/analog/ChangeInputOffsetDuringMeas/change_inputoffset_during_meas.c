/**
 * Short example to show the functionality of InputOffset
 *
 * for analogue channels of a TRION-dSTG or TRION-MULTI board
 *
 *
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "dewepxi_types.h"
#include "trion_sdk_util.h"

char HandleKeyIn(int BoardNo, double newoffset);

/**
* @brief  read data from ringbuffer, perform scaling. the last value of AI0 is returned (could be extended to become real average)
*
* @param  boardno       boarnumber to operate on
* @param  nosamples     number of samples to process
* @param  scaleinfo     scalinfo struct for scaling (attention: all channels use the same in this example; just for sake of simplicity)
* @param  show          print to console?
* @param  davg0         pointer to double, holding the last scaled value of AI0 (should become average)
* @return int           0 on success
*/
int process_samples(int boardno, int nosamples, const ScaleInfo* scaleinfo, BOOL show, double* davg0);

//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-2402-dSTG",
                                    "TRION-2402-MULTI",
                                    "TRION-2402-dACC",
                                    "TRION-1603-ACC",
                                    "TRION-1620-ACC",
                                    NULL};

//Escape - Key
static const char KEY_ESC = 27;


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

    const double fRange = 5;  //V
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
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Mode", "Voltage");
    CheckError(nErrorCode);
    // switch to one of the lower Ranges, to make effect more obvious
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "Range", "5 V");
    CheckError(nErrorCode);
    // Calculate Scaling Values with the really set values
    nErrorCode = GetAdjustedRange(sSettingStr, &rangespan);
    CheckError(nErrorCode);
    nErrorCode = CalcScaling(&scaleinfo, rangespan.rmin, rangespan.rmax, (int)(24));
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
    nErrorCode = DeWeSetParamStruct_str( sSettingStr, "Samplerate", "1000");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_SIZE, 100);
    CheckError(nErrorCode);
    // Set the circular buffer size to 50 blocks. So circular buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_BLOCK_COUNT, 50);
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

    printf("Acquisition Running\n");
    printf("Press:\n");
    printf("   q - set input-offset back to 0 V\n");
    printf("   a  - Apply the current Average of Channel 0 as Input Offset to all channels\n");
    printf("   <ESC> - to terminate Acquisition and application\n");
    printf("\n");


    if (nErrorCode <= 0)
    {
        double dAVG = 0.0f;
        char    hitkey=0;

        while( KEY_ESC != hitkey )
        {
            sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
            int nAvailSamples;

            hitkey = HandleKeyIn(nBoardID, dAVG);

            Sleep(100);

            // Get the number of samples already stored in the circular buffer
            nErrorCode = DeWeGetParam_i32( nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
            if (CheckError(nErrorCode)) break;

            if ( nAvailSamples == 0 )
            {
                continue;
            }

            ++loopcounter;

            if (process_samples( nBoardID, nAvailSamples, &scaleinfo, (loopcounter%50 == 0), &dAVG) < 0)
            {
                break;
            }

            // Free the circular buffer after read of all values
            nErrorCode = DeWeSetParam_i32( nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
            if (CheckError(nErrorCode)) break;

        }

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

int process_samples(int boardno, int nosamples, const ScaleInfo* scaleinfo, BOOL show, double* davg0)
{
    int nErrorCode = 0;
    sint64 nReadPos=0;       // Pointer to the circular buffer read pointer
    int i = 0;
    int j = 0;

    sint64 nBufStartPos = 0;
    sint64 nBufEndPos = 0;         // Last position in the circular buffer
    int nBufSize = 0;              // Total buffer size
    int nSizeScan = 0;
    sint32 nRawData = 0;
    double fVal = 0;


    // Get detailed information about the circular buffer
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

        // Handle the circular buffer wrap around
        if (nReadPos >= nBufEndPos)
        {
            nReadPos -= nBufSize;
        }

        //24 Bit dataformat assumed here for readability
        //iterate over all channels
        for ( j = 0; j < nSizeScan; j+=4 )
        {
            // Get the sample value at the read pointer of the circular buffer
            // The sample value is 24Bit (little endian, encoded in 32bit).
            nRawData = formatRawData( *(sint32*)(nReadPos + j), (int)24 , 8 );
            fVal = ((((double)(nRawData) * scaleinfo->fScaling)) - scaleinfo->fd);

            if ( 0 == j )
            {
                *davg0 = fVal;      //not a real average in this example. :)
                printf("\r Spls: %5.5d AI%d: %6.6f", nosamples, (int)(j/4), fVal);
            }
        }

        nReadPos += nSizeScan;
    }

    return 0;

}

char HandleKeyIn(int BoardNo, double newoffset)
{
    char sChannelStr[256] = {0};
    char sSettingStr[256] = {0};
    char hitkey = 0;
    int nErrorCode = 0;
    BOOL setOffset = FALSE;
    static double offset_to_set = 0.0f;


    if (kbhit())
    {
        hitkey = getch();

        switch (hitkey)
        {
        case 'q':
        case 'Q':
            offset_to_set = 0.0f;
            setOffset = TRUE;
            break;
        case 'a':
        case 'A':
            setOffset = TRUE;
            offset_to_set = offset_to_set + newoffset;  //<-- Here live Dragons!
                                                        // as the measd value is offset by any previous offset we have to roll this over here
                                                        // real-world-solution: Set Offset to 0 first, perform avg, Set new avged value
            break;
        default:
            //handled below <ESC> or don't care if none of them
            break;
        }
    }


    if ( setOffset )
    {
        snprintf(sChannelStr, sizeof(sChannelStr),"BoardID%d/AIAll", BoardNo);
        snprintf(sSettingStr, sizeof(sSettingStr),"%lf V", offset_to_set);
        nErrorCode = DeWeSetParamStruct_str( sChannelStr, "InputOffset", sSettingStr );
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParam_i32( BoardNo, CMD_UPDATE_PARAM_AI, UPDATE_ALL_CHANNELS);
        CheckError(nErrorCode);

        printf("\n\n");
        printf("New InputOffset: %s\n\n", sSettingStr);
        printf("\n\n");
    }

    return hitkey;
}



