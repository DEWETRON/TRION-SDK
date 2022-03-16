/**
 * TRION-SDK Quickstart example.
 *
 * This code is licensed under MIT license (see LICENSE.txt for details)
 * Copyright (c) 2022 by DEWETRON GmbH
 */


#include "dewepxi_load.h"
#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"


int main(int argc, char* argv[])
{
    int boards = 0;
    int avail_samples = 0;

    // Step 1 : Basic SDK Initialization
    DeWePxiLoad();

    // boards is negative for simulation
    DeWeDriverInit(&boards);

    // Step 2: Open boards
    // 0: chassis controller
    // 1: TRION3-1850-MULTI
    DeWeSetParam_i32(0, CMD_OPEN_BOARD, 0);
    DeWeSetParam_i32(0, CMD_RESET_BOARD, 0);
    DeWeSetParam_i32(1, CMD_OPEN_BOARD, 0);
    DeWeSetParam_i32(1, CMD_RESET_BOARD, 0);

    // Step 3: Enable board-counter channel on board 0
    DeWeSetParamStruct_str("BoardID0/BoardCnt0", "Used", "True");

    // Step 4: Configure acquisition properties
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_SIZE, 200);
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_COUNT, 50);
    DeWeSetParamStruct_str("BoardID0/AcqProp", "SampleRate", "2000");

    // Step 5: Configure sync-in propoerties
    DeWeSetParamStruct_str("BoardID0/AcqProp/SyncSettings/SyncIn", "Mode", "PTP");
    //Startcounter has to be set equal to Samplerate (eg 2000)
    DeWeSetParamStruct_str("BoardID0/AcqProp", "StartCounter", "2000");

    // Step 6: Apply settings
    DeWeSetParam_i32(0, CMD_UPDATE_PARAM_ALL, 0);

    // Step 7: Issue resync
    DeWeSetParam_i32(0, CMD_TIMING_STATE, 0);

    // Step 8: Wait until the board indicates a good sync-state
    {
        sint32 timing_state;
        // Break with success or CTRL+C only
        do
        {
            Sleep(100);
            DeWeGetParam_i32(0, CMD_TIMING_STATE, &timing_state);
        } while (timing_state != TIMINGSTATE_LOCKED);
    }

    // Step 9: Start acquisition
    DeWeSetParam_i32(0, CMD_START_ACQUISITION, 0);

    // This example is only shows howto get the number of samples available
    // and howto free them:

    // sleep for a short period, then:
    DeWeGetParam_i32(0, CMD_BUFFER_AVAIL_NO_SAMPLE, &avail_samples);
    DeWeSetParam_i32(0, CMD_BUFFER_FREE_NO_SAMPLE, avail_samples);

    // Step 10: Stop acquisition
    DeWeSetParam_i32(0, CMD_STOP_ACQUISITION, 0);

    // Step 11: Free boards and unload SDK
    DeWeSetParam_i32(0, CMD_CLOSE_BOARD, 0);
    DeWeSetParam_i32(1, CMD_CLOSE_BOARD, 0);
    DeWeDriverDeInit();
    DeWePxiUnload();
    return 0;
}