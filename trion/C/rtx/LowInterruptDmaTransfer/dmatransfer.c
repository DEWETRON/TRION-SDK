/**
 * Short example to describe how to transfer data from multiple TRION3 1820 MULTI boards with
 * DMA
 *
 * This example can be used with up to 8 TRION3-1820-MULTI-8 boards and a TRION3-CONTROLLER
 *
 *
 * Describes the following:
 *  - Configure the API for multi-threaded initialization with thread affinity
 *  - Configure the API to use only a single CPU for interrupt handling
 *  - Configure the API to use a dedicated DMA master board (slave boards will not emit any interrupt)
 *  - Setup of 8 AI channels and board counter per board
 *  - Set first board (CHASSIS CONTROLLER) as master
 *  - Set remaining boards as slaves
 *  - Set sample rate to 200000 Samples/second on each board
 *  - Acquisition configuration is done via XML
 *  - Check every board counter value for validity
 */

#ifndef UNDER_RTSS
#error "This example must be compiled for the RTX64 realtime subsystem"
#endif

#include <dewepxi_load.h>
#include <Rtapi.h>

#include <stdio.h>

// TODO: Change this path to the actual location of the dwpxi_api_x64.rtdll file
#define DWPXI_API_DLL "dwpxi_api_x64.rtdll"

#define MAX_BOARDS 13
#define SAMPLE_RATE "200000"
#define BLOCK_SIZE 200
#define NUM_BLOCKS 3
#define NUM_ITERATIONS (1000)

struct BoardInfo
{
    int index;
    int valid;
    char name[40];
    int num_ai;
    int num_cnt;
    int num_bcnt;
    sint32 scanline_size;
    int64_t total_samples;
    char* buffer_start;
    char* buffer_end;
};

void print_error(int err)
{
    if (err > 0)
    {
        RtPrintf("Error: %s\n", DeWeErrorConstantToString(err));
    }
    else if (err < 0)
    {
        RtPrintf("Warning: %s\n", DeWeErrorConstantToString(err));
    }
}

#define CheckError(err) if (err != ERR_NONE) { print_error(err); if (err > 0) return err; }

int board_initialize(struct BoardInfo* self, int board_no)
{
    int err = ERR_NONE;
    char target[32] = { 0 };
    char buffer[32] = { 0 };

    self->index = board_no;

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

    // Enable thread support (i.e. configure multiple TRION boards in parallel)
    err = DeWeSetParamStruct_str("driver/api/config/thread", "enabled", "true");
    CheckError(err);

    // Set thread affinity to four CPUs (uses during multi-threaded initialization)
    snprintf(buffer, sizeof(buffer), "%d", 0x3C); // 0011 1100
    err = DeWeSetParamStruct_str("driver/api/config/thread", "Affinity", buffer);
    CheckError(err);

    // Set interrupt affinity to one CPU
    snprintf(buffer, sizeof(buffer), "%d", 0x10); // 0001 0000
    err = DeWeSetParamStruct_str("driver/api/config/thread", "IrqAffinity", buffer);
    CheckError(err);

    // Set board 0 as the master board for interrupt handling (only the master board will now emit interrupts)
    err = DeWeSetParamStruct_str("driver/api/config/thread", "masterboard", "0");
    CheckError(err);

    // Cobine "DMA finished" interrupts and process them only on the previously set master board
    err = DeWeSetParamStruct_str("driver/api/config/thread", "CombineDmaInterrupts", "true");
    CheckError(err);

    err = DeWeDriverInit(num_boards);
    CheckError(err);
    
    if (*num_boards < 0)
    {
        // In simulations, the number of boards is negative
        *num_boards = -(*num_boards);
    }

    return ERR_NONE;
}

