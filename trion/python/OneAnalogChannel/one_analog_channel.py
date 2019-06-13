#!/usr/bin/python

#
# Short example to describe how a digital counter channel is read.
#
# This example should be used with any TRIOn board featuring AI channels as board 0
#
# Describes following:
#  - Setup of 1 AI channel
#  - Query for the ADC delay
#  - Print of analog values.


import sys
sys.path.append('../../../trion_api/python')
import time
from dewepxi_load import *
from dewepxi_apicore import *
#import msvcrt


def main(argv):
    """
    Main function
    """
    nErrorCode  = 0
    nNoOfBoards = 0

    # Load pxi_api.dll
    if not DeWePxiLoad():
        print("trion api dll could not be found. Exiting...")
        sys.exit(-1)


    # Initialize driver and retrieve the number of TRION boards
    # nNoOfBoards is a negative number if system is in DEMO mode!
    [nErrorCode, nNoOfBoards] = DeWeDriverInit()

    # Check if TRION cards are in the system
    if abs(nNoOfBoards) == 0:
        print("No Trion cards found. Aborting...\n")
        print("Please configure a system using the DEWE2 Explorer.\n")
        sys.exit(1)


    # Open & Reset the board
    nErrorCode = DeWeSetParam_i32( 0, CMD_OPEN_BOARD, 0 )
    nErrorCode = DeWeSetParam_i32( 0, CMD_RESET_BOARD, 0 )

    # Set configuration to use one board in standalone operation
    nErrorCode = DeWeSetParamStruct_str( "BoardID0/AcqProp", "OperationMode", "Slave")
    nErrorCode = DeWeSetParamStruct_str( "BoardID0/AcqProp", "ExtTrigger", "False")
    nErrorCode = DeWeSetParamStruct_str( "BoardID0/AcqProp", "ExtClk", "False")

    # After reset all channels are disabled.
    # So here 1 analog channel will be enabled (AI)
    nErrorCode = DeWeSetParamStruct_str( "BoardID0/AI0", "Used", "True")

    # Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
    # For the default samplerate 2000 samples per second, 200 is a buffer for
    # 0.1 seconds
    nErrorCode = DeWeSetParam_i32( 0, CMD_BUFFER_BLOCK_SIZE, 200)
    # Set the ring buffer size to 50 blocks. So ring buffer can store samples
    # for 5 seconds
    nErrorCode = DeWeSetParam_i32( 0, CMD_BUFFER_BLOCK_COUNT, 50)

    # Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( 0, CMD_UPDATE_PARAM_ALL, 0)

    # Get the ADC delay. The typical conversion time of the ADC.
    # The ADCDelay is the offset of analog samples to digital or counter samples.
    # It is measured in number of samples,
    [nErrorCode, nADCDelay] = DeWeGetParam_i32( 0, CMD_BOARD_ADC_DELAY)

    # Data Acquisition - stopped with any key
    nErrorCode = DeWeSetParam_i32( 0, CMD_START_ACQUISITION, 0)

    if nErrorCode <= 0:
        nBufEndPos = 0           # Last position in the ring buffer
        nBufSize   = 0           # Total buffer size

        # Get detailed information about the ring buffer
        # to be able to handle the wrap around
        [nErrorCode, nBufEndPos] = DeWeGetParam_i64( 0, CMD_BUFFER_END_POINTER)
        print("nErrorCode = %d, nBufEndPos = %d" % (nErrorCode, nBufEndPos))
        [nErrorCode, nBufSize]   = DeWeGetParam_i32( 0, CMD_BUFFER_TOTAL_MEM_SIZE)
        print("nErrorCode = %d, nBufSize = %d" % (nErrorCode, nBufSize))

        # Acquisition loops
        for loop_count in range(1, 10):
        
            nReadPos      = 0
            nAvailSamples = 0
            nRawData      = 0

            # wait for 100ms
            time.sleep(0.1)

            # Get the number of samples already stored in the ring buffer
            [nErrorCode, nAvailSamples] = DeWeGetParam_i32( 0, CMD_BUFFER_AVAIL_NO_SAMPLE)
            print("nErrorCode = %d, nAvailSamples = %d" % (nErrorCode, nAvailSamples))

            # Available samples has to be recalculated according to the ADC delay
            nAvailSamples = nAvailSamples - nADCDelay

            # skip if number of samples is smaller than the current ADC delay
            if nAvailSamples <= 0:
                continue

            # Get the current read pointer
            [nErrorCode, nReadPos] = DeWeGetParam_i64( 0, CMD_BUFFER_ACT_SAMPLE_POS)
            print("nErrorCode = %d, nReadPos = %d" % (nErrorCode, nReadPos))

            # Read the current samples from the ring buffer
            for i in range(0, nAvailSamples):
                # Get the sample value at the read pointer of the ring buffer
                nRawData = DeWeGetSampleData(nReadPos)

                # Print the sample value
                print(nRawData)
                sys.stdout.flush()

                # Increment the read pointer
                nReadPos = nReadPos + 4;
                # Handle the ring buffer wrap around
                if nReadPos > nBufEndPos:
                    nReadPos -= nBufSize;

            # Free the ring buffer after read of all values
            nErrorCode = DeWeSetParam_i32( 0, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples)

    # Stop data acquisition
    nErrorCode = DeWeSetParam_i32( 0, CMD_STOP_ACQUISITION, 0)

    # Close and deinit
    nErrorCode = DeWeSetParam_i32( 0, CMD_CLOSE_BOARD, 0 )

    # Unload pxi_api.dll
    DeWePxiUnload()

    return nErrorCode

#----------------------------------------------------------------------
# main
if __name__ == "__main__":
    ret = main(sys.argv)
    sys.exit(ret)
