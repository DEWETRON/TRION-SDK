/**
 * Complex example to show raw and decoded access to TRION-CAN
 *
 * This example should be used with a TRION-CAN board installed
 * or configured in the simulated system
 *
 */

#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "trion_sdk_util.h"


//needed Board-Type for this example
const char* sBoardNameNeeded[] = {  "TRION-CAN",
                                    NULL};

#define CANBUFFER   1000
#define EXTENDED_ARG_PARSING    1

typedef struct tag_config_t
{
    uint32  BoardID;
    BOOL    chn0_used;
    BOOL    chn1_used;
    BOOL    chn2_used;
    BOOL    chn3_used;
    uint32  terminating_port;
    sint32  acking_port;
    uint32  frames_to_read;
    uint32  poll_interval;
    BOOL    readraw;
    BOOL    readdecoded;
    BOOL    verbose;
} config_t, *pconfig_t;

static void PrintParameters(pconfig_t config)
{
    if (!config)
    {
        return;
    }

    fprintf(stderr,"BoardID: %u\n", config->BoardID);
    fprintf(stderr, "Ch0 : %s\n", (config->chn0_used ? "True" : "False"));
    fprintf(stderr, "Ch1 : %s\n", (config->chn1_used ? "True" : "False"));
    fprintf(stderr, "Ch2 : %s\n", (config->chn2_used ? "True" : "False"));
    fprintf(stderr, "Ch3 : %s\n", (config->chn3_used ? "True" : "False"));
    fprintf(stderr, "Terminating Port: %i\n", config->terminating_port);
    fprintf(stderr, "Acking Port: %i\n", config->acking_port);
    fprintf(stderr, "Frames to read: %u\n", config->frames_to_read);
    fprintf(stderr, "Poll interval: %u\n", config->poll_interval);
    fprintf(stderr, "ReadRaw: %s\n", (config->readraw)?"True" : "False");
    fprintf(stderr, "ReadDecoded: %s\n", (config->readdecoded) ? "True" : "False");
    fprintf(stderr, "Verbose: %s\n", (config->verbose) ? "True" : "False");

    fflush(stderr);
}


static void DefaultParameters(pconfig_t config)
{
    if (!config)
    {
        return;
    }

    config->BoardID = 0;
    config->chn0_used = FALSE;
    config->chn1_used = FALSE;
    config->chn2_used = FALSE;
    config->chn3_used = FALSE;
    config->terminating_port = -1;
    config->acking_port = -1;
    config->frames_to_read = 100;
    config->poll_interval = 100;
    config->readraw = FALSE;
    config->readdecoded = FALSE;
    config->verbose = FALSE;
}

static BOOL ischannelused(sint32 chnno, const pconfig_t config)
{
    BOOL isused;

    isused = FALSE;
    switch (chnno)
    {
    case -1:
        isused = TRUE;
        //always ok
        break;
    case 0:
        isused = config->chn0_used;
        break;
    case 1:
        isused = config->chn1_used;
        break;
    case 2:
        isused = config->chn2_used;
        break;
    case 3:
        isused = config->chn3_used;
        break;
    default:
        //TESNO?
        return FALSE;
        //break;
    }

    return isused;
}

