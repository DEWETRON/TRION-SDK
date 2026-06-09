/**
 * This program will start multiple boards in DMA mode and evaluate if they run correctly
 *
 * This example can be used with up to 16 TRION3-1820-MULTI-8 boards and a TRION3-CONTROLLER
 *
 *
 * Describes the following:
 *  - Configure the API for multi-threaded initialization with thread affinity
 *  - Setup of 8 AI channels and board counter per board
 *  - Set first board (CHASSIS CONTROLLER) as master
 *  - Set remaining boards as slaves
 *  - Set sample rate to 200000 Samples/second on each board
 *  - Acquisition configuration is done via XML
 *  - Check every board counter value for validity
 *  - Check if board counter across all boards run synchronously
 *  - Generate RTX profiling events for each detected error for offline analysis
 */

#ifndef UNDER_RTSS
#error "This example must be compiled for the RTX64 realtime subsystem"
#endif

#include <dewepxi_load.h>
#include <Rtapi.h>
#include <RtssApi.h>

#include <stdio.h>
#include <time.h>

#define STR_HELPER(x) #x
#define STR(x) STR_HELPER(x)

// TODO: Change this path to the actual location of the dwpxi_api_x64.rtdll file
#define DWPXI_API_DLL "dwpxi_api_x64.rtdll"

#define MAX_BOARDS 17
#define SAMPLE_RATE 200000 // Sample rate of the boards in Hz
#define BLOCK_SIZE  200    // Block-Rate 1kHz
#define NUM_BLOCKS  100    // Allocate 100 Block ring-buffer
#define STARTCNT    20000

//#define RUNTIME_IN_SECONDS (60*60*24) // Run for 1 day
#define RUNTIME_IN_SECONDS (60*60*24*7) // Run for 7 days

#define US_PER_ITERATION ((1000000ll * BLOCK_SIZE) / SAMPLE_RATE)
#define NUM_ITERATIONS ((RUNTIME_IN_SECONDS*1000000ll)/US_PER_ITERATION)
#define NUM_MEASUREMENTS 1

// Tune these values to enable shared IST handling
#define DMA_MASTER_BOARD 1
#define COMBINE_DMA_START_INTERRUPT TRUE     // if TRUE, use shared IST for starting DMA
#define COMBINE_DMA_FINISHED_INTERRUPTS TRUE // if TRUE, use shared IST for DMA finished handling
#define USE_CHASSIS_CONTROLLER FALSE         // if TRUE, the chassis controller is started alongside the boards
#define QUIT_ON_NUM_ERRORS 9999 // do not quit when 0, otherwise quit when number of errors has reached this value
#define QUIT_DELAY 10 // quit on error condition after this many iterations

// Define custom warnings
#define WARNING_BOARDCNT_UNEXPECTED_VALUE -1000000
#define WARNING_ACT_SAMPLE_NON_MONOTONOUS -1000001
#define WARNING_ACT_SAMPLE_OUT_OF_SYNC    -1000002

//I32 Register Access (Bar0/Bar1)
#define CMD_REG_BAR0                         0x0501 // R/W (Used as: CMD_REG_BAR0 | (Address << 16 ) )
#define CMD_REG_BAR1                         0x0502 // R/W (Used as: CMD_REG_BAR1 | (Address << 16 ) )
#define CMD_REG_BAR2                         0x0503 // R/W (Used as: CMD_REG_BAR1 | (Address << 16 ) )

#define REG_RTC_SEC  0x0C00
#define REG_RTC_NSEC 0x0C08
#define REG_START_SEC  0x0C10
#define REG_START_NSEC 0x0C18

#define REG_FIRMWARE_VERSION 4

static LARGE_INTEGER s_perf_freq;

struct JitterAnalysis
{
    uint64_t max_jitter;
    uint64_t sum_jitter;
    uint64_t num_blocks;
};

static struct JitterAnalysis s_jitter;
void resetJitter()
{
    memset(&s_jitter, 0, sizeof(s_jitter));
}

