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
 *  - Set chassis controller as sync master (board 0)
 *  - Set first board as slave
 *  - Configures the DMA master on board 1 (board 0 does not use DMA at all)
 *  - Performs basic sample counter checks on all board to verify they are in sync
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
#define SAMPLE_RATE 10000 // the sample rate in Hz, the polling callback will be called at the same rate
#define BLOCK_SIZE 10
#define NUM_BLOCKS 3
#define RUNTIME_IN_SECONDS 30
#define NUM_ITERATIONS (1000*RUNTIME_IN_SECONDS)

#define DMA_MASTER_BOARD "1"                 // use first TRION board, not Chassis-Controller to send DMA interrupts
#define COMBINE_DMA_START_INTERRUPT TRUE     // if TRUE, use shared IST for starting DMA
#define COMBINE_DMA_FINISHED_INTERRUPTS TRUE // if TRUE, use shared IST for DMA finished handling
#define USE_CHASSIS_CONTROLLER TRUE

#define QUIT_ON_NUM_ERRORS 5 // do not quit when 0, otherwise quit when number of errors has reached this value

// Define custom warnings
#define WARNING_BOARDCNT_UNEXPECTED_VALUE -1000000
#define WARNING_ACT_SAMPLE_NON_MONOTONOUS -1000001
#define WARNING_ACT_SAMPLE_OUT_OF_SYNC    -1000002

#define STR_HELPER(x) #x
#define STR(x) STR_HELPER(x)

struct BoardInfo
{
    int index;
    int valid;
    char name[40];
    int num_ai;
    int num_cnt;
    int num_bcnt;
    int num_value_registers;
    uint64_t total_samples;
    int uses_dma_transfers;
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
    char buffer[32];

    *num_boards = 0;

    RtPrintf("API Configuration:\n");

    // Enable thread support (i.e. configure multiple TRION boards in parallel)
    nErrorCode = DeWeSetParamStruct_str("driver/api/config/thread", "enabled", "true");
    CheckError(nErrorCode);

#if COMBINE_DMA_START_INTERRUPT
    // Set interrupt affinity to one CPU
    snprintf(buffer, sizeof(buffer), "%d", 0xF0); // 1111 0000
    nErrorCode = DeWeSetParamStruct_str("driver/api/config/thread", "IrqAffinity", buffer);
    CheckError(nErrorCode);

    // Set board DMA_MASTER_BOARD as the master board for interrupt handling (only the master board will now emit interrupts)
    nErrorCode = DeWeSetParamStruct_str("driver/api/config/thread", "masterboard", DMA_MASTER_BOARD);
    CheckError(nErrorCode);
    RtPrintf(" - Board " DMA_MASTER_BOARD " will trigger all DMA transfers (shared IST)\n");
#else
    RtPrintf(" - All board will trigger their own DMA transfers (separate ISTs)\n");
#endif

#if COMBINE_DMA_START_INTERRUPT && COMBINE_DMA_FINISHED_INTERRUPTS
    // Combine "DMA finished" interrupts and process them only on the previously set master board
    nErrorCode = DeWeSetParamStruct_str("driver/api/config/thread", "CombineDmaInterrupts", "true");
    CheckError(nErrorCode);
    RtPrintf(" - Board " DMA_MASTER_BOARD " will finish DMA transfers for all boards (shared IST)\n");
#else
    RtPrintf(" - All boards will finish their own DMA transfers (separate ISTs)\n");
#endif

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
        if (nNumInterations > 1000 * timeout_seconds)
        {
            nErrorCode = ERR_TIMEOUT;
            break;
        }

        Sleep(1);

        nErrorCode = DeWeGetParam_i32(board_no, CMD_ACQ_STATE, &nAcqState);
        CheckError(nErrorCode);
        if (nNumInterations % 100 == 0)
        {
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
        }
    } while (nErrorCode == ERR_NONE && nAcqState != ACQ_STATE_RUNNING);

    return nErrorCode;
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

