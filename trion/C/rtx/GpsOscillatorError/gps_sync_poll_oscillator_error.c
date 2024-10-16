/**
 * Short example to describe how to synchronize the acquisition start with GPS
 * and read the Unix-Timestamp of the exact acquisition start time in UTC time
 *
 * This example requires a TRION3-CONTROLLER
 *
 *
 * Describes the following:
 *  - Configure the API for multi-threaded initialization
 *  - Setup of GPS and 1 AI channel
 *  - Set chassis controller as master
 *  - Set first board as slave
 *  - poll continously for oscillator error 
 */

#ifndef UNDER_RTSS
#error "This example must be compiled for the RTX64 realtime subsystem"
#endif

#include <dewepxi_load.h>
#include <Rtapi.h>

#include <stdio.h>
#include <time.h>

 // TODO: Change this path to the actual location of the dwpxi_api_x64.rtdll file
#define DWPXI_API_DLL "dwpxi_api_x64.rtdll"

#define NUM_BOARDS 2
#define SAMPLE_RATE "1000" // the sample rate in Hz, the polling callback will be called at the same rate

struct BoardInfo
{
    int index;
    int valid;
    char name[40];
    int num_ai;
    int num_cnt;
    int num_bcnt;
    int num_value_registers;
};

struct ApiTime
{
    int year;
    int day_of_year;
    int seconds;
    int milliseconds;
};

void print_error(int nErrorCode)
{
    if (nErrorCode > 0)
    {
        RtPrintf("Error: %s\n", DeWeErrorConstantToString(nErrorCode));
    }
    else if (nErrorCode < 0)
    {
        RtPrintf("Warning: %s\n", DeWeErrorConstantToString(nErrorCode));
    }
}

#define CheckError(nErrorCode) if (nErrorCode != ERR_NONE) { print_error(nErrorCode); if (nErrorCode > 0) return nErrorCode; }

void retrieve_driver_info()
{
    char version[40] = { 0 };
    char build_date[40] = { 0 };
    DeWeGetParamStruct_str("driver/api", "version", version, sizeof(version));
    DeWeGetParamStruct_str("driver/api", "builddate", build_date, sizeof(build_date));
    RtPrintf("API Version: %s, Date: %s\n", version, build_date);
}

int configure_api(int* num_boards)
{
    int nErrorCode = ERR_NONE;

    *num_boards = 0;

    // Enable thread support (i.e. configure multiple TRION boards in parallel)
    nErrorCode = DeWeSetParamStruct_str("driver/api/config/thread", "enabled", "true");
    CheckError(nErrorCode);

    nErrorCode = DeWeDriverInit(num_boards);
    CheckError(nErrorCode);

    if (*num_boards < 0)
    {
        // In simulations, the number of boards is negative
        *num_boards = -(*num_boards);
    }

    return ERR_NONE;
}

int board_initialize(struct BoardInfo* self, int board_no)
{
    int nErrorCode = ERR_NONE;
    char target[32] = { 0 };
    char buffer[32] = { 0 };

    self->index = board_no;

    snprintf(target, sizeof(target), "BoardID%d", board_no);
    nErrorCode = DeWeGetParamStruct_str(target, "BoardName", self->name, sizeof(self->name));
    CheckError(nErrorCode);

    snprintf(target, sizeof(target), "BoardID%d/AI", board_no);
    nErrorCode = DeWeGetParamStruct_str(target, "Channels", buffer, sizeof(buffer));
    CheckError(nErrorCode);
    self->num_ai = atoi(buffer);

    snprintf(target, sizeof(target), "BoardID%d/CNT", board_no);
    nErrorCode = DeWeGetParamStruct_str(target, "Channels", buffer, sizeof(buffer));
    CheckError(nErrorCode);
    self->num_cnt = atoi(buffer);

    snprintf(target, sizeof(target), "BoardID%d/BoardCNT", board_no);
    nErrorCode = DeWeGetParamStruct_str(target, "Channels", buffer, sizeof(buffer));
    CheckError(nErrorCode);
    self->num_bcnt = atoi(buffer);

    self->valid = TRUE;

    return ERR_NONE;
}