BOOL parseARGS(int argc, char **argv, pconfig_t config)
{
    //bx        BoardID [x| 0 < x <=128], default 0
    //cx        [x| 0 <= x <= 3], if present enables specific channel
    //fx        [x| 0 <= x < MAX_UINT32] frames to read, defualt 100
    //tx        [x| 0 <= x <= 3 ^ x=-1] port that is terminating (-1 if none), default none; must be a used one
    //ax        [x| 0 <= x <= 3 ^ x=-1] port that is acking (-1 if none), defualt none; must be a used one
    //px        [x| 0 < x <= 1000] sleep time between polls, default 100
    //rr        read raw, default false
    //rd        read decoded, default false
    //v         enable verbose

    int i;

    if (!config)
    {
        return FALSE;
    }

    DefaultParameters(config);

    for (i = 1; i < argc; ++i)
    {
        char* arg = argv[i];

        switch (*arg)
        {
        case 'h':
            fprintf(stderr, "bx        BoardID [x| 0 < x <=128], default 0\n");
            fprintf(stderr, "cx        [x| 0 <= x <= 3], if present enables specific channel\n");
            fprintf(stderr, "fx        [x| 0 <= x < MAX_UINT32] frames to read, defualt 100\n");
            fprintf(stderr, "tx        [x| 0 <= x <= 3 ^ x=-1] port that is terminating (-1 if none), default none; must be a used one\n");
            fprintf(stderr, "ax        [x| 0 <= x <= 3 ^ x=-1] port that is acking (-1 if none), defualt none; must be a used one\n");
            fprintf(stderr, "px        [x| 0 < x <= 1000] sleep time between polls, default 100\n");
            fprintf(stderr, "rr        read raw, default false\n");
            fprintf(stderr, "rd        read decoded, default false\n");
            fflush(stderr);
            return FALSE;
            break;
        case 'v':
            config->verbose = TRUE;
            break;
        case 'b':
                //boardID
            if (1 != sscanf((arg + 1), "%u", &config->BoardID))
            {
                fprintf(stderr, "Illegal Boardnumber: %s\n", arg);
                fflush(stderr);
                return FALSE;
            }
            break;
        case 't':
                //terminating port
            if (1 != sscanf((arg + 1), "%i", &config->terminating_port))
            {
                fprintf(stderr, "Illegal terminating port: %s\n", arg);
                fflush(stderr);
                return FALSE;
            }
            break;
        case 'a':
            //terminating port
            if (1 != sscanf((arg + 1), "%i", &config->acking_port))
            {
                fprintf(stderr, "Illegal acking port: %s\n", arg);
                fflush(stderr);
                return FALSE;
            }
            break;
        case 'c':
                //activate channel
            switch (*(arg+1))
            {
            case '0':
                config->chn0_used = TRUE;
                break;
            case '1':
                config->chn1_used = TRUE;
                break;
            case '2':
                config->chn2_used = TRUE;
                break;
            case '3':
                config->chn3_used = TRUE;
                break;
            default:
                fprintf(stderr, "Illegal channel-no? argument: %s\n", arg);
                fflush(stderr);
                return FALSE;
            }
            break;
        case 'f':
                //frames to receive
            if (1 != sscanf((arg + 1), "%u", &config->frames_to_read))
            {
                fprintf(stderr, "Illegal frames to receive argument: %s\n", arg);
                fflush(stderr);
                return FALSE;
            }
            break;
        case 'p':
                //polling intervall
            if (1 != sscanf((arg + 1), "%u", &config->poll_interval))
            {
                fprintf(stderr, "Illegal polling interval argument: %s\n", arg);
                fflush(stderr);
                return FALSE;
            }
            break;
        case 'r':
                //read (raw/decoded)
            switch (*(arg + 1))
            {
            case 'r':
                config->readraw = TRUE;
                break;
            case 'd':
                config->readdecoded = TRUE;
                break;
            default:
                fprintf(stderr, "Illegal read strategy argument: %s\n", arg);
                fflush(stderr);
                return FALSE;
            }
            break;
        default:
            fprintf(stderr, "Unknown argument: %s\n", arg);
            fflush(stderr);
            return FALSE;
            //break;
        }
    }

    if (!(config->chn0_used | config->chn1_used | config->chn2_used | config->chn3_used))
    {
        //Do not wait for any frame, if no channel is active
        config->frames_to_read = 0;
    }

    //check if terminating port is an active one
    if (!ischannelused(config->terminating_port, config))
    {
        fprintf(stderr, "terminating port %i is not used\n", config->terminating_port);
        fflush(stderr);
        return FALSE;
    }

    //check if acking port is an active one
    if (!ischannelused(config->acking_port, config))
    {
        fprintf(stderr, "acking port %i is not used\n", config->acking_port);
        fflush(stderr);
        return FALSE;
    }

    if ((config->poll_interval < 1) || (config->poll_interval > 1000))
    {
        fprintf(stderr, "poll intervall %u not >0 or not <=1000\n", config->poll_interval);
        fflush(stderr);
        return FALSE;
    }

    return TRUE;
}

static void print_can_frame_decoded_pydict(const PBOARD_CAN_FRAME frame)
{
    uint32 i;

    fprintf(stdout, "{");

    fprintf(stdout, "'channel': 'd%u', ", frame->CanNo);
    fprintf(stdout, "'timestamp': 0x%x, ", frame->SyncCounter );
    fprintf(stdout, "'id': 0x%x, ", frame->MessageId );;

    fprintf(stdout, "'data': [");;

    //don not revert agin
    for (i = 0; i < frame->DataLength; ++i)
    {
        fprintf(stdout, "0x%x,", frame->CanData[i]);
    }
    fprintf(stdout, "]");
    fprintf(stdout, "}\n");

    fflush(stdout);
}