void printLocalTime(bool print_cr = false)
{
    char buffer[32];
    auto current_time = time(NULL);
    auto local_time = localtime(&current_time);
    asctime_s(buffer, sizeof(buffer), local_time);
    if (print_cr)
    {
        RtPrintf("%s\n", buffer);
    }
    else
    {
        RtPrintf(buffer);
    }
}

void printJitter()
{
    if (s_jitter.num_blocks == 0)
    {
        return;
    }
    RtPrintf("Jitter analysis:\n");
    RtPrintf(" - Avg jitter: %7u us\n", (uint32_t)(s_jitter.sum_jitter / s_jitter.num_blocks));
    RtPrintf(" - Max jitter: %7u us\n", (uint32_t)(s_jitter.max_jitter));
}

void addJitter(int delta_us)
{
    uint64_t jitter = abs(delta_us - US_PER_ITERATION);
    s_jitter.max_jitter = max(s_jitter.max_jitter, jitter);
    s_jitter.sum_jitter += jitter;
    s_jitter.num_blocks++;
}

struct BoardInfo
{
    int index;
    int valid;
    int firmware_version;
    char name[40];
    int num_ai;
    int num_cnt;
    int num_bcnt;
    int32_t scanline_size;
    int64_t total_samples;
    char* buffer_start;
    char* buffer_end;
    LARGE_INTEGER last_sample_time;
};

void resetBordInfoMeasData(BoardInfo* board_info)
{
    board_info->last_sample_time.QuadPart = 0;
    board_info->total_samples = 0;
}

void print_error(int err, int line)
{
    if (err > 0)
    {
        RtPrintf("line %d - Error: %s\n", line, DeWeErrorConstantToString(err));
    }
    else if (err < 0)
    {
        RtPrintf("line %d - Warning: %s\n", line, DeWeErrorConstantToString(err));
    }
}

#define CheckError(err) if (err != ERR_NONE) { print_error(err, __LINE__); if (err > 0) return err; }
#define CheckErrorRelaxed(err) if (err != ERR_NONE) { print_error(err, __LINE__); if (err > 0) return err; }

int board_initialize(struct BoardInfo* self, int board_no)
{
    int err = ERR_NONE;
    char target[32] = { 0 };
    char buffer[32] = { 0 };

    self->index = board_no;

    err = DeWeGetParam_i32(board_no, CMD_REG_BAR1 | (REG_FIRMWARE_VERSION << 16), &self->firmware_version);
    CheckError(err);

    snprintf(target, sizeof(target), "BoardID%d", board_no);
    err = DeWeGetParamStruct_str(target, "BoardName", self->name, sizeof(self->name));
    CheckError(err);

    snprintf(target, sizeof(target), "BoardID%d/AI", board_no);
    err = DeWeGetParamStruct_str(target, "Channels", buffer, sizeof(buffer));
    CheckError(err);
    self->num_ai = atoi(buffer);

    snprintf(target, sizeof(target), "BoardID%d/CNT", board_no);
    err = DeWeGetParamStruct_str(target, "Channels", buffer, sizeof(buffer));
    CheckError(err);
    self->num_cnt = atoi(buffer);

    snprintf(target, sizeof(target), "BoardID%d/BoardCNT", board_no);
    err = DeWeGetParamStruct_str(target, "Channels", buffer, sizeof(buffer));
    CheckError(err);
    self->num_bcnt = atoi(buffer);

    self->valid = TRUE;

    return ERR_NONE;
}

void retrieve_driver_info()
{
    char version[40] = {0};
    char build_date[40] = {0};
    DeWeGetParamStruct_str("driver/api", "version", version, sizeof(version));
    DeWeGetParamStruct_str("driver/api", "builddate", build_date, sizeof(build_date));
    RtPrintf("API Version: %s, Date: %s\n", version, build_date);
}