int wait_for_sync(int board_no, int timeout_seconds)
{
    int nErrorCode = ERR_NONE;
    int nTimingState = 0;
    int nNumInterations = 0;

    // issue a resync-command, to force the HW to ressync to the Input
    nErrorCode = DeWeSetParam_i32(board_no, CMD_TIMING_STATE, nTimingState);
    CheckError(board_no);
    RtPrintf("Waiting for GPS receiver to lock and sync...\n");
    do
    {
        ++nNumInterations;
        if (nNumInterations > timeout_seconds)
        {
            nErrorCode = ERR_TIMEOUT;
            break;
        }

        //give 1.0 secs time (any shorter or longer value would do)
        Sleep(1000);

        nErrorCode = DeWeGetParam_i32(board_no, CMD_TIMING_STATE, &nTimingState);
        CheckError(nErrorCode);
        switch (nTimingState)
        {
        case TIMINGSTATE_LOCKED:
            //RtPrintf("TIMINGSTATE_LOCKED\n");
            break;
        case TIMINGSTATE_NOTRESYNCED:
            RtPrintf(" [%d] TIMINGSTATE_NOTRESYNCED\n", nNumInterations);
            break;
        case TIMINGSTATE_UNLOCKED:
            RtPrintf(" [%d] TIMINGSTATE_UNLOCKED\n", nNumInterations);
            break;
        case TIMINGSTATE_LOCKEDOOR:
            RtPrintf(" [%d] TIMINGSTATE_LOCKEDOOR\n", nNumInterations);
            break;
        case TIMINGSTATE_TIMEERROR:
            RtPrintf(" [%d] TIMINGSTATE_TIMEERROR\n", nNumInterations);
            break;
        case TIMINGSTATE_RELOCKOOR:
            RtPrintf(" [%d] TIMINGSTATE_RELOCKOOR\n", nNumInterations);
            break;
        case TIMINGSTATE_NOTIMINGMODE:
            RtPrintf(" [%d] TIMINGSTATE_NOTIMINGMODE\n", nNumInterations);
            break;
        default:
            RtPrintf(" [%d] Unexpected State (TESNO): %d\n", nNumInterations, nTimingState);
            break;
        }
    } while (nErrorCode == ERR_NONE && nTimingState != TIMINGSTATE_LOCKED);

    return nErrorCode;
}

int wait_for_acq(int board_no, int timeout_seconds)
{
    int nErrorCode = ERR_NONE;
    int nAcqState = 0;
    int nNumInterations = 0;

    RtPrintf("Waiting for ACQ running...\n");
    do
    {
        ++nNumInterations;
        if (nNumInterations > 10 * timeout_seconds)
        {
            nErrorCode = ERR_TIMEOUT;
            break;
        }

        Sleep(100);

        nErrorCode = DeWeGetParam_i32(board_no, CMD_ACQ_STATE, &nAcqState);
        CheckError(nErrorCode);
        switch (nAcqState)
        {
        case ACQ_STATE_RUNNING:
            break;
        case ACQ_STATE_IDLE:
            RtPrintf(" [%d] IDLE\n", nNumInterations);
            break;
        case ACQ_STATE_SYNCED:
            RtPrintf(" [%d] SYNCED\n", nNumInterations);
            break;
        case ACQ_STATE_ERROR:
            RtPrintf(" [%d] ERROR\n", nNumInterations);
            break;
        default:
            RtPrintf(" [%d] Unexpected State: %d\n", nNumInterations, nAcqState);
            break;
        }
    } while (nErrorCode == ERR_NONE && nAcqState != ACQ_STATE_RUNNING);

    return nErrorCode;
}

int getRegisterBar1(int board_id, int which)
{
    const int CMD_REG_BAR1 = 0x0502;
    const int command_id = CMD_REG_BAR1 | (which << 16u);

    int raw_value = 0;
    DeWeGetParam_i32(board_id, command_id, &raw_value);

    return raw_value;
}

int getErrorRegister(int board_id, int which)
{
    int raw_val = getRegisterBar1(board_id, which);

    //sign extend 28 bit value
    if (raw_val & 0x08000000)
    {
        raw_val = raw_val | 0xf0000000;
    }
    else
    {
        raw_val = raw_val & 0x0fffffff;
    }

    return raw_val;
}

