# Copyright DEWETRON GmbH 2013
#
# dewepxi_api_core module
#


from dewepxi_const import *
from dewepxi_types import *
from ctypes import *

#
# Functors
f_dewe_driver_init    = None
f_dewe_driver_deinit  = None
f_dewe_set_param_i32  = None
f_dewe_get_param_i32  = None
f_dewe_set_param_i64  = None
f_dewe_get_param_i64  = None
f_dewe_set_param_struct_str = None
f_dewe_get_param_struct_str = None
f_dewe_get_param_struct_strlen = None

f_dewe_set_param_xml_str = None
f_dewe_get_param_xml_str = None
f_dewe_get_param_xml_strlen = None

f_dewe_open_can = None
f_dewe_close_can = None
f_dewe_get_channel_prop_can = None
f_dewe_set_channel_prop_can = None
f_dewe_start_can = None
f_dewe_stop_can = None
f_dewe_read_can = None
f_dewe_read_can_raw_frame = None
f_dewe_free_can_frames = None
f_dewe_write_can = None
f_dewe_error_cnt_can = None

f_dewe_open_dma_uart = None
f_dewe_close_dma_uart = None
f_dewe_get_channel_prop_dma_uart = None
f_dewe_set_channel_prop_dma_uart = None
f_dewe_start_dma_uart = None
f_dewe_stop_dma_uart = None
f_dewe_read_dma_uart = None
f_dewe_read_dma_uart_raw_frame = None
f_dewe_write_dma_uart = None

f_dewe_error_ant_to_string = None


__MAX_STRING_BUF = 256

#
# Setup functors
def SetDeWeDriverInit(func):
    """
    """
    global f_dewe_driver_init
    f_dewe_driver_init = func

def SetDeWeDriverDeInit(func):
    """
    """
    global f_dewe_driver_deinit
    f_dewe_driver_deinit = func

def SetDeWeSetParam_i32(func):
    """
    """
    global f_dewe_set_param_i32
    f_dewe_set_param_i32 = func

def SetDeWeGetParam_i32(func):
    """
    """
    global f_dewe_get_param_i32
    f_dewe_get_param_i32 = func

def SetDeWeSetParam_i64(func):
    """
    """
    global f_dewe_set_param_i64
    f_dewe_set_param_i64 = func

def SetDeWeGetParam_i64(func):
    """
    """
    global f_dewe_get_param_i64
    f_dewe_get_param_i64 = func

def SetDeWeSetParamStruct_str(func):
    """
    """
    global f_dewe_set_param_struct_str
    f_dewe_set_param_struct_str = func

def SetDeWeGetParamStruct_str(func):
    """
    """
    global f_dewe_get_param_struct_str
    f_dewe_get_param_struct_str = func

def SetDeWeGetParamStruct_strLEN(func):
    """
    """
    global f_dewe_get_param_struct_strlen
    f_dewe_get_param_struct_strlen = func

def SetDeWeSetParamXML_str(func):
    """
    """
    global f_dewe_set_param_xml_str
    f_dewe_set_param_xml_str = func

def SetDeWeGetParamXML_str(func):
    """
    """
    global f_dewe_get_param_xml_str
    f_dewe_get_param_xml_str = func

def SetDeWeGetParamXML_strLEN(func):
    """
    """
    global f_dewe_get_param_xml_strlen
    f_dewe_get_param_xml_strlen = func

def SetDeWeOpenCAN(func):
    """
    """
    global f_dewe_open_can
    f_dewe_open_can = func

def SetDeWeCloseCAN(func):
    """
    """
    global f_dewe_close_can
    f_dewe_close_can = func

def SetDeWeGetChannelPropCAN(func):
    """
    """
    global f_dewe_get_channel_prop_can
    f_dewe_get_channel_prop_can = func

def SetDeWeSetChannelPropCAN(func):
    """
    """
    global f_dewe_set_channel_prop_can
    f_dewe_set_channel_prop_can = func

def SetDeWeStartCAN(func):
    """
    """
    global f_dewe_start_can
    f_dewe_start_can = func

def SetDeWeStopCAN(func):
    """
    """
    global f_dewe_stop_can
    f_dewe_stop_can = func