int configure_api(int* num_boards)
{
    int err = ERR_NONE;
    char buffer[32];

    *num_boards = 0;

    RtPrintf("API Configuration:\n");

    // Enable thread support (i.e. configure multiple TRION boards in parallel)
    err = DeWeSetParamStruct_str("driver/api/config/thread", "enabled", "true");
    CheckError(err);

    // Set thread affinity to four CPUs (uses during multi-threaded initialization)
    snprintf(buffer, sizeof(buffer), "%d", 0xF0); // 1111 0000
    err = DeWeSetParamStruct_str("driver/api/config/thread", "Affinity", buffer);
    CheckError(err);

#if COMBINE_DMA_START_INTERRUPT
    // Set interrupt affinity to one CPU
    snprintf(buffer, sizeof(buffer), "%d", 0xF0); // 1111 0000
    err = DeWeSetParamStruct_str("driver/api/config/thread", "IrqAffinity", buffer);
    CheckError(err);

    // Set board 0 as the master board for interrupt handling (only the master board will now emit interrupts)
    err = DeWeSetParamStruct_str("driver/api/config/thread", "masterboard", STR(DMA_MASTER_BOARD));
    CheckError(err);
    RtPrintf(" - Board " STR(DMA_MASTER_BOARD) " will trigger all DMA transfers (shared IST)\n");
#else
    RtPrintf(" - All board will trigger their own DMA transfers (separate ISTs)\n");
#endif

#if COMBINE_DMA_START_INTERRUPT && COMBINE_DMA_FINISHED_INTERRUPTS
    // Combine "DMA finished" interrupts and process them only on the previously set master board
    err = DeWeSetParamStruct_str("driver/api/config/thread", "CombineDmaInterrupts", "true");
    CheckError(err);
    RtPrintf(" - Board " STR(DMA_MASTER_BOARD) " will finish DMA transfers for all boards (shared IST)\n");
#else
    RtPrintf(" - All boards will finish their own DMA transfers (separate ISTs)\n");
#endif
    RtPrintf("\n");

    err = DeWeDriverInit(num_boards);
    CheckError(err);

    if (*num_boards < 0)
    {
        // In simulations, the number of boards is negative
        *num_boards = -(*num_boards);
    }

    return ERR_NONE;
}

int print_rtc(int board_no)
{
    uint32 sec1, sec2;
    uint32 nsec1, nsec2;
    int err;
    err = DeWeGetParam_i32(board_no, CMD_REG_BAR1 | (REG_RTC_SEC << 16), (sint32*)&sec1); // latch nsec and return previous value of sec
    err = DeWeGetParam_i32(board_no, CMD_REG_BAR1 | (REG_RTC_NSEC << 16), (sint32*)&nsec1); // read previously latched nsec
    err = DeWeGetParam_i32(board_no, CMD_REG_BAR1 | (REG_RTC_SEC << 16), (sint32*)&sec2); // latch nsec and return previous value of sec
    err = DeWeGetParam_i32(board_no, CMD_REG_BAR1 | (REG_RTC_NSEC << 16), (sint32*)&nsec2); // latch nsec and return previous value of sec

    if (nsec2 < nsec1 && sec1 == sec2)
    {
        ++sec2;
    }

    CheckError(err);
    RtPrintf("Board%d-RTC: %u.%09u\n", board_no, sec2, nsec2);

    return ERR_NONE;
}

// dummy buffer to simulate converting samples to 32bit
volatile static int sample_buffer[8];

