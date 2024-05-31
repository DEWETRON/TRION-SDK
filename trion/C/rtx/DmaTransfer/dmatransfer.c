/**
 * Short example to describe how to transfer data from multiple TRION3 1820 MULTI boards with
 * DMA
 *
 * This example can be used with up to 8 TRION3-1820-MULTI-8 boards and a TRION3-CONTROLLER
 *
 *
 * Describes the following:
 *  - Configure the API for multi-threaded initialization with thread affinity
 *  - Setup of 8 AI channels and board counter per board
 *  - Set first board (CHASSIS CONTROLLER) as master
 *  - Set remaining boards as slaves
 *  - Set sample rate to 200000 Samples/second on each board
 */

#ifndef UNDER_RTSS
#error "This example must be compiled for the RTX64 realtime subsystem"
#endif

#include <dewepxi_load.h>
#include <Rtapi.h>

#include <stdio.h>

// TODO: Change this path to the actual location of the dwpxi_api_x64.rtdll file
#define DWPXI_API_DLL "dwpxi_api_x64.rtdll"

#define MAX_BOARDS 9
#define SAMPLE_RATE 200000
#define BLOCK_SIZE 200
#define NUM_BLOCKS 3

struct BoardInfo
{
    int index;
    int valid;
    char name[40];
    int num_ai;
    int num_cnt;
    int num_bcnt;
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

    // Set thread affinity to three CPUs
    snprintf(buffer, sizeof(buffer), "%d", 0x70); // 0111 0000
    err = DeWeSetParamStruct_str("driver/api/config/thread", "affinity", buffer);
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

int acquisition_loop(struct BoardInfo* boards, int num_boards, int num_runs)
{
    int err = ERR_NONE;
    while (num_runs-- > 0)
    {
        int board_no = 0;
        for (board_no = 0; board_no < num_boards; ++board_no)
        {
            int samples_available = 0;
            
            err = DeWeGetParam_i32(board_no, CMD_BUFFER_WAIT_AVAIL_NO_SAMPLE, &samples_available);
            CheckError(err);

            if (samples_available != BLOCK_SIZE)
            {
                RtPrintf("Board %d returned not exactly one block, not real-time?\n", board_no);
            }

            err = DeWeSetParam_i32(board_no, CMD_BUFFER_FREE_NO_SAMPLE, samples_available);
            CheckError(err);
        }
    }
    return ERR_NONE;
}

int perform_dma_measurements(int num_boards)
{
    int err = ERR_NONE;
    struct BoardInfo boards[MAX_BOARDS] = {0};
    int master_board = -1;
    int board_no = 0;

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
                master_board = board_no;
            }

            err = DeWeSetParam_i32(board_no, CMD_OPEN_BOARD, 0);
            CheckError(err);
            err = DeWeSetParam_i32(board_no, CMD_RESET_BOARD, 0);
            CheckError(err);

            // Configure Acquisition
            snprintf(target, sizeof(target), "BoardID%d/AcqProp", board_no);

            if (master_board == board_no)
            {
                err = DeWeSetParamStruct_str(target, "OperationMode", "Master");
                CheckError(err);
                err = DeWeSetParamStruct_str(target, "ExtTrigger", "False");
                CheckError(err);
            }
            else
            {
                err = DeWeSetParamStruct_str(target, "OperationMode", "Slave");
                CheckError(err);
                err = DeWeSetParamStruct_str(target, "ExtTrigger", "PosEdge");
                CheckError(err);
            }

            err = DeWeSetParamStruct_str(target, "ExtClk", "False");
            CheckError(err);
            snprintf(buffer, sizeof(buffer), "%d", SAMPLE_RATE);
            err = DeWeSetParamStruct_str(target, "SampleRate", buffer); // Each sample will generate a call back
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
            err = DeWeSetParam_i32(board_no, CMD_BUFFER_BLOCK_SIZE, BLOCK_SIZE);
            CheckError(err);
            err = DeWeSetParam_i32(board_no, CMD_BUFFER_BLOCK_COUNT, NUM_BLOCKS);
            CheckError(err);

            // Commit settings to the board
            err = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ALL, 0);
            CheckError(err);
        }
    }

    RtPrintf("Starting acquisition...\n");

    // Start slaves first
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
    err = DeWeSetParam_i32(master_board, CMD_START_ACQUISITION, 0);
    CheckError(err);

    err = acquisition_loop(boards, num_boards, 1000);
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

    if (nNoOfBoards > 9)
    {
        // use only the first 9 boards (CONTROLLER + 8 boards)
        nNoOfBoards = 9;
    }

    RtPrintf("Initialized the API successfully and found %d boards\n", nNoOfBoards);

    if (nNoOfBoards == 0)
    {
        goto cleanup;
    }

    err = perform_dma_measurements(nNoOfBoards);

cleanup:
    DeWePxiUnload();

    RtPrintf("Example finished. Exit code %d\n", err);

    return err;
}