int read_acquisition_start_time(int master_board)
{
    int dummy = 0;
    char target[64] = { 0 };
    struct ApiTime system_time;
    char acq_start_time[64] = { 0 };
    int nErrorCode = 0;

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

    return nErrorCode;
}

static int verify_act_sample_count(struct BoardInfo* boards, int num_boards)
{
    sint64 min_sc, max_sc, last_sc;
    int min_idx, max_idx;
    LARGE_INTEGER start_time, stop_time;
    LARGE_INTEGER freq;
    int first_board = -1;

    if (num_boards == 0)
    {
        return ERR_NONE;
    }

    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&start_time);

    for (int n = 0; n < num_boards; ++n)
    {
        sint64 board_cnt;
        int err;

        if (!boards[n].valid)
        {
            continue;
        }

        err = DeWeGetParam_i64(boards[n].index, CMD_ACT_SAMPLE_COUNT, &board_cnt);
        CheckError(err);

        if (first_board < 0)
        {
            min_sc = board_cnt;
            max_sc = board_cnt;
            last_sc = board_cnt;
            min_idx = boards[n].index;
            max_idx = boards[n].index;
            first_board = n;
        }
        else
        {
            if (board_cnt < min_sc)
            {
                min_sc = board_cnt;
                min_idx = boards[n].index;
            }
            if (board_cnt > max_sc)
            {
                max_sc = board_cnt;
                max_idx = boards[n].index;
            }
            if (board_cnt < last_sc)
            {
                // we expect the current board to have at least the same sample count than the one read before
                return WARNING_ACT_SAMPLE_NON_MONOTONOUS;
            }
        }
    }

    QueryPerformanceCounter(&stop_time);
    int readout_time_us = (1000000 * (stop_time.QuadPart - start_time.QuadPart)) / freq.QuadPart;
    int drift_samples = static_cast<int>(max_sc - min_sc);
    int drift_us = (drift_samples * 1000000) / SAMPLE_RATE;

    // The time time read all samples must be larger or equal than the time elapsed according to the boards
    // otherwise, this indicates that at least one board has drifted away temporally
    if (first_board >= 0 && drift_us > readout_time_us + (1000000/SAMPLE_RATE))
    {
        RtPrintf("Sample drift larger than expected: min = %6u [%d], max = %6u [%d] [%d: %d us in %d us] (total received: %6u)\n",
            min_sc, min_idx, max_sc, max_idx,
            drift_samples, drift_us, readout_time_us,
            boards[first_board].total_samples);
        return WARNING_ACT_SAMPLE_OUT_OF_SYNC;
    }

    return ERR_NONE;
}