static int verify_block_boardcnt(struct BoardInfo* board, const void* data, int num_samples)
{
    if (board->scanline_size < sizeof(uint32_t))
    {
        return ERR_BUFFER_NO_AVAIL_DATA;
    }

    // the board counter is always the last element in the scanline (production code should interpret the scanline descriptor)
    const char* board_cnt_ptr = (const char*)data + board->scanline_size - sizeof(uint32_t);
    const char* data_ptr = (const char*)data;
    for (int n = 0; n < num_samples; ++n)
    {
        if (board->num_ai > 0)
        {
            for (int ai = 0; ai < board->num_ai; ++ai)
            {
                const char* s_ptr = data_ptr + 3 * ai;
                int sample = (s_ptr[0] << 8) | (s_ptr[1] << 16) | (s_ptr[2] << 24);
                sample_buffer[ai] = sample;
            }
        }

        uint32_t board_cnt = *reinterpret_cast<const uint32_t*>(board_cnt_ptr);

        // Note: Due to the way TRION asynchroneously resets the Board-Counters, we may get the value 0 twice after acquisition start
        // the following computation is needed to determine the expected board count value for each sample in case of two 0-counts after ACQ start
        int64_t corrected_count = board->total_samples + n - 1; // board counts are shifted by 1
        if (corrected_count == -1)
        {
            corrected_count = 0; // [0, 0, 1, 2, 3, ...] order on first block
        }
        if (board_cnt != (uint32_t)(board->total_samples + n) && board_cnt != (uint32_t)(corrected_count))
        {
            return WARNING_BOARDCNT_UNEXPECTED_VALUE;
        }

        // advance data pointer to next scanline and correct for buffer wrap-around
        board_cnt_ptr += board->scanline_size;
        data_ptr += board->scanline_size;
        if (board_cnt_ptr >= board->buffer_end)
        {
            board_cnt_ptr = board_cnt_ptr - board->buffer_end + board->buffer_start;
            data_ptr = board->buffer_start;
        }
    }
    return ERR_NONE;
}

static int verify_act_sample_count(struct BoardInfo* boards, int num_boards)
{
    int64_t min_sc = 0, max_sc = 0, last_sc;
    int min_idx, max_idx;
    LARGE_INTEGER start_time, stop_time;
    int first_board = -1;

    if (num_boards == 0)
    {
        return ERR_NONE;
    }

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
    int readout_time_ns = (int)((1000000000ll * (stop_time.QuadPart - start_time.QuadPart)) / s_perf_freq.QuadPart);
    int drift_samples = (int)(max_sc - min_sc);
    int drift_ns = (int)((drift_samples * 1000000000ll) / SAMPLE_RATE);
    int max_drift_ns = (int)(((drift_samples > 0 ? (drift_samples - 1) : 0) * 1000000000ll) / SAMPLE_RATE); // expect a jitter up to one sample due to where we start reading

    // The time time read all samples must be larger or equal than the time elapesed accoring to the boards
    // otherwise, this indicates that at least one board has drifted away temporally
    if (first_board >= 0 && max_drift_ns > readout_time_ns)
    {
        RtPrintf("Sample drift larger than expected: min = %6u [%d], max = %6u [%d] [%d: %d ns in %d us] (total received: %6u)\n",
            min_sc, min_idx, max_sc, max_idx,
            drift_samples, drift_ns, readout_time_ns,
            boards[first_board].total_samples);
        return WARNING_ACT_SAMPLE_OUT_OF_SYNC;
    }

    return ERR_NONE;
}

