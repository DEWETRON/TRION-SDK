/**
 * Short example to describe how to poll data from multiple TRION3 1820 MULTI boards
 *
 * This example can be used with up to 16 TRION3-1820-MULTI-8 boards and a TRION3-CONTROLLER
 *
 *
 * Describes the following:
 *  - Configure the API for multi-threaded initialization
 *  - Setup of 8 AI channels and board counter per board
 *  - Set first board as master
 *  - Set remaining boards as slave
 *  - Set sample rate to 1000 Samples/second on each board
 *  - Turn off DMA and register a "new sample" callback function on the master board
 *  - Print of min/max board counters of all boards (should be equal because boards are synchronized)
 */

#ifndef UNDER_RTSS
#error "This example must be compiled for the RTX64 realtime subsystem"
#endif

#include <dewepxi_load.h>
#include <Rtapi.h>

#include <stdio.h>

// TODO: Change this path to the actual location of the dwpxi_api_x64.rtdll file
#define DWPXI_API_DLL "dwpxi_api_x64.rtdll"

#define MAX_BOARDS 20
#define SAMPLE_RATE 1000 // the sample rate in Hz, the polling callback will be called at the same rate

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

// For a TRION3-1820-MULTI-8 board, there are 14 sample registers:
// - 8 x 32bit AI sample value
// - 2 x (32bit Counter + 32bit subcounter)
// - 1 x (32bit Board-Counter + 32bit Board-subcounter)
struct HsMultiValues
{
    int ai[8];
    struct { int count; int subcount; } cnt[2];
    struct { int count; int subcount; } boardcnt;
};

// For a TRION3-CONTROLLER board, there are 11 sample registers:
// - 4 x (32bit Counter + 32bit subcounter)
// - 1 x (32bit Board-Counter + 32bit Board-subcounter)
// - 1 x 32bit DIO data (bits 0 to 11)
struct ChassisControllerValues
{
    struct { int count; int subcount; } cnt[4];
    struct { int count; int subcount; } boardcnt;
    int dio;
};

struct Context
{
    int num_boards;
    struct BoardInfo board[MAX_BOARDS];
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

    *num_boards = 0;

    // Enable thread support (i.e. configure multiple TRION boards in parallel)
    err = DeWeSetParamStruct_str("driver/api/config/thread", "enabled", "true");
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

/**
 * Main callback method that will be called on each new sample from the interrupt service thread directly
 * IMPORTANT: Do not use extensive RtPrintf calls or set any breakpoints in here
 */
void new_sample_callback(int board_no, void* ctx)
{
    struct Context* boards = (struct Context*)ctx;
    int err = ERR_NONE;
    int min_sample_count = 0x7FFFFFFF;
    int max_sample_count = -1;
    int num_values = 0;
    int n = 0;
    for (n = 0; n < boards->num_boards; ++n)
    {
        struct BoardInfo* board = boards->board + n;
        void* values_pointer = NULL;
        sint32 sample_count = 0;

        if (!board->valid)
            continue;
        
        err = DeWeGetParam_i64(n, CMD_BOARD_ACT_SAMPLE_VALUE_POINTER, (sint64*)&values_pointer);
        
        // read the sample count from the board counter
        if (board->num_value_registers == sizeof(struct HsMultiValues) / sizeof(int))
        {
            struct HsMultiValues* values = (struct HsMultiValues*)values_pointer;
            sample_count = values->boardcnt.count;
        }
        else if (board->num_value_registers == sizeof(struct ChassisControllerValues) / sizeof(int))
        {
            struct ChassisControllerValues* values = (struct ChassisControllerValues*)values_pointer;
            sample_count = values->boardcnt.count;
        }
        else
        {
            // Ignore unsupported number of registers
            continue;
        }

        // Store min and max values to verify that all boards are in sync
        if (sample_count < min_sample_count)
        {
            min_sample_count = sample_count;
        }
        if (sample_count > max_sample_count)
        {
            max_sample_count = sample_count;
        }
        ++num_values;
    }

    // Limit the number of print calls to 100Hz or less, printing is not recommend in the callback function in production code
    if (SAMPLE_RATE <= 100)
    {
        RtPrintf("New sample from %d board: count min = %d, max = %d\n", num_values, min_sample_count, max_sample_count);
    }
    else if (min_sample_count % 10 == 0)
    {
        RtPrintf("New sample from %d board: count min = %d, max = %d\n", num_values, min_sample_count, max_sample_count);
    }
}

int perform_polling(int num_boards)
{
    int err = ERR_NONE;
    struct Context context = {0};
    int master_board = -1;
    int board_no = 0;

    context.num_boards = num_boards;

    // Iterate over all boards, select the master and valid slaves and configure them
    for (board_no = 0; board_no < num_boards; ++board_no)
    {
        char target[32] = { 0 };
        char buffer[32] = { 0 };
        struct BoardInfo* board = context.board + board_no;
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

            err = DeWeGetParam_i32(board_no, CMD_BOARD_ACT_SAMPLE_VALUE_COUNT, &board->num_value_registers);
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
            snprintf(buffer, sizeof(buffer), "%d Hz", SAMPLE_RATE);
            err = DeWeSetParamStruct_str(target, "SampleRate", buffer); // Each sample will generate a call back
            CheckError(err);
            err = DeWeSetParamStruct_str(target, "DMABuffer0Enabled", "False"); // Disable DMA since we will poll channel values directly
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

            // Commit settings to the board
            err = DeWeSetParam_i32(board_no, CMD_UPDATE_PARAM_ALL, 0);
            CheckError(err);
        }
    }

    // Configure the Polling callback
    // ==============================
    // After a sample has been acquired by a board, an interrupt is generated
    // This interrupt will call a user-callback from the interrupt service thread directly
    // We simply attach to the interrupt of the master board and read across all boards (boards are synchronized)

    // set a context, which will be a void* parameter to the callback method
    err = DeWeSetParam_i64(master_board, CMD_BOARD_ACT_SAMPLE_CALLBACK_CONTEXT, (sint64)&context);
    CheckError(err);

    // set the actual callback function. Once the callback is set, it will be called for each sample during acquisition
    err = DeWeSetParam_i64(master_board, CMD_BOARD_ACT_SAMPLE_CALLBACK, (sint64)&new_sample_callback);
    CheckError(err);

    RtPrintf("Starting acquisition...\n");

    // Start slaves first
    for (board_no = 0; board_no < num_boards; ++board_no)
    {
        struct BoardInfo* board = context.board + board_no;
        if (board->valid && board_no != master_board)
        {
            err = DeWeSetParam_i32(board_no, CMD_START_ACQUISITION, 0);
            CheckError(err);
        }
    }

    // Start the master
    err = DeWeSetParam_i32(master_board, CMD_START_ACQUISITION, 0);
    CheckError(err);

    // Wait some time, the callback will be called while we wait
    Sleep(1000);

    RtPrintf("Stopping acquisition...\n");
    for (board_no = 0; board_no < num_boards; ++board_no)
    {
        struct BoardInfo* board = context.board + board_no;
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

    RtPrintf("Initialized the API successfully and found %d boards\n", nNoOfBoards);

    if (nNoOfBoards == 0)
    {
        goto cleanup;
    }

    err = perform_polling(nNoOfBoards);

cleanup:
    DeWePxiUnload();

    RtPrintf("Example finished. Exit code %d\n", err);

	return err;
}