def SetDeWeReadCAN(func):
    """
    """
    global f_dewe_read_can
    f_dewe_read_can = func

def SetDeWeReadCANRawFrame(func):
    """
    """
    global f_dewe_read_can_raw_frame
    f_dewe_read_can_raw_frame = func

def SetDeWeFreeFramesCAN(func):
    """
    """
    global f_dewe_free_can_frames
    f_dewe_free_can_frames = func


def SetDeWeWriteCAN(func):
    """
    """
    global f_dewe_write_can
    f_dewe_write_can = func

def SetDeWeErrorCntCAN(func):
    """
    """
    global f_dewe_error_cnt_can
    f_dewe_error_cnt_can = func

def SetDeWeOpenDmaUart(func):
    """
    """
    global f_dewe_open_dma_uart
    f_dewe_open_dma_uart = func

def SetDeWeCloseDmaUart(func):
    """
    """
    global f_dewe_close_dma_uart
    f_dewe_close_dma_uart = func

def SetDeWeGetChannelPropDmaUart(func):
    """
    """
    global f_dewe_get_channel_prop_dma_uart
    f_dewe_get_channel_prop_dma_uart = func

def SetDeWeSetChannelPropDmaUart(func):
    """
    """
    global f_dewe_set_channel_prop_dma_uart
    f_dewe_set_channel_prop_dma_uart = func

def SetDeWeStartDmaUart(func):
    """
    """
    global f_dewe_start_dma_uart
    f_dewe_start_dma_uart = func

def SetDeWeStopDmaUart(func):
    """
    """
    global f_dewe_stop_dma_uart
    f_dewe_stop_dma_uart = func

def SetDeWeReadDmaUart(func):
    """
    """
    global f_dewe_read_dma_uart
    f_dewe_read_dma_uart = func

def SetDeWeReadDmaUartRawFrame(func):
    """
    """
    global f_dewe_read_dma_uart_raw_frame
    f_dewe_read_dma_uart_raw_frame = func

def SetDeWeWriteDmaUart(func):
    """
    """
    global f_dewe_write_dma_uart
    f_dewe_write_dma_uart = func

def SetDeWeErrorConstantToString(func):
    """
    """
    global f_dewe_error_constant_to_string
    f_dewe_error_constant_to_string = func


#
# Exported API functions
def DeWeDriverInit():
    """
    DeWe Trion API Initialization
    """
    if f_dewe_driver_init != None:
        nNoOfBoards = c_int()
        ErrorCode = f_dewe_driver_init(byref(nNoOfBoards))
        return [ErrorCode, nNoOfBoards.value]
    else:
        return [-1, 0]

def DeWeDriverDeInit():
    """
    DeWe Trion API DeInitialization
    """
    if f_dewe_driver_deinit != None:
        ErrorCode = f_dewe_driver_deinit()
        return ErrorCode
    else:
        return -1


def DeWeSetParam_i32(nBoardNo, nCommandId, nVal):
    """
    """
    if f_dewe_set_param_i32 != None:
        board_id   = c_int(nBoardNo)
        command_id = c_uint(nCommandId)
        val        = c_int(nVal)

        #print(nBoardNo, nCommandId, nVal)
        #print(board_id, command_id, val)
        ErrorCode = f_dewe_set_param_i32(board_id, command_id, val)
        return ErrorCode
    else:
        return -1


def DeWeGetParam_i32(nBoardNo, nCommandId):
    """
    """
    if f_dewe_set_param_i32 != None:
        board_id   = c_int(nBoardNo)
        command_id = c_uint(nCommandId)
        val        = c_int()
        ErrorCode = f_dewe_get_param_i32(board_id, command_id, byref(val))
        return [ErrorCode, val.value]
    else:
        return [-2, 0]


def DeWeSetParam_i64(nBoardNo, nCommandId, nVal):
    """
    """
    if f_dewe_set_param_i64 != None:
        board_id   = c_int(nBoardNo)
        command_id = c_uint(nCommandId)
        val        = c_longlong(nVal)

        #print(nBoardNo, nCommandId, nVal)
        #print(board_id, command_id, val)
        ErrorCode = f_dewe_set_param_i64(board_id, command_id, val)
        return ErrorCode
    else:
        return -1