int acquisition_loop(struct BoardInfo* boards, int num_boards, size_t num_runs, size_t* num_errors)
{
    int err = ERR_NONE;
    size_t num_it;
    int break_countdown = -1;

    for (num_it = 0; num_it < num_runs; ++num_it)
    {
        int n;
        BOOL has_printed_block = FALSE;
        BOOL has_printed_jitter = FALSE;
        for (n = 0; n < num_boards; ++n)
        {
            int samples_available = 0;
            void* data = NULL;
            struct BoardInfo* board = boards + n;
            LARGE_INTEGER current_time;
            BOOL has_error = FALSE;

            if (!board->valid)
            {
                continue;
            }

            err = DeWeGetParam_i32(board->index, CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLE, &samples_available);
            CheckError(err);

            QueryPerformanceCounter(&current_time);

            if (samples_available != BLOCK_SIZE)
            {
                RtGenerateEvent(1, &n, 1);
                if (!has_printed_block)
                {
                    printLocalTime();
                    RtPrintf("%8u> ERR: Board %d returned %d blocks, real-time violated\n", (uint32)num_it, board->index, samples_available / BLOCK_SIZE);
                    has_printed_block = TRUE;
                }
                (*num_errors)++;
                has_error = TRUE;
            }

            if (board->last_sample_time.QuadPart > 0)
            {
                int delta_us = (1000000ll * (current_time.QuadPart - board->last_sample_time.QuadPart)) / s_perf_freq.QuadPart;

                addJitter(delta_us);

                if (delta_us - US_PER_ITERATION > US_PER_ITERATION/2)
                {
                    RtGenerateEvent(2, &delta_us, 4);
                    if (!has_printed_jitter)
                    {
                        printLocalTime();
                        RtPrintf("%8u> WRN: Board %d jitter of %d us\n", (uint32)num_it, board->index, delta_us);
                        has_printed_jitter = TRUE;
                    }
                    // disable in normal runs
                    if (!has_error) (*num_errors)++;
                    has_error = TRUE;
                }
            }
            board->last_sample_time = current_time;

            err = DeWeGetParam_i64(board->index, CMD_BUFFER_0_ACT_SAMPLE_POS, (sint64*)&data);
            CheckError(err);

            /**
             * Check if all board counter values of the current board contain the expected values (must be equal to the sample count since start)
             */
            err = verify_block_boardcnt(board, data, samples_available);
            switch (err)
            {
            case WARNING_BOARDCNT_UNEXPECTED_VALUE:
                RtGenerateEvent(2, &n, 1);
                RtPrintf("%8u> ERR: Board-Counter of board %d has an unexpected value\n", (uint32)num_it, board->index);
                if (!has_error) (*num_errors)++;
                has_error = TRUE;
                break;
            default:
                CheckError(err);
                break;
            }

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
            RtGenerateEvent(10, &n, 1);
            RtPrintf("%8u> ERR: Act sample counts are not monotonously increasing\n", (uint32)num_it);
            (*num_errors)++;
            break;
        case WARNING_ACT_SAMPLE_OUT_OF_SYNC:
            RtGenerateEvent(11, &n, 1);
            RtPrintf("%8u> ERR: Act sample count out of sync, boards are not synchronized correctly\n", (uint32)num_it);
            (*num_errors)++;
            break;
        default:
            CheckError(err);
            break;
        }

        if (break_countdown > 0)
        {
            if (--break_countdown == 0)
            {
                break;
            }
        }
        else
        {
            if (QUIT_ON_NUM_ERRORS > 0 && *num_errors >= QUIT_ON_NUM_ERRORS)
            {
                break_countdown = QUIT_DELAY;
                //break;
            }
        }

        if (num_it % (1000 * 60) == 0)
        {
            // print progress after each 60 seconds
            RtPrintf("%8u> running...\n", (uint32)num_it);
        }
        if (num_it % (1000 * 60 * 10) == 0)
        {
            printJitter();
            resetJitter();
        }
    }

    return ERR_NONE;
}

