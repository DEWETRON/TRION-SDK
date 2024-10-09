"""
Copyright DEWETRON GmbH 2013

dewepxi_load module
"""


import ctypes
import os
import platform
import sys
from .dewepxi_apicore import *


if "64" in platform.architecture()[0]:
    if sys.platform.startswith("win"):
        TRION_DLL_NAME = "dwpxi_api_x64.dll"
        TRIONET_DLL_NAME = "dwpxi_netapi_x64.dll"
    elif sys.platform.startswith("linux"):
        TRION_DLL_NAME = "libdwpxi_api_x64.so"
        TRIONET_DLL_NAME = "libdwpxi_netapi_x64.so"
    elif sys.platform == "darwin":
        TRION_DLL_NAME = "libdwpxi_api_x64.dylib"
        TRIONET_DLL_NAME = "libdwpxi_netapi_x64.dylib"
    else:
        raise OSError("Unknown OS")
else:
    if sys.platform.startswith("win"):
        TRION_DLL_NAME = "dwpxi_api.dll"
        TRIONET_DLL_NAME = "dwpxi_netapi.dll"
    elif sys.platform.startswith("linux"):
        TRION_DLL_NAME = "libdwpxi_api.so"
        TRIONET_DLL_NAME = "libdwpxi_netapi.so"
    elif sys.platform == "darwin":
        TRION_DLL_NAME = "libdwpxi_api.dylib"
        TRIONET_DLL_NAME = "libdwpxi_netapi.dylib"
    else:
        raise OSError("Unknown OS")


g_trion_api_dll = None


def DeWePxiLoad(lib:str="TRION"):
    """
    Load the trion API dll and bind its public functions
    lib can be "TRION" or "TRIONET"
    """
    try:
        global g_trion_api_dll

        backend = "TRION"
        if lib == "TRION":
            dll_file = TRION_DLL_NAME
        elif lib == "TRIONET":
            dll_file = TRIONET_DLL_NAME
        else:
            raise ValueError(f"Given lib ({lib}) not supported")

        if os.name == "nt":
            try:
                script_name = sys.argv[0]
                path_name = os.path.dirname(script_name)
                located_at = os.path.normpath(os.path.join(path_name, dll_file))
                g_trion_api_dll = windll.LoadLibrary(located_at)
            except:
                g_trion_api_dll = windll.LoadLibrary(dll_file)
        elif TRION_DLL_NAME.endswith(".dll"):
            try:
                script_name = sys.argv[0]
                path_name = os.path.dirname(script_name)
                located_at = os.path.normpath(os.path.join(path_name, dll_file))
                g_trion_api_dll = cdll.LoadLibrary(located_at)
            except:
                g_trion_api_dll = cdll.LoadLibrary(dll_file)
        else:
            g_trion_api_dll = cdll.LoadLibrary(dll_file)

        # load dll functions

        # driver init
        SetDeWeDriverInit(g_trion_api_dll.DeWeDriverInit)
        SetDeWeDriverDeInit(g_trion_api_dll.DeWeDriverDeInit)

        # _i32 functions
        SetDeWeSetParam_i32(g_trion_api_dll.DeWeSetParam_i32)
        SetDeWeGetParam_i32(g_trion_api_dll.DeWeGetParam_i32)

        # _i64 functions
        SetDeWeSetParam_i64(g_trion_api_dll.DeWeSetParam_i64)
        SetDeWeGetParam_i64(g_trion_api_dll.DeWeGetParam_i64)

        # string based functions
        SetDeWeSetParamStruct_str(g_trion_api_dll.DeWeSetParamStruct_str)
        SetDeWeGetParamStruct_str(g_trion_api_dll.DeWeGetParamStruct_str)
        SetDeWeGetParamStruct_strLEN(g_trion_api_dll.DeWeGetParamStruct_strLEN)

        SetDeWeSetParamXML_str(g_trion_api_dll.DeWeSetParamXML_str)
        SetDeWeGetParamXML_str(g_trion_api_dll.DeWeGetParamXML_str)
        SetDeWeGetParamXML_strLEN(g_trion_api_dll.DeWeGetParamXML_strLEN)

        # CAN functions
        SetDeWeOpenCAN(g_trion_api_dll.DeWeOpenCAN)
        SetDeWeCloseCAN(g_trion_api_dll.DeWeCloseCAN)
        SetDeWeGetChannelPropCAN(g_trion_api_dll.DeWeGetChannelPropCAN)
        SetDeWeSetChannelPropCAN(g_trion_api_dll.DeWeSetChannelPropCAN)
        SetDeWeStartCAN(g_trion_api_dll.DeWeStartCAN)
        SetDeWeStopCAN(g_trion_api_dll.DeWeStopCAN)
        SetDeWeReadCAN(g_trion_api_dll.DeWeReadCAN)
        SetDeWeReadCANRawFrame(g_trion_api_dll.DeWeReadCANRawFrame)
        SetDeWeFreeFramesCAN(g_trion_api_dll.DeWeFreeFramesCAN)
        SetDeWeWriteCAN(g_trion_api_dll.DeWeWriteCAN)
        SetDeWeErrorCntCAN(g_trion_api_dll.DeWeErrorCntCAN)

        # Asynchronous channel(UART) functions
        SetDeWeOpenDmaUart(g_trion_api_dll.DeWeOpenDmaUart)
        SetDeWeCloseDmaUart(g_trion_api_dll.DeWeCloseDmaUart)
        SetDeWeGetChannelPropDmaUart(g_trion_api_dll.DeWeGetChannelPropDmaUart)
        SetDeWeSetChannelPropDmaUart(g_trion_api_dll.DeWeSetChannelPropDmaUart)
        SetDeWeStartDmaUart(g_trion_api_dll.DeWeStartDmaUart)
        SetDeWeStopDmaUart(g_trion_api_dll.DeWeStopDmaUart)
        SetDeWeReadDmaUart(g_trion_api_dll.DeWeReadDmaUart)
        SetDeWeReadDmaUartRawFrame(g_trion_api_dll.DeWeReadDmaUartRawFrame)
        SetDeWeWriteDmaUart(g_trion_api_dll.DeWeWriteDmaUart)

        # Obtain readable ErrorMessage from ErroCode
        g_trion_api_dll.DeWeErrorConstantToString.restype = ctypes.c_char_p
        SetDeWeErrorConstantToString(g_trion_api_dll.DeWeErrorConstantToString)

        # no test interface for now
    except Exception as err:
        print(err)
        return False
    return True