static BOOL check_canframe_decoded_pattern(const PBOARD_CAN_FRAME frame)
{
    static uint8 expmsgid[4] = { 0, 0, 0, 0 };
    static uint8 expmsg[4][8] = {
        { 0, 2, 4, 8, 16, 32, 64, 128 },
        { 0, 2, 4, 8, 16, 32, 64, 128 },
        { 0, 2, 4, 8, 16, 32, 64, 128 },
        { 0, 2, 4, 8, 16, 32, 64, 128 }
    };

    int i;

    uint8 idx = frame->CanNo;
    if (idx > 3)
    {
        return FALSE;
    }
    expmsgid[idx] += 1;
    for (i = 0; i < 8; ++i)
    {
        expmsg[idx][i] += 1;
    }

    if (frame->MessageId != expmsgid[idx])
    {
        fprintf(stderr, "MSGID %x != %x !\n", frame->MessageId, expmsgid[idx]);
        return FALSE;
    }

    for (i = 0; i < 8; ++i)
    {
        if (expmsg[idx][i] != frame->CanData[i])
        {
            fprintf(stderr, "DATA differs  %x != %x\n", frame->CanData[i], expmsgid[idx]);
            return FALSE;
        }
    }

    return TRUE;
}


static uint32 decodeMessageId(const PBOARD_CAN_RAW_FRAME pframe)
{
    if (((pframe->Hdr >> 30) & 0x1) != 0) {
        return (pframe->Hdr & 0x1fffffff);
    }
    else {
        return ((pframe->Hdr >> 18) & 0x7ff);
    }
}


static void print_can_frame_raw_pydict(const PBOARD_CAN_RAW_FRAME frame)
{
    int i;
    fprintf(stdout, "{");

    fprintf(stdout, "'channel': 'r%u', ", (uint8)((frame->Err >> 28) & 0xF));
    fprintf(stdout, "'timestamp': 0x%x, ", frame->Pos);
    fprintf(stdout, "'id': 0x%x, ", decodeMessageId(frame));

    fprintf(stdout, "'data': [");;

    for (i = ((frame->Err >> 24) & 0xf) - 1; i >= 0; i--)
    {
        fprintf(stdout, "0x%x,", frame->Data[i]);
    }
    fprintf(stdout, "]");
    fprintf(stdout, "}\n");

    fflush(stdout);
}