int perform_stability_evaluation(int num_boards, size_t num_iterations, size_t num_measurements)
{
    int err = ERR_NONE;
    struct BoardInfo boards[MAX_BOARDS] = {0};
    int master_board = -1;
    int board_no = 0;
    int num_valid_boards = 0;
    int measurement_counter = 0;

    // open and reset all boards in the system
    err = DeWeSetParam_i32(board_no, CMD_OPEN_BOARD_ALL, 0);
    CheckError(err);
    err = DeWeSetParam_i32(board_no, CMD_RESET_BOARD_ALL, 0);
    CheckError(err);

    // Iterate over all boards, select the master and valid slaves and configure them
    for (board_no = 0; board_no < num_boards; ++board_no)
    {
        char target[32] = { 0 };
        char buffer[32] = { 0 };
        struct BoardInfo* board = boards + board_no;
        err = board_initialize(board, board_no);
        CheckError(err);

#if USE_CHASSIS_CONTROLLER
        board->valid = board->num_bcnt > 0; // We will only use boards with board counters
#else
        board->valid = board->num_ai > 0 && board->num_bcnt > 0; // We will only use boards with board counters
#endif

        if (!board->valid)
        {
            RtPrintf("Skipping board %d: %s (FW %x) with %d AI, %d CNT and %d BCNT channels\n",
                board_no, board->name, board->firmware_version,
                board->num_ai, board->num_cnt, board->num_bcnt);
            continue;
        }

        ++num_valid_boards;
        RtPrintf("Using board %d:    %s (FW %x) with %d AI, %d CNT and %d BCNT channels\n",
            board_no, board->name, board->firmware_version,
            board->num_ai, board->num_cnt, board->num_bcnt);

        if (master_board < 0)
        {
            // define first board as SYNC master
            master_board = board_no;
        }

        // Configure acquisition via XML
        const char* xml_config;
        if (board_no == master_board)
        {
            xml_config =
                "<Configuration>"
                    "<Acquisition><AcqProp>"
                        "<SampleRate Unit = \"Hz\">" STR(SAMPLE_RATE) "</SampleRate>"
                        "<OperationMode>Master</OperationMode>"
                        "<ExtTrigger>False</ExtTrigger>"
                        "<ExtClk>False</ExtClk>"
                        "<StartCounter>" STR(STARTCNT) "</StartCounter>"
                    "</AcqProp></Acquisition>"
                    "<Channel>"
                    "</Channel>"
                "</Configuration>";
        }
        else
        {
            xml_config =
                "<Configuration>"
                    "<Acquisition><AcqProp>"
                        "<SampleRate Unit = \"Hz\">" STR(SAMPLE_RATE) "</SampleRate>"
                        "<OperationMode>Slave</OperationMode>"
                        "<ExtTrigger>PosEdge</ExtTrigger>"
                        "<ExtClk>False</ExtClk>"
                    "</AcqProp></Acquisition>"
                    "<Channel>"
                    "</Channel>"
                "</Configuration>";
        }
        snprintf(target, sizeof(target), "BoardID%d", board_no);
        err = DeWeSetParamStruct_str(target, "config", xml_config);
        CheckError(err);

        // Configure channels
        // enable all AI channels (unused in this demo, but they show realistic read load on the PCI bus)
        snprintf(target, sizeof(target), "BoardID%d/AIAll", board_no);
        err = DeWeSetParamStruct_str(target, "Used", "True");
        CheckError(err);

        // enable board counter and have it report the current sample number
        snprintf(target, sizeof(target), "BoardID%d/BoardCNT0", board_no);
        err = DeWeSetParamStruct_str(target, "Used", "True");
        CheckError(err);
        err = DeWeSetParamStruct_str(target, "Reset", "OnReStart");
        CheckError(err);
        err = DeWeSetParamStruct_str(target, "Source_A", "ACQ_CLK");
        CheckError(err);

        // Setup DMA transfer dimensions
        err = DeWeSetParam_i32(board_no, CMD_BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
        CheckError(err);
        err = DeWeSetParam_i32(board_no, CMD_BUFFER_0_BLOCK_COUNT, NUM_BLOCKS);
        CheckError(err);

        // Commit settings to the board
        err = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ALL, 0);
        CheckError(err);

        // read back the scanline size
        err = DeWeGetParam_i32(board_no, CMD_BUFFER_0_ONE_SCAN_SIZE, &board->scanline_size);
        CheckError(err);

        // read buffer address range
        err = DeWeGetParam_i64(board_no, CMD_BUFFER_0_START_POINTER, (sint64*)&board->buffer_start);
        CheckError(err);
        err = DeWeGetParam_i64(board_no, CMD_BUFFER_0_END_POINTER, (sint64*)&board->buffer_end);
        CheckError(err);
    }

    if (num_valid_boards == 0)
    {
        RtPrintf("No valid boards found. Quitting.\n");
        return ERR_NONE;
    }

    //RtMonitorChangeState(MONITOR_CONTROL_START); // start monitoring to capture potential errors in the trace
    for (measurement_counter = 0; measurement_counter < num_measurements; ++measurement_counter)
    {
        for (board_no = 0; board_no < num_boards; ++board_no)
        {
            struct BoardInfo* board = boards + board_no;
            resetBordInfoMeasData(board);
        }
        resetJitter();

        RtPrintf("===================================\n");
        RtPrintf("Starting measurement num %d / %d...\n", measurement_counter + 1, num_measurements);

        // Start slaves first
        if (num_valid_boards > 1)
        {
            RtPrintf("Starting %d slaves...\n", num_valid_boards);
            for (board_no = 0; board_no < num_boards; ++board_no)
            {
                struct BoardInfo* board = boards + board_no;
                if (board->valid && board_no != master_board && board_no != DMA_MASTER_BOARD)
                {
                    int samples_available;
                    err = DeWeSetParam_i32(board_no, CMD_START_ACQUISITION, 0);
                    CheckError(err);

                    err = DeWeGetParam_i32(board->index, CMD_BUFFER_0_AVAIL_NO_SAMPLE, &samples_available);
                    CheckErrorRelaxed(err);
                }
            }
        }

        // Start the master
        RtPrintf("Starting DMA master (board %i)...\n", DMA_MASTER_BOARD);
        err = DeWeSetParam_i32(DMA_MASTER_BOARD, CMD_START_ACQUISITION, 0);
        CheckError(err);

        if (master_board != DMA_MASTER_BOARD)
        {
            RtPrintf("Starting master (board %i)...\n", master_board);
            err = DeWeSetParam_i32(master_board, CMD_START_ACQUISITION, 0);
            CheckError(err);
        }

        {
            char target[64];
            char acq_start_time[64];
            snprintf(target, sizeof(target), "BoardId%d/AcqProp/Timing/AcqStartTime", 0);
            err = DeWeGetParamStruct_str(target, "UnixTimestamp", acq_start_time, sizeof(acq_start_time)); // e.g. 1716980535.210
            CheckError(err);
            RtPrintf("Acquisition start time: %s\n", acq_start_time);

            // TODO: Read RTC clock directly?
        }
        // start time
        print_rtc(0);

        size_t num_errors = 0;
        RtMonitorChangeState(MONITOR_CONTROL_START); // start monitoring to capture potential errors in the trace
        err = acquisition_loop(boards, num_boards, num_iterations, &num_errors);
        RtMonitorChangeState(MONITOR_CONTROL_STOP);
        CheckError(err);

        RtPrintf("Stopping acquisition...\n");

        //RtMonitorChangeState(MONITOR_CONTROL_START); // start monitoring to capture potential errors in the trace
        // Create a well defined stop order
        int stop_order[MAX_BOARDS];
        memset(stop_order, -1, sizeof(stop_order));
        {
            int idx = 0;
#define STOP_DMA_FIRST
//#define STOP_SYNC_FIRST
//#define STOP_SLAVE_FIRST

#ifdef STOP_DMA_FIRST
            // first, stop the DMA master board (kill the only board that actively handles DMAs)
            // then, stop all other boards in arbitrary order
            stop_order[idx++] = DMA_MASTER_BOARD;
            for (board_no = 0; board_no < num_boards; ++board_no)
            {
                struct BoardInfo* board = boards + board_no;
                if (board->valid && board_no != DMA_MASTER_BOARD && board_no != master_board)
                {
                    stop_order[idx++] = board_no;
                }
            }
            if (master_board != DMA_MASTER_BOARD)
            {
                stop_order[idx++] = master_board; // just to show that this board can be stopped any time
            }
#endif

#ifdef STOP_SYNC_FIRST
            // first, stop the sync master board
            // then, stop all other boards in arbitrary order
            stop_order[idx++] = master_board;
            for (board_no = 0; board_no < num_boards; ++board_no)
            {
                struct BoardInfo* board = boards + board_no;
                if (board->valid && board_no != DMA_MASTER_BOARD && board_no != master_board)
                {
                    stop_order[idx++] = board_no;
                }
            }
            if (master_board != DMA_MASTER_BOARD)
            {
                stop_order[idx++] = DMA_MASTER_BOARD; // just to show that this board can be stopped any time
            }
#endif

#ifdef STOP_SLAVE_FIRST
            // first, stop the sync master board (kill the only board that actively handles DMAs)
            // then, stop all other boards in arbitrary order
            for (board_no = 0; board_no < num_boards; ++board_no)
            {
                struct BoardInfo* board = boards + board_no;
                if (board->valid && board_no != master_board && board_no != DMA_MASTER_BOARD)
                {
                    stop_order[idx++] = board_no;
                }
            }
            stop_order[idx++] = master_board;
            if (master_board != DMA_MASTER_BOARD)
            {
                stop_order[idx++] = DMA_MASTER_BOARD;
            }
#endif
        }

        LARGE_INTEGER sleep_time;
        //sleep_time.QuadPart = (20 * measurement_counter + 600) * 10; // sleep 500us
        sleep_time.QuadPart = (800) * 10; // sleep 500us
        RtSleepFt(&sleep_time);

        for (int n = 0; n < num_boards; ++n)
        {
            board_no = stop_order[n];
            if (board_no < 0)
            {
                break;
            }

            RtPrintf("Stopping board %d...\n", board_no);
            err = DeWeSetParam_i32(board_no, CMD_STOP_ACQUISITION, 0);
            CheckError(err);
            RtSleep(2);
            err = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ACQ_BUFFER, 0);
            CheckError(err);
        }

        //RtMonitorChangeState(MONITOR_CONTROL_STOP);
        RtPrintf("\n");
        RtPrintf("*****************************\n");
        RtPrintf("Finished with %u errors\n", num_errors);
        printJitter();
        RtPrintf("*****************************\n");
    }
    //RtMonitorChangeState(MONITOR_CONTROL_STOP);

    RtPrintf("Closing boards...\n");
    err = DeWeSetParam_i32(0, CMD_CLOSE_BOARD_ALL, 0);
    CheckError(err);

    return ERR_NONE;
}