def DeWePxiUnload():
    """Unload the trion API dll"""
    try:
        global g_trion_api_dll
        DeWeDriverDeInit()

        SetDeWeDriverInit(None)
        SetDeWeDriverDeInit(None)

        SetDeWeSetParam_i32(None)
        SetDeWeGetParam_i32(None)

        SetDeWeSetParam_i64(None)
        SetDeWeGetParam_i64(None)

        SetDeWeSetParamStruct_str(None)
        SetDeWeGetParamStruct_str(None)
        SetDeWeGetParamStruct_strLEN(None)

        SetDeWeSetParamXML_str(None)
        SetDeWeGetParamXML_str(None)
        SetDeWeGetParamStruct_strLEN(None)

        SetDeWeOpenCAN(None)
        SetDeWeCloseCAN(None)
        SetDeWeGetChannelPropCAN(None)
        SetDeWeSetChannelPropCAN(None)
        SetDeWeStartCAN(None)
        SetDeWeStopCAN(None)
        SetDeWeReadCAN(None)
        SetDeWeReadCANRawFrame(None)
        SetDeWeWriteCAN(None)
        SetDeWeErrorCntCAN(None)

        SetDeWeOpenDmaUart(None)
        SetDeWeCloseDmaUart(None)
        SetDeWeGetChannelPropDmaUart(None)
        SetDeWeSetChannelPropDmaUart(None)
        SetDeWeStartDmaUart(None)
        SetDeWeStopDmaUart(None)
        SetDeWeReadDmaUart(None)
        SetDeWeReadDmaUartRawFrame(None)
        SetDeWeWriteDmaUart(None)

        SetDeWeErrorConstantToString(None)

        if os.name == "posix":
            print("Trying dll unload")
            try:
                handle = g_trion_api_dll._handle
                #ctypes._ctypes.dlclose(handle)
                #del g_trion_api_dll
                #g_trion_api_dll  = None
                #print("H:", handle)

                #libdl = cdll.LoadLibrary("libdl.so")
                #print(libdl)
                #if libdl != None:
                #    libdl.dlclose(c_void_p(handle))
                #else:
                #    print("Could not load libdl")
            except:
                print("Unloading failed")
    except:
        pass