def DeWeGetParam_i64(nBoardNo, nCommandId):
    """
    """
    if f_dewe_set_param_i64 != None:
        board_id   = c_int(nBoardNo)
        command_id = c_uint(nCommandId)
        val        = c_longlong()
        ErrorCode = f_dewe_get_param_i64(board_id, command_id, byref(val))
        return [ErrorCode, val.value]
    else:
        return [-3, 0]


def DeWeSetParamStruct_str(Target, Command, Var):
    """
    """
    if f_dewe_set_param_struct_str != None:
        ErrorCode = f_dewe_set_param_struct_str(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), c_char_p(Var.encode('utf-8')))
        return ErrorCode
    else:
        return -1

def DeWeGetParamStruct_str(Target, Command):
    """
    """
    if f_dewe_get_param_struct_str != None:
        s = create_string_buffer(__MAX_STRING_BUF)
        ErrorCode = f_dewe_get_param_struct_str(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), s, c_int(__MAX_STRING_BUF))

        if ErrorCode == 230001:
            [ErrorCode, result_len] = DeWeGetParamStruct_strLEN(Target, Command)
            s = create_string_buffer(result_len+2)
            ErrorCode = f_dewe_get_param_struct_str(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), s, c_int(result_len+1))
        return [ErrorCode, bytes.decode(s.value, 'utf-8')]
    else:
        return [-1, ""]

def DeWeGetParamStruct_strLEN(Target, Command):
    """
    """
    if f_dewe_get_param_struct_strlen != None:
        Len = c_int()
        ErrorCode = f_dewe_get_param_struct_strlen(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), byref(Len))
        return [ErrorCode, Len.value]
    else:
        return [-1, 0]


def DeWeSetParamXML_str(Target, Command, Var):
    """
    """
    if f_dewe_set_param_xml_str != None:
        ErrorCode = f_dewe_set_param_xml_str(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), c_char_p(Var.encode('utf-8')))
        return ErrorCode
    else:
        return -1

def DeWeGetParamXML_str(Target, Command):
    """
    """
    if f_dewe_get_param_xml_str != None:
        s = create_string_buffer(__MAX_STRING_BUF)
        ErrorCode = f_dewe_get_param_xml_str(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), s, c_int(__MAX_STRING_BUF))

        if ErrorCode == 230001:
            [ErrorCode, result_len] = DeWeGetParamXML_strLEN(Target, Command)
            s = create_string_buffer(result_len+2)
            ErrorCode = f_dewe_get_param_xml_str(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), s, c_int(result_len+1))
        return [ErrorCode, bytes.decode(s.value, 'utf-8')]
    else:
        return [-1, ""]

def DeWeGetParamXML_strLEN(Target, Command):
    """
    """
    if f_dewe_get_param_xml_strlen != None:
        Len = c_int()
        ErrorCode = f_dewe_get_param_xml_strlen(c_char_p(Target.encode('utf-8')), c_char_p(Command.encode('utf-8')), byref(Len))
        return [ErrorCode, Len.value]
    else:
        return [-1, 0]


#
# CAN API

def DeWeOpenCAN(nBoardNo):
    """
    """
    if f_dewe_open_can != None:
        ErrorCode = f_dewe_open_can(c_int(nBoardNo))
        return ErrorCode
    else:
        return -1

def DeWeCloseCAN(nBoardNo):
    """
    """
    if f_dewe_close_can != None:
        ErrorCode = f_dewe_close_can(c_int(nBoardNo))
        return ErrorCode
    else:
        return -1

def DeWeGetChannelPropCAN(nBoardNo, nChannelNo):
    """
    """
    if f_dewe_get_channel_prop_can != None:
        cProp = BOARD_CAN_CHANNEL_PROP()
        ErrorCode = f_dewe_get_channel_prop_can(c_int(nBoardNo), c_int(nChannelNo), byref(cProp))
        return [ErrorCode, cProp]
    else:
        return [-1, BOARD_CAN_CHANNEL_PROP()]