int main(int argc, char* argv[])
{
    int err = ERR_NONE;
    int nNoOfBoards = 0;
    int revision = DeWePxiLoadByName(DWPXI_API_DLL);
    if (revision == 0)
    {
        RtPrintf("ERROR: Cannot load the DWPXI API DLL: %s\n", DWPXI_API_DLL);
        return 1;
    }

    retrieve_driver_info();

    QueryPerformanceFrequency(&s_perf_freq);

    err = configure_api(&nNoOfBoards);
    if (err > ERR_NONE)
    {
        goto cleanup;
    }

    if (nNoOfBoards > MAX_BOARDS)
    {
        // use only the first MAX_BOARDS boards
        nNoOfBoards = MAX_BOARDS;
    }

    RtPrintf("Initialized the API successfully and found %d boards\n", nNoOfBoards);

    if (nNoOfBoards == 0)
    {
        goto cleanup;
    }

    RtPrintf("Measurement Configuration:\n");
    RtPrintf(" - sample rate: %d Hz\n", SAMPLE_RATE);
    RtPrintf(" - block size:  %d samples\n", BLOCK_SIZE);
    RtPrintf(" - DMA buffer:  %d blocks\n", NUM_BLOCKS);
    RtPrintf(" - cycle time:  %d us\n", US_PER_ITERATION);
    RtPrintf(" - iterations:  %u cycles\n", NUM_ITERATIONS);
    RtPrintf("\n");

    RtPrintf("Starting at "); printLocalTime(true);

    err = perform_stability_evaluation(nNoOfBoards, NUM_ITERATIONS, NUM_MEASUREMENTS);

    RtPrintf("Stopped at "); printLocalTime(true);

cleanup:
    DeWePxiUnload();

    RtPrintf("Example finished. Exit code %d\n", err);

    return err;
}
