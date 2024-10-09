#!/usr/bin/python3
# Copyright DEWETRON GmbH 2021
#
# Short example to describe how use the boards' TEDS interface
#


import sys
from trion_sdk import *

def main(argv):
    """
    Main function
    """

    nBoardId = 0

    # Load pxi_api.dll
    if not DeWePxiLoad():
        print("trion api dll could not be found. Exiting...")
        sys.exit(-1)

    # Initialize driver and retrieve the number of TRION boards
    # nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode, nNoOfBoards = DeWeDriverInit()

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
    target = f"BoardID{nBoardId}/AI"
    nErrorCode = DeWeSetParamStruct_str(target +"/AIAll", "Used", "True")

    # Get number of channels
    nErrorCode, nNoOfChannels = DeWeGetParamStruct_str(target, "Channels")

    # Update the hardware with settings
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL )

    if nErrorCode != ERROR_NONE:
        return nErrorCode

    # read TEDS
    nErrorCode = readTEDS(nBoardId, int(nNoOfChannels))

    # Close and deinit
    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_CLOSE_BOARD )

    # Unload pxi_api.dll
    DeWePxiUnload()

    return nErrorCode


def readTEDS(nBoardId: int, nNoOfChannels: int):
    for i in range(0, nNoOfChannels):

        # err = readSingleTEDSEX_str(nBoardId, i)
        # if err > 0:
        #     return err

        err = updateSingleTEDSEX_i32(nBoardId, i)
        if err > ERROR_NONE:
            return err

    return 0


def readSingleTEDSEX_str(nBoardId: int, channel_no: int):
    """
    Read TEDS using TedsReadEx str command
    """
    target = f"BoardID{nBoardId}/AI{channel_no}"
    err, ret = DeWeGetParamStruct_str(target, "TedsReadEx")
    if err == ERROR_NONE:
        print(ret)

    return 0


def updateSingleTEDSEX_i32(nBoardId: int, channel_no: int):
    """
    Read TEDS i32 and XML commands
    """
    enable_write = False    # so the TEDS EEPROM is not changed by accident
    simulate_teds = False   # Set to True to simulate a TEDS chip

    if simulate_teds:
        err = DeWeSetParamStruct_str("driver/api/TrionSystemSim/1WireDevice", f"BoardID{nBoardId}/AI{channel_no}", "DS2431")
        if err != ERROR_NONE:
            print("Simulating TEDS failed: " + DeWeErrorConstantToString(err))
            return err

    target = f"BoardID{nBoardId}/aitedsex/AI{channel_no}"

    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_READ, channel_no)
    if err != ERROR_NONE:
        print("Reading TEDS FAILED")
        return err

    err, ret = DeWeGetParamXML_str(target, "TEDSData")
    if err != ERROR_NONE:
        print(DeWeErrorConstantToString(err))
        return

    print(f"TEDS Data on BoardId{nBoardId}/AI{channel_no}:")
    print(ret)

    # early return if write is disabled
    if not enable_write:
        return ERROR_NONE

    # example: update serial number
    err = DeWeSetParamXML_str(target, "TEDSData/TEDSInfo/@Serial", "654321")
    if err != ERROR_NONE:
        print("Setting new serial FAILED" + DeWeErrorConstantToString(err))
        return err

    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_SYNCHRONIZE, channel_no)
    if err != ERROR_NONE:
        print("Syncing TEDS FAILED" + DeWeErrorConstantToString(err))
        return err

    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_WRITE, channel_no)
    if err != ERROR_NONE:
        print("Writing TEDS FAILED" + DeWeErrorConstantToString(err))
        return err

    # verify
    err = DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_READ, channel_no)
    if err != ERROR_NONE:
        print("Reading TEDS FAILED" + DeWeErrorConstantToString(err))
        return err

    err, ret = DeWeGetParamXML_str(target, "TEDSData/TEDSInfo/@Serial")
    if err == ERROR_NONE:
        print(ret)

    return ERROR_NONE


#----------------------------------------------------------------------
# main
if __name__ == "__main__":
    ret = main(sys.argv)
    sys.exit(ret)