void poll_oscillator_state(int board_no, int duration_seconds)
{
    int nNumInterations = 0;

    RtPrintf("Polling Oscillator State...\n");
    for(;;)
    {
        ++nNumInterations;
        if (nNumInterations > duration_seconds)
        {
            break;
        }

        // The values have a 1Hz update rate
        // so a query 1 per sec is sufficient
        Sleep(1000);
        
        // check oscillator error once per second
        const int REG_E_PERIOD = 0x00b0;
        const int REG_E_PHASE = 0x00b4;
        int error_period = 0;
        int error_phase = 0;

        error_period = getErrorRegister(board_no, REG_E_PERIOD);
        error_phase = getErrorRegister(board_no, REG_E_PHASE);

        RtPrintf("> oscillator deviation period: '%d' / phase: '%d'\n", error_period, error_phase);
    }
}

int read_apitime(struct ApiTime* out, const char* target)
{
    int nErrorCode = ERR_NONE;
    char buffer[32];

    nErrorCode = DeWeGetParamStruct_str(target, "Year", buffer, sizeof(buffer));
    CheckError(nErrorCode);
    out->year = RtAtoi(buffer);

    nErrorCode = DeWeGetParamStruct_str(target, "Day", buffer, sizeof(buffer));
    CheckError(nErrorCode);
    out->day_of_year = RtAtoi(buffer);

    nErrorCode = DeWeGetParamStruct_str(target, "Sec", buffer, sizeof(buffer));
    CheckError(nErrorCode);
    out->seconds = RtAtoi(buffer);

    nErrorCode = DeWeGetParamStruct_str(target, "SubSec", buffer, sizeof(buffer));
    CheckError(nErrorCode);
    out->milliseconds = RtAtoi(buffer);

    return nErrorCode;
}

