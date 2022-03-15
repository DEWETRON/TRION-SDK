/**
 * TRION-SDK Quickstart example with acquisition loop.
 *
 * This code is licensed under MIT license (see LICENSE.txt for details)
 * Copyright (c) 2022 by DEWETRON GmbH
 */


#include "dewepxi_load.h"
#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include <iostream>

int main(int argc, char* argv[])
{
    int boards = 0;
    int avail_samples = 0;
    sint64 buf_end_pos = 0;         // Last position in the ring buffer
    int buff_size = 0;              // Total size of the ring buffer

    // Basic SDK Initialization
    DeWePxiLoad();

    // boards is negative for simulation
    DeWeDriverInit(&boards);

    // Open boards
    // 0: chassis controller
    // 1: TRION3-1850-MULTI
    DeWeSetParam_i32(0, CMD_OPEN_BOARD, 0);
    DeWeSetParam_i32(0, CMD_RESET_BOARD, 0);
    DeWeSetParam_i32(1, CMD_OPEN_BOARD, 0);
    DeWeSetParam_i32(1, CMD_RESET_BOARD, 0);

    // Enable AI0 channel on board 1, disable all other
    DeWeSetParamStruct_str("BoardID1/AI0", "Used", "True");
    DeWeSetParamStruct_str("BoardID1/AI1", "Used", "False");
    DeWeSetParamStruct_str("BoardID1/AI2", "Used", "False");
    DeWeSetParamStruct_str("BoardID1/AI3", "Used", "False");

    // Configure acquisition properties
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_SIZE, 20);
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_COUNT, 50);
    DeWeSetParamStruct_str("BoardID1/AcqProp", "SampleRate", "100");

    // Apply settings
    DeWeSetParam_i32(1, CMD_UPDATE_PARAM_ALL, 0);

    // Get buffer configuration
    DeWeGetParam_i64(1, CMD_BUFFER_END_POINTER, &buf_end_pos);
    DeWeGetParam_i32(1, CMD_BUFFER_TOTAL_MEM_SIZE, &buff_size);

    // Start acquisition
    DeWeSetParam_i32(1, CMD_START_ACQUISITION, 0);

    // Measurement loop and sample processing
    sint64 read_pos = 0;
    sint32* read_pos_ptr = 0;
    int sample_value = 0;

    // Break with CTRL+C only
    while (1)
    {
        // Get the number of samples available
        DeWeGetParam_i32(1, CMD_BUFFER_AVAIL_NO_SAMPLE, &avail_samples);
        if (avail_samples <= 0)
        {
            continue;
        }

        // Get the current read pointer
        DeWeGetParam_i64(1, CMD_BUFFER_ACT_SAMPLE_POS, &read_pos);

        // Read the current samples from the ring buffer
        for (int i = 0; i < avail_samples; ++i)
        {
            // Handle the ring buffer wrap around
            if (read_pos >= buf_end_pos)
            {
                read_pos -= buff_size;
            }

            // read_pos a variable containing the address of the first sample of the new block.
            // It is necessary to cast it to a pointer (reinterpret_cast in C++) and to add i 
            // to process all samples.
            read_pos_ptr = reinterpret_cast<sint32*>(read_pos) + i;
            sample_value = *read_pos_ptr;

            // Warning: This will not work for multilple AI channels with 24Bit resolution
            // Please have a look at the acq with scan_descriptor example.

            std::cout << "AI0: " << std::dec << sample_value << "   " << std::hex << sample_value << std::endl;
        }

        DeWeSetParam_i32(1, CMD_BUFFER_FREE_NO_SAMPLE, avail_samples);
    }


    // Stop acquisition
    DeWeSetParam_i32(1, CMD_STOP_ACQUISITION, 0);

    // Free boards and unload SDK
    DeWeSetParam_i32(0, CMD_CLOSE_BOARD, 0);
    DeWeSetParam_i32(1, CMD_CLOSE_BOARD, 0);
    DeWeDriverDeInit();
    DeWePxiUnload();

    return 0;
}