int acquisition_loop(struct BoardInfo* boards, int num_boards, size_t num_runs)
{
    int err = ERR_NONE;
    size_t num_errors = 0;
    size_t num_it;
    int n;
    for (num_it = 0; num_it < num_runs; ++num_it)
    {
        for (n = 0; n < num_boards; ++n)
        {
            int samples_available = 0;
            void* data = NULL;
            struct BoardInfo* board = boards + n;

            if (!board->valid)
            {
                continue;
            }

            if (board->uses_dma_transfers == FALSE)
            {
                continue;
            }

            err = DeWeGetParam_i32(board->index, CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLE, &samples_available);
            CheckError(err);

            if (samples_available != BLOCK_SIZE)
            {
                RtPrintf("%8u> ERR: Board %d returned %d blocks, real-time violated\n", (uint32)num_it, board->index, samples_available / BLOCK_SIZE);
                ++num_errors;
            }

            err = DeWeGetParam_i64(board->index, CMD_BUFFER_0_ACT_SAMPLE_POS, (sint64*)&data);
            CheckError(err);

            // here you can process samples in the buffer at <data>

            err = DeWeSetParam_i32(board->index, CMD_BUFFER_0_FREE_NO_SAMPLE, samples_available);
            CheckError(err);

            board->total_samples += samples_available;
        }

        /**
         * Check that all boards are synchronized by reading their ACT_SAMPLE_COUNT register sequentially and comparing them
         */
        err = verify_act_sample_count(boards, num_boards);
        switch (err)
        {
        case WARNING_ACT_SAMPLE_NON_MONOTONOUS:
            RtPrintf("%8u> ERR: Act sample counts are not monotonously increasing\n", (uint32)num_it);
            ++num_errors;
            break;
        case WARNING_ACT_SAMPLE_OUT_OF_SYNC:
            RtPrintf("%8u> ERR: Act sample count out of sync, boards are not synchronized correctly\n", (uint32)num_it);
            ++num_errors;
            break;
        default:
            CheckError(err);
            break;
        }

        if (QUIT_ON_NUM_ERRORS > 0 && num_errors >= QUIT_ON_NUM_ERRORS)
        {
            break;
        }
        if (num_it % (1000 * 10) == 0)
        {
            // print progress after each 10 seconds
            RtPrintf("%8u> running...\n", (uint32)num_it);
        }
    }

    RtPrintf("\n");
    RtPrintf("*****************************\n");
    RtPrintf("Finished with %u errors\n", num_errors);
    RtPrintf("*****************************\n");
    return ERR_NONE;
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
            board->uses_dma_transfers = FALSE;
        }
        else
        {
            board->uses_dma_transfers = TRUE;
        }

        if (FALSE && board->uses_dma_transfers) // currently deactivated
        {
            // enable board counter and have it report the current sample number
            snprintf(target, sizeof(target), "BoardID%d/BoardCNT0", board_no);
            nErrorCode = DeWeSetParamStruct_str(target, "Used", "True");
            CheckError(nErrorCode);
            nErrorCode = DeWeSetParamStruct_str(target, "Reset", "OnReStart");
            CheckError(nErrorCode);
            nErrorCode = DeWeSetParamStruct_str(target, "Source_A", "ACQ_CLK");
            CheckError(nErrorCode);
        }

        // Configure Acquisition
        snprintf(target, sizeof(target), "BoardID%d/AcqProp", board_no);
        nErrorCode = DeWeSetParamStruct_str(target, "ExtClk", "False");
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParamStruct_str(target, "SampleRate", STR(SAMPLE_RATE)); // Each sample will generate a call back
        CheckError(nErrorCode);

        if (master_board == board_no)
        {
            nErrorCode = DeWeSetParamStruct_str(target, "OperationMode", "Master");
            CheckError(nErrorCode);
            nErrorCode = DeWeSetParamStruct_str(target, "ExtTrigger", "False");
            CheckError(nErrorCode);

            // Set start counter to sample rate, so we start on exactly one second
            nErrorCode = DeWeSetParamStruct_str(target, "StartCounter", STR(SAMPLE_RATE));
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

        // Setup DMA transfer dimensions
        nErrorCode = DeWeSetParam_i32(board_no, CMD_BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParam_i32(board_no, CMD_BUFFER_0_BLOCK_COUNT, NUM_BLOCKS);
        CheckError(nErrorCode);

        // Commit settings to the board
        nErrorCode = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ALL, 0);
        CheckError(nErrorCode);

        nErrorCode = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ACQ_TIMING, 0);
        CheckError(nErrorCode);
    }

    if (master_board == -1)
    {
        DeWeSetParam_i32(0, CMD_CLOSE_BOARD_ALL, 0);
        RtPrintf("No CONTROLLER board found, invalid hardware configuration\n");
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
    nErrorCode = read_acquisition_start_time(master_board);
    CheckError(nErrorCode);

    // acquisition loop
    nErrorCode = acquisition_loop(boards, NUM_BOARDS, NUM_ITERATIONS);
    CheckError(nErrorCode);

    RtPrintf("Stopping acquisition...\n");
    for (board_no = 0; board_no < NUM_BOARDS; ++board_no)
    {
        nErrorCode = DeWeSetParam_i32(board_no, CMD_STOP_ACQUISITION, 0);
        CheckError(nErrorCode);
    }

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