int run_example()
{
    int nErrorCode = ERR_NONE;
    struct BoardInfo boards[NUM_BOARDS] = { 0 };
    int master_board = -1;
    int board_no = 0;

    // Iterate over all boards, select the master and valid slaves and configure them
    for (board_no = 0; board_no < NUM_BOARDS; ++board_no)
    {
        char target[64] = { 0 };
        struct BoardInfo* board = boards + board_no;
        nErrorCode = board_initialize(board, board_no);
        CheckError(nErrorCode);

        board->valid = board->num_bcnt > 0; // We will only use boards with board counters

        if (FALSE == board->valid)
        {
            continue;
        }

        RtPrintf("Found board %d: %s with %d AI, %d CNT and %d BCNT channels\n",
            board_no, board->name,
            board->num_ai, board->num_cnt, board->num_bcnt);

        nErrorCode = DeWeSetParam_i32(board_no, CMD_OPEN_BOARD, 0);
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParam_i32(board_no, CMD_RESET_BOARD, 0);
        CheckError(nErrorCode);

        if (0 == strcmp(board->name, "TRION3-CONTROLLER"))
        {
            if (master_board != -1)
            {
                RtPrintf("Multiple CONTROLLER boards found, invalid configuration\n");
                return ERR_BOARD_TYPE_NOT_VALID;
            }

            master_board = board_no;
        }

        // Configure Acquisition
        snprintf(target, sizeof(target), "BoardID%d/AcqProp", board_no);
        nErrorCode = DeWeSetParamStruct_str(target, "ExtClk", "False");
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParamStruct_str(target, "SampleRate", SAMPLE_RATE); // Each sample will generate a call back
        CheckError(nErrorCode);

        if (master_board == board_no)
        {
            nErrorCode = DeWeSetParamStruct_str(target, "OperationMode", "Master");
            CheckError(nErrorCode);
            nErrorCode = DeWeSetParamStruct_str(target, "ExtTrigger", "False");
            CheckError(nErrorCode);

            // Set start counter to sample rate, so we start on exactly one second
            nErrorCode = DeWeSetParamStruct_str(target, "StartCounter", SAMPLE_RATE);
            CheckError(nErrorCode);

            // Configure GPS
            snprintf(target, sizeof(target), "BoardID%d/AcqProp/SyncSettings/SyncIn", board_no);
            nErrorCode = DeWeSetParamStruct_str(target, "Mode", "GPS");
            CheckError(nErrorCode);
        }
        else
        {
            nErrorCode = DeWeSetParamStruct_str(target, "OperationMode", "Slave");
            CheckError(nErrorCode);
            nErrorCode = DeWeSetParamStruct_str(target, "ExtTrigger", "PosEdge");
            CheckError(nErrorCode);
        }

        // Commit settings to the board
        nErrorCode = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ALL, 0);
        CheckError(nErrorCode);

        nErrorCode = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ACQ_TIMING, 0);
        CheckError(nErrorCode);
    }

    if (master_board == -1)
    {
        DeWeSetParam_i32(0, CMD_CLOSE_BOARD_ALL, 0);
        return ERR_BOARD_NOT_MASTER;
    }

    nErrorCode = wait_for_sync(master_board, 30);
    CheckError(nErrorCode);

    RtPrintf("Starting acquisition...\n");
    // Start the slaves
    for (board_no = 0; board_no < NUM_BOARDS; ++board_no)
    {
        if (board_no == master_board)
            continue;

        nErrorCode = DeWeSetParam_i32(board_no, CMD_START_ACQUISITION, 0);
        CheckError(nErrorCode);
    }

    // Start the master
    nErrorCode = DeWeSetParam_i32(master_board, CMD_START_ACQUISITION, 0);
    CheckError(nErrorCode);

    // The following code just demonstrates, that the actual start is delayed
    // until PSS. No need to do that in a real world application
    nErrorCode = wait_for_acq(master_board, 30);
    CheckError(nErrorCode);

    // Read current time
    {
        int dummy = 0;
        char target[64] = { 0 };
        struct ApiTime system_time;
        char acq_start_time[64] = { 0 };
        //dummy won't hold any useful information
        nErrorCode = DeWeGetParam_i32(master_board, CMD_TIMING_TIME, &dummy);
        CheckError(nErrorCode);
        snprintf(target, sizeof(target), "BoardId%d/AcqProp/Timing/SystemTime", master_board);
        nErrorCode = read_apitime(&system_time, target);
        CheckError(nErrorCode);
        RtPrintf("Current system time: %04d/%d %d.%03d\n", system_time.year, system_time.day_of_year, system_time.seconds, system_time.milliseconds);

        // Read the acquisition start time as a UNIX timestamp with subsecond precision (subseconds should be 0 when synchronized to GPS)
        snprintf(target, sizeof(target), "BoardId%d/AcqProp/Timing/AcqStartTime", master_board);
        nErrorCode = DeWeGetParamStruct_str(target, "UnixTimestamp", acq_start_time, sizeof(acq_start_time)); // e.g. 1716980535.210
        CheckError(nErrorCode);

        {
            time_t timestamp = RtAtoi(acq_start_time);
            struct tm* utc_time = gmtime(&timestamp);
            char timestr[80] = { 0 };
            strftime(timestr, sizeof(timestr), "%Y-%m-%d %H:%M:%S", utc_time);
            RtPrintf("Acquisition start UNIX time = %s (UTC: %s)\n", acq_start_time, timestr);
        }
    }

    RtPrintf("Stopping acquisition...\n");
    for (board_no = 0; board_no < NUM_BOARDS; ++board_no)
    {
        nErrorCode = DeWeSetParam_i32(board_no, CMD_STOP_ACQUISITION, 0);
        CheckError(nErrorCode);
    }

    //just trtack the oscillator state for 30 seconds
    poll_oscillator_state(master_board, 30);

    RtPrintf("Closing boards...\n");
    nErrorCode = DeWeSetParam_i32(0, CMD_CLOSE_BOARD_ALL, 0);
    CheckError(nErrorCode);

    return ERR_NONE;
}

int main(int argc, char* argv[])
{
    int nErrorCode = ERR_NONE;
    int nNoOfBoards = 0;
    int revision = DeWePxiLoadByName(DWPXI_API_DLL);
    if (revision == 0)
    {
        RtPrintf("ERROR: Cannot load the DWPXI API DLL: %s\n", DWPXI_API_DLL);
        return 1;
    }

    retrieve_driver_info();

    nErrorCode = configure_api(&nNoOfBoards);
    if (nErrorCode > ERR_NONE)
    {
        goto cleanup;
    }

    if (nNoOfBoards < NUM_BOARDS)
    {
        RtPrintf("Initialized the API and found only %d boards (%d or more required)\n", nNoOfBoards, NUM_BOARDS);
        goto cleanup;
    }

    RtPrintf("Initialized the API successfully and found %d boards\n", nNoOfBoards);

    nErrorCode = run_example();

cleanup:
    DeWePxiUnload();

    RtPrintf("Example finished. Exit code %d\n", nErrorCode);

    return nErrorCode;
}
