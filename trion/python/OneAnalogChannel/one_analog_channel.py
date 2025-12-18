#!/usr/bin/python
# Copyright DEWETRON GmbH 2019
#
# Short example that describes how read an analog input channel
#
# This example can be used with any TRION board featuring AI channels
#
# Describes following:
#  - Select the first board with AI channels
#  - Setup of 1 AI channel
#  - Print of analog values.

import ctypes
import sys
import time
import numpy as np
from trion_sdk import *

BLOCK_SIZE = 1000
BLOCK_COUNT = 10
#RESOLUTION_AI = 16 # bit
RESOLUTION_AI = 24 # bit

USE_TRIONET_API = False          # Set to True when using TrioNet or NexDAQ devices
TRIONET_LOCAL_IP = "10.0.0.1"    # When using TRIONET, specify the IP of the computer interface
TRIONET_NET_MASK = "255.255.0.0" # When using TRIONET, specify the netmask of the computer interface

def main(argv):
    """
    Main function
    """
    nErrorCode  = ERROR_NONE
    nNoOfBoards = 0

    # Load pxi_api.dll
    backend = "TRIONET" if USE_TRIONET_API else "TRION"
    if not DeWePxiLoad(lib = backend):
        print("Trion API DLL could not be found. Exiting...")
        sys.exit(-1)

    if USE_TRIONET_API:
        nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/LocalIP", TRIONET_LOCAL_IP)
        nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/NetMask", TRIONET_NET_MASK)

    # Initialize driver and retrieve the number of TRION boards
    # nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode, nNoOfBoards = DeWeDriverInit()
    if nErrorCode != ERROR_NONE:
        print(DeWeErrorConstantToString(nErrorCode))
        sys.exit(nErrorCode)

    # Check if TRION cards are in the system
    if abs(nNoOfBoards) == 0:
        print("No Trion boards found. Aborting...\n")
        print("Please configure a system using the DEWE2 Explorer.\n")
        sys.exit(1)

    print(f"Found {abs(nNoOfBoards)} Trion boards:")

    # Open & Reset the boards
    nErrorCode = DeWeSetParam_i32( 0, CMD_OPEN_BOARD_ALL )
    nErrorCode = DeWeSetParam_i32( 0, CMD_RESET_BOARD_ALL )

    nBoard = None
    for n in range(abs(nNoOfBoards)):
        nErrorCode, boardname = DeWeGetParamStruct_str( f"BoardID{n}", "BoardName")
        nErrorCode, num_ai = DeWeGetParamStruct_str( f"BoardID{n}/AI", "Channels")
        nErrorCode, num_cnt = DeWeGetParamStruct_str( f"BoardID{n}/CNT", "Channels")
        print(f" - BoardID{n} {boardname}: #AI={num_ai}, #CNT={num_cnt}")
        if not nBoard and int(num_ai) > 0:
            nBoard = n

    if nBoard is None:
        print("Could not find a board with an AI channel")
        sys.exit(1)

    print()
    print(f"Configuring board {n}...")

    # Set configuration to use one board in standalone operation
    nErrorCode = DeWeSetParamStruct_str( f"BoardID{nBoard}/AcqProp", "OperationMode", "Master")
    nErrorCode = DeWeSetParamStruct_str( f"BoardID{nBoard}/AcqProp", "ExtTrigger", "False")
    nErrorCode = DeWeSetParamStruct_str( f"BoardID{nBoard}/AcqProp", "ExtClk", "False")
    nErrorCode = DeWeSetParamStruct_str( f"BoardID{nBoard}/AcqProp", "SampleRate", "10000")
    nErrorCode = DeWeSetParamStruct_str( f"BoardID{nBoard}/AcqProp", "ResolutionAI", f"{RESOLUTION_AI}")

    # By default, all analog channels are enabled after a reset
    # So here the first analog channel will be enabled (AI)
    nErrorCode = DeWeSetParamStruct_str( f"BoardID{nBoard}/AIALL", "Used", "False")
    nErrorCode = DeWeSetParamStruct_str( f"BoardID{nBoard}/AI0", "Used", "True")

    # Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    # For the samplerate 10000 samples per second, 1000 is a buffer for
    # 0.1 seconds
    nErrorCode = DeWeSetParam_i32( nBoard, CMD_BUFFER_BLOCK_SIZE, BLOCK_SIZE)
    # Set the circular buffer size to 50 blocks. So the circular buffer can store samples
    # for 5 seconds
    nErrorCode = DeWeSetParam_i32( nBoard, CMD_BUFFER_BLOCK_COUNT, BLOCK_COUNT)

    # Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoard, CMD_UPDATE_PARAM_ALL)

    # Determine the size of a sample scan
    nErrorCode, nSizeScan = DeWeGetParam_i32( nBoard, CMD_BUFFER_ONE_SCAN_SIZE )

    # Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( nBoard, CMD_START_ACQUISITION)

    if nErrorCode <= ERROR_NONE:
        # Get detailed information about the circular buffer
        # to be able to handle the wrap around
        nErrorCode, nBufEndPos = DeWeGetParam_i64( nBoard, CMD_BUFFER_END_POINTER)
        print(f"nErrorCode = {nErrorCode}, nBufEndPos = {nBufEndPos}")
        nErrorCode, nBufSize   = DeWeGetParam_i32( nBoard, CMD_BUFFER_TOTAL_MEM_SIZE)
        print(f"nErrorCode = {nErrorCode}, nBufSize = {nBufSize}")

        # Acquisition loops
        for _ in range(1, 10):
            # wait for 100ms
            time.sleep(0.1)

            # Get the number of samples already stored in the circular buffer
            nErrorCode, nAvailSamples = DeWeGetParam_i32( nBoard, CMD_BUFFER_AVAIL_NO_SAMPLE)
            print(f"nErrorCode = {nErrorCode}, nAvailSamples = {nAvailSamples}")

            # skip if number of samples is smaller than the current ADC delay
            if nAvailSamples <= 0:
                continue

            # Get the current read pointer
            nErrorCode, nReadPos = DeWeGetParam_i64( nBoard, CMD_BUFFER_ACT_SAMPLE_POS)
            print(f"nErrorCode = {nErrorCode}, nReadPos = {nReadPos}")

            # Read the current samples from the circular buffer
            # do not read across buffer boundaries
            while nAvailSamples > 0:
                read_samples = min(nAvailSamples, (nBufEndPos - nReadPos) // nSizeScan)

                raw_data_type = ctypes.c_byte * (nSizeScan * read_samples)
                raw_data = raw_data_type.from_address(nReadPos)
                if RESOLUTION_AI == 16:
                    data = np.frombuffer(raw_data, dtype=np.int16)[::2]
                    data = data * 2**-15 # convert int16 to floats between -1 and 1
                elif RESOLUTION_AI == 24:
                    data = np.frombuffer(raw_data, dtype=np.uint8)
                    data = data[::4] | data[1::4].astype(int) << 8 | data[2::4].view(np.int8).astype(int) << 16
                    data = data * 2**-23 # convert int24 to floats between -1 and 1
                else:
                    data = None

                # Print the first 100 values
                np.set_printoptions(formatter={"float": "{: 0.2f}".format})
                print(data[:100])

                # Free the circular buffer after read of values
                nErrorCode = DeWeSetParam_i32( nBoard, CMD_BUFFER_FREE_NO_SAMPLE, read_samples)

                nReadPos += nSizeScan * read_samples
                nAvailSamples -= read_samples
                if nReadPos >= nBufEndPos:
                    nReadPos -= nBufSize

    # Stop data acquisition
    nErrorCode = DeWeSetParam_i32( nBoard, CMD_STOP_ACQUISITION)

    # Close and deinit
    nErrorCode = DeWeSetParam_i32( 0, CMD_CLOSE_BOARD_ALL )

    # Unload pxi_api.dll
    DeWePxiUnload()

    return nErrorCode

#----------------------------------------------------------------------
# main
if __name__ == "__main__":
    ret = main(sys.argv)
    sys.exit(ret)