def DeWeSetChannelPropCAN(nBoardNo, nChannelNo, cProp):
    """
    """
    if f_dewe_set_channel_prop_can != None:
        ErrorCode = f_dewe_set_channel_prop_can(c_int(nBoardNo), c_int(nChannelNo), cProp)
        return ErrorCode
    else:
        return -1

def DeWeStartCAN(nBoardNo, nChannelNo):
    """
    """
    if f_dewe_start_can != None:
        ErrorCode = f_dewe_start_can(c_int(nBoardNo), c_int(nChannelNo))
        return ErrorCode
    else:
        return -1

def DeWeStopCAN(nBoardNo, nChannelNo):
    """
    """
    if f_dewe_stop_can != None:
        ErrorCode = f_dewe_stop_can(c_int(nBoardNo), c_int(nChannelNo))
        return ErrorCode
    else:
        return -1

def DeWeReadCAN(nBoardNo, nMaxFrameCount):
    """
    """
    if f_dewe_read_can != None:
        CanFrameArrayType = BOARD_CAN_FRAME * nMaxFrameCount
        pCanFrames = CanFrameArrayType()
        nRealFrameCount = c_int()
        ErrorCode = f_dewe_read_can(c_int(nBoardNo), byref(pCanFrames), c_int(nMaxFrameCount), byref(nRealFrameCount))
        return [ErrorCode, pCanFrames, nRealFrameCount.value]
    else:
        return [-1, BOARD_CAN_FRAME()]

def DeWeReadCANRawFrame(nBoardNo):
    """
    """
    if f_dewe_read_can_raw_frame != None:
        #f_dewe_read_can_raw_frame.argtypes = [c_int, POINTER(POINTER(BOARD_CAN_RAW_FRAME)), POINTER(c_int)]
        #f_dewe_read_can_raw_frame.restype  = c_int
        pCanFrames = POINTER(BOARD_CAN_RAW_FRAME)()
        nRealFrameCount = c_int(0)
        ErrorCode = f_dewe_read_can_raw_frame(c_int(nBoardNo), byref(pCanFrames), byref(nRealFrameCount))

        return [ErrorCode, pCanFrames, nRealFrameCount.value]
    else:
        return [-1, None]

def DeWeFreeFramesCAN(nBoardNo, nFrameCount):
    """
    """
    if f_dewe_free_can_frames != None:
        ErrorCode = f_dewe_free_can_frames(c_int(nBoardNo), c_int(nFrameCount))
        return ErrorCode
    else:
        return -1


def DeWeWriteCAN(nBoardNo, CanFrameList):
    """
    """
    if f_dewe_write_can != None:
        nRealFrameCount = c_int()
        nFrameCount = len(CanFrameList)
        CanFrameArrayType = BOARD_CAN_FRAME * nFrameCount
        pCanFrames = CanFrameArrayType()

        i = 0
        for CanFrame in CanFrameList:
          pCanFrames[i] = CanFrame
          i+=1

        ErrorCode = f_dewe_write_can(c_int(nBoardNo), byref(pCanFrames), c_int(nFrameCount), byref(nRealFrameCount))
        return [ErrorCode, nRealFrameCount.value]
    else:
        return [-1, 0]

def DeWeErrorCntCAN(nBoardNo, nChannelNo):
    """
    """
    if f_dewe_error_cnt_can != None:
        nErrorCount = c_int()
        ErrorCode = f_dewe_error_cnt_can(c_int(nBoardNo), c_int(nChannelNo), byref(nErrorCount))
        return [ErrorCode, nErrorCount]
    else:
        return [-1, 0]


#
# Async UART API

def DeWeOpenDmaUart(nBoardNo):
    """
    """
    if f_dewe_open_dma_uart != None:
        ErrorCode = f_dewe_open_dma_uart(c_int(nBoardNo))
        return ErrorCode
    else:
        return -1

def DeWeCloseDmaUart(nBoardNo):
    """
    """
    if f_dewe_close_dma_uart != None:
        ErrorCode = f_dewe_close_dma_uart(c_int(nBoardNo))
        return ErrorCode
    else:
        return -1

