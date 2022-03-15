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

    // Step 3: Enable AI0 channel on board 1, disable all other
    DeWeSetParamStruct_str("BoardID1/AI0", "Used", "True");
    DeWeSetParamStruct_str("BoardID1/AI1", "Used", "False");
    DeWeSetParamStruct_str("BoardID1/AI2", "Used", "False");
    DeWeSetParamStruct_str("BoardID1/AI3", "Used", "False");

    // Step 4: Configure acquisition properties
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_SIZE, 200);
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_COUNT, 50);
    DeWeSetParamStruct_str("BoardID1/AcqProp", "SampleRate", "2000");

    // Step 5: Apply settings
    DeWeSetParam_i32(1, CMD_UPDATE_PARAM_ALL, 0);

    // Step 6: Start acquisition
    DeWeSetParam_i32(1, CMD_START_ACQUISITION, 0);

    // Step 7: Measurement loop and sample processing
    
    // This example is only shows howto get the number of samples available
    // and howto free them:

    // sleep for a short period, then:
    DeWeGetParam_i32(1, CMD_BUFFER_AVAIL_NO_SAMPLE, &avail_samples);
    DeWeSetParam_i32(1, CMD_BUFFER_FREE_NO_SAMPLE, avail_samples);

    // Step 8: Stop acquisition
    DeWeSetParam_i32(1, CMD_STOP_ACQUISITION, 0);

    // Step 9: Free boards and unload SDK
    DeWeSetParam_i32(0, CMD_CLOSE_BOARD, 0);
    DeWeSetParam_i32(1, CMD_CLOSE_BOARD, 0);
    DeWeDriverDeInit();
    DeWePxiUnload();
    return 0;
}