int main(int argc, char* argv[])
{
    int nNoOfBoards=0;
    int nErrorCode = 0;
    int nBoardID = 0;
    char sChannelStr[256]={0};
    char sErrorText[256]={0};
    char sBoardID[256]={0};
    BOARD_CAN_FRAME aDecodedFrame[CANBUFFER];
    config_t        config;
    int i;
    int chnno;

    DefaultParameters(&config);

#ifdef EXTENDED_ARG_PARSING
    if (TRUE != parseARGS(argc, argv, &config))
    {
        snprintf(sErrorText, sizeof(sErrorText), "Error parsing parameters\n");
        return UnloadTrionApi(sErrorText);
    }
    else {
        PrintParameters(&config);
    }
#endif

    // Load pxi_api.dll
    if ( 0 != LoadTrionApi() )
    {
        return 1;
    }

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
#ifndef EXTENDED_ARG_PARSING
    if( TRUE != ARG_GetBoardId(argc, argv, nNoOfBoards, &nBoardID) )
    {
        snprintf(sErrorText, sizeof(sErrorText), "Invalid BoardId: %d\nNumber of found boards: %d", nBoardID, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    } else {
        config.BoardID = nBoardID;
    }
#endif

    nBoardID = config.BoardID;
    if (nBoardID >= nNoOfBoards)
    {
        snprintf(sErrorText, sizeof(sErrorText), "BoardID %d (zero based!) exceeds number of Boards %d: ", nBoardID, nNoOfBoards);
        return UnloadTrionApi(sErrorText);
    }


    // Build a string in the format: "BoardID0", "BoardID1", ...
    snprintf(sBoardID, sizeof(sBoardID),"BoardID%d", nBoardID);

    // Open & Reset all boards
    for (i = 0; i < nNoOfBoards; ++i)
    {
        nErrorCode = DeWeSetParam_i32(i, CMD_OPEN_BOARD, 0);
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParam_i32(i, CMD_RESET_BOARD, 0);
        CheckError(nErrorCode);
        if (i != nBoardID)
        {
            //close all unused boards
            nErrorCode = DeWeSetParam_i32(i, CMD_CLOSE_BOARD, 0);
            CheckError(nErrorCode);
        }
    }

    /// Check if selected board is suitable for test
    if ( FALSE == TestBoardType(nBoardID, sBoardNameNeeded))
    {
        return UnloadTrionApi(NULL);
    }

    // Set configuration to use one board in standalone operation
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/AcqProp", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "OperationMode", "Master");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "ExtTrigger", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr, "ExtClk", "False");
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str(sChannelStr, "SampleRate", "250000");
    CheckError(nErrorCode);


    // configure the BoardCoutner-channel
    // for HW - timestamping to work it is necessary to have
    // at least one synchronous channel active. All TRION
    // boardtypes support a channel called Board-Counter (BoardCNT)
    // this is a basic counter channel, that usually has no
    // possibility to feed an external signal, and is usually
    // used to route internal signals to its input
    snprintf(sChannelStr, sizeof(sChannelStr),"%s/BoardCNT0", sBoardID);
    nErrorCode = DeWeSetParamStruct_str( sChannelStr , "Used", "True");
    CheckError(nErrorCode);

    // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    // For the default sample-rate 2000 samples per second, 200 is a buffer for
    // 0.1 seconds
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_BLOCK_SIZE, 50000);
    CheckError(nErrorCode);

    // Set the ring buffer size to 50 blocks. So ring buffer can store samples
    // for 5 seconds
    nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_BLOCK_COUNT, 200);
    CheckError(nErrorCode);

    for (chnno = 0; chnno < 4; ++chnno)
    {
        // configure the given CAN-channel
        // only two properties that need to be changed for this example are
        // SyncCounter: set it to 10Mhz, so the CAN Data will have timestamps with
        // Used: enable the channel for usage
        snprintf(sChannelStr, sizeof(sChannelStr),"%s/CAN%d", sBoardID, chnno);
        nErrorCode = DeWeSetParamStruct_str(sChannelStr, "SyncCounter", "10 MHzCount");
        if (CheckError(nErrorCode))
        {
            return UnloadTrionApi("Error at configuring Synccounter\nAborting.....\n");
        }

        if (chnno == config.terminating_port)
        {
            nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Termination", "True");
        }
        else {
            nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Termination", "False");
        }
        if (CheckError(nErrorCode))
        {
            return UnloadTrionApi("Error at configuring Termination\nAborting.....\n");
        }


        if (chnno == config.acking_port)
        {
            nErrorCode = DeWeSetParamStruct_str(sChannelStr, "ListenOnly", "False");
        }
        else {
            nErrorCode = DeWeSetParamStruct_str(sChannelStr, "ListenOnly", "True");
        }
        if (CheckError(nErrorCode))
        {
            return UnloadTrionApi("Error at configuring ListenOnly\nAborting.....\n");
        }


        nErrorCode = DeWeSetParamStruct_str(sChannelStr, "BaudRate", "500000");
        if (CheckError(nErrorCode))
        {
            return UnloadTrionApi("Error at configuring BaudRate\nAborting.....\n");
        }

        if (ischannelused(chnno, &config))
        {
            nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Used", "True");
        }
        else {
            nErrorCode = DeWeSetParamStruct_str(sChannelStr, "Used", "False");
        }
        if (CheckError(nErrorCode))
        {
            return UnloadTrionApi("Error at configuring Used-flag\nAborting.....\n");
        }
    }

    // Open the CAN - Interface to this Board
    nErrorCode = DeWeOpenCAN(nBoardID);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at opening CAN-Interface\nAborting.....\n");
    }

    // Configure the ASYNC-Polling Time to 100ms
    // Configure the Frame-Size (CAN == 8)
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_ASYNC_POLLING_TIME, config.poll_interval);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal polling time\nAborting.....\n");
    }

#if 0
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_ASYNC_FRAME_SIZE, 8);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal frame size\nAborting.....\n");
    }