def DeWeGetChannelPropDmaUart(nBoardNo, nChannelNo):
    """
    """
    if f_dewe_get_channel_prop_dma_uart != None:
        pProp = BOARD_UART_CHANNEL_PROP()
        ErrorCode = f_dewe_get_channel_prop_dma_uart(c_int(nBoardNo), c_int(nChannelNo), byref(pProp))
        return [ErrorCode, pProp]
    else:
        return [-1, BOARD_UART_CHANNEL_PROP()]

def DeWeSetChannelPropDmaUart(nBoardNo, nChannelNo, pProp):
    """
    """
    if f_dewe_set_channel_prop_dma_uart != None:
        ErrorCode = f_dewe_set_channel_prop_dma_uart(c_int(nBoardNo), c_int(nChannelNo), pProp)
        return ErrorCode
    else:
        return -1

def DeWeStartDmaUart(nBoardNo, nChannelNo):
    """
    """
    if f_dewe_start_dma_uart != None:
        ErrorCode = f_dewe_start_dma_uart(c_int(nBoardNo), c_int(nChannelNo))
        return ErrorCode
    else:
        return -1

def DeWeStopDmaUart(nBoardNo, nChannelNo):
    """
    """
    if f_dewe_stop_dma_uart != None:
        ErrorCode = f_dewe_stop_dma_uart(c_int(nBoardNo), c_int(nChannelNo))
        return ErrorCode
    else:
        return -1

def DeWeReadDmaUart(nBoardNo, nMaxFrameCount):
    """
    """
    if f_dewe_read_dma_uart != None:
        UartFrameArrayType = BOARD_UART_FRAME * nMaxFrameCount
        pUartFrames = UartFrameArrayType()
        nRealFrameCount = c_int()
        ErrorCode = f_dewe_read_dma_uart(c_int(nBoardNo), byref(pUartFrames), c_int(nMaxFrameCount), byref(nRealFrameCount))
        return [ErrorCode, pUartFrames, nRealFrameCount.value]
    else:
        return [-1, BOARD_UART_FRAME(), 0]


def DeWeReadDmaUartRawFrame(nBoardNo):
    """
    """
    if f_dewe_read_dma_uart_raw_frame != None:
        f_dewe_read_dma_uart_raw_frame.argtypes = [c_int, POINTER(POINTER(BOARD_UART_RAW_FRAME)), POINTER(c_int)]
        f_dewe_read_dma_uart_raw_frame.restype  = c_int
        pUartFrames = POINTER(BOARD_UART_RAW_FRAME)()
        nRealFrameCount = c_int(0)
        ErrorCode = f_dewe_read_dma_uart_raw_frame(c_int(nBoardNo), byref(pUartFrames), byref(nRealFrameCount))
        return [ErrorCode, pUartFrames]
    else:
        return [-1, None]

def DeWeWriteDmaUart(nBoardNo, pUartFrames, nFrameCount):
    """
    """
    if f_dewe_write_dma_uart != None:
        nRealFrameCount = c_int()
        ErrorCode = f_dewe_write_dma_uart(c_int(nBoardNo), pUartFrames, c_int(nFrameCount), byref(nRealFrameCount))
        return [ErrorCode, nRealFrameCount]
    else:
        return [-1, 0]

# Obtain readable ErrorMessage from ErrorCode
def DeWeErrorConstantToString(ErrorCode):
    """
    """
    ErrString = ""
    if f_dewe_error_constant_to_string != None:
        raw = c_char_p(f_dewe_error_constant_to_string(c_int(ErrorCode)))
        for c in raw.value:
          ErrString = ErrString + str(chr(c))
        return ErrString
    else:
        return ""


#
# API helper
def DeWeGetSampleData(nReadPos):
    """
    Raw access the DMA buffer at the given nReadPos.
    """
    p = cast(nReadPos, POINTER(c_int))
    return p[0]
    #return 1

def DeWeGetSampleDataArray(nReadPos):
    """
    Raw access the DMA buffer at the given nReadPos.
    """
    p = cast(nReadPos, POINTER(c_int))
    return p
    #return 1

def DeWeGetSampleArray(nReadPos, res):
    """
    Raw access the DMA buffer at the given nReadPos.
    """
    if res == 24 or res == '24':
        p = cast(nReadPos, POINTER(c_int))
    elif res == 16 or res == '16':
        p = cast(nReadPos, POINTER(c_short))
    return p