int verify_block(struct BoardInfo* board, const void* data, int num_samples)
{
    if (board->scanline_size < sizeof(uint32_t))
    {
        return ERR_BUFFER_NO_AVAIL_DATA;
    }

    // the board counter is always the last element in the scanline (production code should interpret the scanline descriptor)
    const char* board_cnt_ptr = (const char*)data + board->scanline_size - sizeof(uint32_t);
    for (int n = 0; n < num_samples; ++n)
    {
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
            return ERR_INVALID_VALUE;
        }

        // advance data pointer to next scanline and correct for buffer wrap-around
        board_cnt_ptr += board->scanline_size;
        if (board_cnt_ptr >= board->buffer_end)
        {
            board_cnt_ptr = board_cnt_ptr - board->buffer_end + board->buffer_start;
        }
    }
    return ERR_NONE;
}

int acquisition_loop(struct BoardInfo* boards, int num_boards, size_t num_runs)
{
    int err = ERR_NONE;
    size_t num_it;
    for (num_it = 0; num_it < num_runs; ++num_it)
    {
        int n;
        for (n = 0; n < num_boards; ++n)
        {
            int samples_available = 0;
            void* data = NULL;
            struct BoardInfo* board = boards + n;

            if (!board->valid)
            {
                continue;
            }
            
            err = DeWeGetParam_i32(board->index, CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLE, &samples_available);
            CheckError(err);

            if (samples_available != BLOCK_SIZE)
            {
                RtPrintf("Board %d returned %d blocks, not real-time?\n", board->index, samples_available / BLOCK_SIZE);
            }

            err = DeWeGetParam_i64(board->index, CMD_BUFFER_0_ACT_SAMPLE_POS, (sint64*)&data);
            CheckError(err);

            // check if all board counter values are as expected
            err = verify_block(board, data, samples_available);
            CheckError(err);

            err = DeWeSetParam_i32(board->index, CMD_BUFFER_0_FREE_NO_SAMPLE, samples_available);
            CheckError(err);

            board->total_samples += samples_available;
        }

        if (num_it % (1000 * 10) == 0)
        {
            // print progress after each 10 seconds
            RtPrintf("%8u> running...\n", (uint32)num_it);
        }
    }
    return ERR_NONE;
}

int perform_dma_measurements(int num_boards, size_t num_iterations)
{
    int err = ERR_NONE;
    struct BoardInfo boards[MAX_BOARDS] = {0};
    int master_board = -1;
    int board_no = 0;

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

        board->valid = board->num_bcnt > 0; // We will only use boards with board counters
        
        RtPrintf("Found board %d: %s with %d AI, %d CNT and %d BCNT channels\n",
            board_no, board->name,
            board->num_ai, board->num_cnt, board->num_bcnt);

        if (board->valid)
        {
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
                            "<SampleRate Unit = \"Hz\">" SAMPLE_RATE "</SampleRate>"
                            "<OperationMode>Master</OperationMode>"
                            "<ExtTrigger>False</ExtTrigger>"
                            "<ExtClk>False</ExtClk>"
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
                            "<SampleRate Unit = \"Hz\">" SAMPLE_RATE "</SampleRate>"
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
    }

    // Start slaves first
    RtPrintf("Starting slaves...\n");
    for (board_no = 0; board_no < num_boards; ++board_no)
    {
        struct BoardInfo* board = boards + board_no;
        if (board->valid && board_no != master_board)
        {
            err = DeWeSetParam_i32(board_no, CMD_START_ACQUISITION, 0);
            CheckError(err);
        }
    }

    // Start the master
    RtPrintf("Starting master...\n");
    err = DeWeSetParam_i32(master_board, CMD_START_ACQUISITION, 0);
    CheckError(err);

    err = acquisition_loop(boards, num_boards, num_iterations);
    CheckError(err);

    RtPrintf("Stopping acquisition...\n");
    for (board_no = 0; board_no < num_boards; ++board_no)
    {
        struct BoardInfo* board = boards + board_no;
        if (board->valid)
        {
            err = DeWeSetParam_i32(board_no, CMD_STOP_ACQUISITION, 0);
            CheckError(err);
        }
    }

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

    err = perform_dma_measurements(nNoOfBoards, NUM_ITERATIONS);

cleanup:
    DeWePxiUnload();

    RtPrintf("Example finished. Exit code %d\n", err);

    return err;
}