#endif

    // Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_UPDATE_PARAM_ALL, 0);
    if ( CheckError(nErrorCode))
    {
        return UnloadTrionApi("Error at configuring internal frame size\nAborting.....\n");
    }

    // Start CAN capture, before start sync-acquisition
    // the sync - acquisition will synchronize the async data
   nErrorCode = DeWeStartCAN(nBoardID, -1 );
   if ( CheckError(nErrorCode))
   {
       return UnloadTrionApi("Error at starting CAN\nAborting.....\n");
   }

    // Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoardID, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);
    if (nErrorCode <= 0)
    {
        uint32 frames_aggregated = 0;
        uint32 last_frames_aggregated = 0xFFFFFFFF;
        // the synchronous data won't be evaluated at all
        // the samples will immediately being freed - just
        // to prevent an overrun - error (cosmetic)
        fprintf(stderr, "\nAcquisition started. Waiting for CAN frames\n\n\n");

        while (frames_aggregated < config.frames_to_read)
        {
            int nAvailSamples = 0;
            int nAvailCanMsgs = 0;
            int nRawAvailMsgs = 0;
            int i = 0;
            float timestamp = 0.0f;

            // wait for poll interval
            // CAN data are typically slow anyway
            // any longer or shorter timespan is also feasible
            Sleep(config.poll_interval);

            // Get the number of samples already stored in the ring buffer
            nErrorCode = DeWeGetParam_i32(nBoardID, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples);
            CheckError(nErrorCode);

            // Free the ring buffer
            nErrorCode = DeWeSetParam_i32(nBoardID, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples);
            CheckError(nErrorCode);

            // now obtain all CAN - frames that have been collected in this timespan
            nAvailCanMsgs = 0;

            if (config.readraw)
            {
                nRawAvailMsgs = 0;
                PBOARD_CAN_RAW_FRAME frameptr;
                nErrorCode = DeWeReadCANRawFrame(nBoardID, &frameptr, &nRawAvailMsgs);

                for ( i = 0; i < nRawAvailMsgs; ++i)
                {
                    PBOARD_CAN_RAW_FRAME curframe = frameptr + i;
                    print_can_frame_raw_pydict(curframe);
                }

                if (!config.readdecoded)
                {
                    frames_aggregated += nRawAvailMsgs;
                    nErrorCode = DeWeFreeFramesCAN(nBoardID, nRawAvailMsgs);
                }
            }

            if (config.readdecoded)
            {
                do {
                    int maxmsg = CANBUFFER;
                    if (config.readraw)
                    {
                        maxmsg = (maxmsg > nRawAvailMsgs) ? nRawAvailMsgs : maxmsg;
                    }
                    nAvailCanMsgs = 0;
                    // DeWeReadCAN makes a copy of CAN data and does not require a separate DeWeFree*() call
                    nErrorCode = DeWeReadCAN(nBoardID, &aDecodedFrame[0], maxmsg, &nAvailCanMsgs);
                    if (CheckError(nErrorCode))
                    {
                        fprintf(stderr, "Error at obtaining CAN - Frames\nAborting.....\n");
                        fflush(stderr);
                        return UnloadTrionApi("Error at obtaining CAN - Frames\nAborting.....\n");
                    }

                    frames_aggregated += nAvailCanMsgs;

                    if (config.readraw)
                    {
                        if (nAvailCanMsgs > nRawAvailMsgs)
                        {
                            fprintf(stderr, "HUH? nAvailCanMsgs > nRawAvailMsgs %d %d\nAborting.....\n", nAvailCanMsgs, nRawAvailMsgs);
                            fflush(stderr);
                            return UnloadTrionApi("HUH? nAvailCanMsgs > nRawAvailMsgs\nAborting.....\n");
                        }
                        nRawAvailMsgs -= nAvailCanMsgs;
                    }
                    else {
                        //safety
                        nRawAvailMsgs = 0;
                    }

                    for (i = 0; i < nAvailCanMsgs; ++i)
                    {
#if 0
                        timestamp = (float)((float)(aDecodedFrame[i].SyncCounter) / 10000000);  //Timestamp in 100ns re-formated to seconds
                        // note here: with a 10Mhz counter @ 32Bit width, the timestamp will wrap
                        // around after roughly 7 minutes. This Warp around has to be handled by the
                        // application on raw data
                        printf("[%012.7f] MSGID: %8.8X   Port: %d   Errorcount: %d   DataLen: %d   Data: %2.2X %2.2X %2.2X %2.2X   %2.2X %2.2X %2.2X %2.2X\n",
                            timestamp,
                            aDecodedFrame[i].MessageId,
                            aDecodedFrame[i].CanNo,
                            aDecodedFrame[i].ErrorCounter,
                            aDecodedFrame[i].DataLength,
                            aDecodedFrame[i].CanData[0], aDecodedFrame[i].CanData[1], aDecodedFrame[i].CanData[2], aDecodedFrame[i].CanData[3],
                            aDecodedFrame[i].CanData[4], aDecodedFrame[i].CanData[5], aDecodedFrame[i].CanData[6], aDecodedFrame[i].CanData[7]
                            );
#else
                        print_can_frame_decoded_pydict(&aDecodedFrame[i]);
                        if (!check_canframe_decoded_pattern(&aDecodedFrame[i]))
                        {
                            fprintf(stderr, "ERROR at check!\n");
                            fflush(stderr);
                            exit(1);
                        }
#endif
                    }
                } while (nRawAvailMsgs > 0);
            }
            if (config.verbose)
            {
                if (last_frames_aggregated != frames_aggregated)
                {
                    fprintf(stderr, "\r%u of %u aggregated\n", frames_aggregated, config.frames_to_read);
                    fflush(stderr);
                    last_frames_aggregated = frames_aggregated;
                }
            }
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
