#!/usr/bin/python
# Copyright DEWETRON GmbH 2021
#
# Short example to describe how use the oards TEDS interface
#


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
    nBoardId = 0  
    nNoOfBoards = 0
    nNoOfChannels = 0

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
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_OPEN_BOARD, 0 )
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_RESET_BOARD, 0 )

    # After reset all channels are disabled.
    # So enable all channels
    target = "BoardID%d/AI" % nBoardId
    nErrorCode = DeWeSetParamStruct_str(target +"/AIAll", "Used", "True")

    # Get number of channels
    [nErrorCode, nNoOfChannels] = DeWeGetParamStruct_str(target, "Channels")

    # Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0)

    if nErrorCode != 0:
        return nErrorCode

    # read TEDS
    nErrorCode = readTEDS(nBoardId, int(nNoOfChannels))

    # Close and deinit
    nErrorCode = DeWeSetParam_i32( 0, CMD_CLOSE_BOARD, 0 )

    # Unload pxi_api.dll
    DeWePxiUnload()

    return nErrorCode


def readTEDS(nBoardId, nNoOfChannels):

    

    for i in range(0, nNoOfChannels):

        # err = readSingleTEDSEX_str(nBoardId, i)
        # if err > 0:
        #     return err

        err = updateSingleTEDSEX_i32(nBoardId, i)
        if err > 0:
            return err

    return 0


def readSingleTEDSEX_str(nBoardId, channel_no):
    """
    Read TEDS using TedsReadEx str command
    """
    target = "BoardID%d/AI%d" % (nBoardId, channel_no)

    ret = DeWeGetParamStruct_str(target, "TedsReadEx")
    if ret[0] == 0:
        print(str(ret))

    return 0


def updateSingleTEDSEX_i32(nBoardId, channel_no):
    """
    Read TEDS i32 and XML commands
    """
    enable_write = False    # so the TEDS EEPROM is not changed by accident

    target = "BoardID%d/aitedsex/AI%d" % (nBoardId, channel_no)

    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_READ, channel_no)
    if err != 0:
        printf("Reading TEDS FAILED")
        return err

    ret = DeWeGetParamXML_str(target, "TEDSData")
    if ret[0] == 0:
        print(str(ret))

    # early return if write is disabled
    if not enable_write:
        return ret[0]

    # example: update serial number
    err = DeWeSetParamXML_str(target, "TEDSData/TEDSInfo/@Serial", "654321")
    if err != 0:
        print("Setting new serial FAILED" + str(err))
        return err

    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_SYNCHRONIZE, channel_no)
    if err != 0:
        print("Syncing TEDS FAILED" + str(err))
        return err

    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_WRITE, channel_no)
    if err != 0:
        print("Writing TEDS FAILED" + str(err))
        return err

    # verify
    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_READ, channel_no)
    if err != 0:
        print("Reading TEDS FAILED" + str(err))
        return err

    ret = DeWeGetParamXML_str(target, "TEDSData/TEDSInfo/@Serial")
    if ret[0] == 0:
        print(str(ret))

    return 0


#----------------------------------------------------------------------
# main
if __name__ == "__main__":
    ret = main(sys.argv)
    sys.exit(ret)
