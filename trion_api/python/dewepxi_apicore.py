"""
Copyright DEWETRON GmbH 2013

dewepxi_api_core module
"""


from ctypes import *
from .dewepxi_const import *
from .dewepxi_errorcodes import *
from .dewepxi_types import *


# Functors
f_dewe_driver_init = None
f_dewe_driver_deinit = None
f_dewe_set_param_i32 = None
f_dewe_get_param_i32 = None
f_dewe_set_param_i64 = None
f_dewe_get_param_i64 = None
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

f_dewe_error_constant_to_string = None

__MAX_STRING_BUF = 256


# Setup functors
def SetDeWeDriverInit(func):
    """Set dewe driver init"""
    global f_dewe_driver_init
    f_dewe_driver_init = func


def SetDeWeDriverDeInit(func):
    """Set dewe driver de-init"""
    global f_dewe_driver_deinit
    f_dewe_driver_deinit = func


def SetDeWeSetParam_i32(func):
    """Set dewe set param i32"""
    global f_dewe_set_param_i32
    f_dewe_set_param_i32 = func


def SetDeWeGetParam_i32(func):
    """Set dewe get param i32"""
    global f_dewe_get_param_i32
    f_dewe_get_param_i32 = func


def SetDeWeSetParam_i64(func):
    """Set dewe set param i64"""
    global f_dewe_set_param_i64
    f_dewe_set_param_i64 = func


def SetDeWeGetParam_i64(func):
    """Set dewe get param i64"""
    global f_dewe_get_param_i64
    f_dewe_get_param_i64 = func


def SetDeWeSetParamStruct_str(func):
    """Set dewe set param struct str"""
    global f_dewe_set_param_struct_str
    f_dewe_set_param_struct_str = func


def SetDeWeGetParamStruct_str(func):
    """Set dewe get param struct str"""
    global f_dewe_get_param_struct_str
    f_dewe_get_param_struct_str = func


def SetDeWeGetParamStruct_strLEN(func):
    """Set dewe get param struct str len"""
    global f_dewe_get_param_struct_strlen
    f_dewe_get_param_struct_strlen = func


def SetDeWeSetParamXML_str(func):
    """Set dewe set param XML str"""
    global f_dewe_set_param_xml_str
    f_dewe_set_param_xml_str = func


def SetDeWeGetParamXML_str(func):
    """Set dewe get param XML str"""
    global f_dewe_get_param_xml_str
    f_dewe_get_param_xml_str = func


def SetDeWeGetParamXML_strLEN(func):
    """Set dewe get param XML str len"""
    global f_dewe_get_param_xml_strlen
    f_dewe_get_param_xml_strlen = func


def SetDeWeOpenCAN(func):
    """Set dewe open CAN"""
    global f_dewe_open_can
    f_dewe_open_can = func


def SetDeWeCloseCAN(func):
    """Set dewe close CAN"""
    global f_dewe_close_can
    f_dewe_close_can = func


def SetDeWeGetChannelPropCAN(func):
    """Set dewe get channel prop CAN"""
    global f_dewe_get_channel_prop_can
    f_dewe_get_channel_prop_can = func


def SetDeWeSetChannelPropCAN(func):
    """Set dewe set channel prop CAN"""
    global f_dewe_set_channel_prop_can
    f_dewe_set_channel_prop_can = func


def SetDeWeStartCAN(func):
    """Set dewe start CAN"""
    global f_dewe_start_can
    f_dewe_start_can = func


def SetDeWeStopCAN(func):
    """Set dewe stop CAN"""
    global f_dewe_stop_can
    f_dewe_stop_can = func


def SetDeWeReadCAN(func):
    """Set dewe read CAN"""
    global f_dewe_read_can
    f_dewe_read_can = func


def SetDeWeReadCANRawFrame(func):
    """Set dewe read CAN raw frame"""
    global f_dewe_read_can_raw_frame
    f_dewe_read_can_raw_frame = func


def SetDeWeFreeFramesCAN(func):
    """Set dewe free frames CAN"""
    global f_dewe_free_can_frames
    f_dewe_free_can_frames = func


def SetDeWeWriteCAN(func):
    """Set dewe write CAN"""
    global f_dewe_write_can
    f_dewe_write_can = func


def SetDeWeErrorCntCAN(func):
    """Set dewe error cnt CAN"""
    global f_dewe_error_cnt_can
    f_dewe_error_cnt_can = func


def SetDeWeOpenDmaUart(func):
    """Set dewe Open dma uart"""
    global f_dewe_open_dma_uart
    f_dewe_open_dma_uart = func


def SetDeWeCloseDmaUart(func):
    """Set dewe close dma uart"""
    global f_dewe_close_dma_uart
    f_dewe_close_dma_uart = func


def SetDeWeGetChannelPropDmaUart(func):
    """Set dewe get channel prop dma uart"""
    global f_dewe_get_channel_prop_dma_uart
    f_dewe_get_channel_prop_dma_uart = func


def SetDeWeSetChannelPropDmaUart(func):
    """Set dewe set channel prop dma uart"""
    global f_dewe_set_channel_prop_dma_uart
    f_dewe_set_channel_prop_dma_uart = func


def SetDeWeStartDmaUart(func):
    """Set dewe start dma uart"""
    global f_dewe_start_dma_uart
    f_dewe_start_dma_uart = func


def SetDeWeStopDmaUart(func):
    """Set dewe stop dma uart"""
    global f_dewe_stop_dma_uart
    f_dewe_stop_dma_uart = func


def SetDeWeReadDmaUart(func):
    """Set dewe read dma uart"""
    global f_dewe_read_dma_uart
    f_dewe_read_dma_uart = func


def SetDeWeReadDmaUartRawFrame(func):
    """Set dewe read dma uart raw frame"""
    global f_dewe_read_dma_uart_raw_frame
    f_dewe_read_dma_uart_raw_frame = func


def SetDeWeWriteDmaUart(func):
    """Set dewe write dma uart"""
    global f_dewe_write_dma_uart
    f_dewe_write_dma_uart = func


def SetDeWeErrorConstantToString(func):
    """Set dewe error constant to string"""
    global f_dewe_error_constant_to_string
    f_dewe_error_constant_to_string = func


# Exported API functions
def DeWeDriverInit() -> tuple[int, int]:
    """DeWe Trion API Initialization

    Returns
    -------
    tuple with error code and number of detected boards
    """
    if f_dewe_driver_init is None:
        raise NotImplementedError
    no_of_boards = c_int()
    error_code = f_dewe_driver_init(byref(no_of_boards))
    return error_code, no_of_boards.value

def DeWeDriverDeInit() -> int:
    """DeWe Trion API DeInitialization"""
    if f_dewe_driver_deinit is None:
        raise NotImplementedError
    return f_dewe_driver_deinit()

def DeWeSetParam_i32(nBoardNo: int, nCommandId: int, nVal: int = 0) -> int:
    """Dewe set param i32"""
    if f_dewe_set_param_i32 is None:
        raise NotImplementedError
    board_id = c_int(nBoardNo)
    command_id = c_uint(nCommandId)
    val = c_int(nVal)

    return f_dewe_set_param_i32(board_id, command_id, val)

def DeWeGetParam_i32(nBoardNo: int, nCommandId: int) -> tuple[int, int]:
    """Dewe get param i32"""
    if f_dewe_set_param_i32 is None:
        raise NotImplementedError
    board_id = c_int(nBoardNo)
    command_id = c_uint(nCommandId)
    val = c_int()
    error_code = f_dewe_get_param_i32(board_id, command_id, byref(val))
    return error_code, val.value

def DeWeSetParam_i64(nBoardNo: int, nCommandId: int, nVal: int = 0) -> int:
    """Dewe set param i64"""
    if f_dewe_set_param_i64 is None:
        raise NotImplementedError
    board_id = c_int(nBoardNo)
    command_id = c_uint(nCommandId)
    val = c_longlong(nVal)
    return f_dewe_set_param_i64(board_id, command_id, val)

def DeWeGetParam_i64(nBoardNo: int, nCommandId: int) -> tuple[int, int]:
    """Dewe get param i64"""
    if f_dewe_set_param_i64 is None:
        raise NotImplementedError
    board_id = c_int(nBoardNo)
    command_id = c_uint(nCommandId)
    val = c_longlong()
    error_code = f_dewe_get_param_i64(board_id, command_id, byref(val))
    return error_code, val.value

def DeWeSetParamStruct_str(Target: str, Command: str, Var: str) -> int:
    """Dewe set param struct str"""
    if f_dewe_set_param_struct_str is None:
        raise NotImplementedError
    return f_dewe_set_param_struct_str(
        c_char_p(Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), c_char_p(Var.encode("utf-8"))
    )

def DeWeGetParamStruct_str(Target: str, Command: str) -> tuple[int, str]:
    """Dewe get param struct str"""
    if f_dewe_get_param_struct_str is None:
        raise NotImplementedError
    s = create_string_buffer(__MAX_STRING_BUF)
    error_code = f_dewe_get_param_struct_str(
        c_char_p(Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), s, c_int(__MAX_STRING_BUF)
    )

    if error_code == ERROR_BUFFER_TOO_SMALL:
        error_code, result_len = DeWeGetParamStruct_strLEN(Target, Command)
        s = create_string_buffer(result_len + 2)
        error_code = f_dewe_get_param_struct_str(
            c_char_p(Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), s, c_int(result_len + 1)
        )
    return error_code, bytes.decode(s.value, "utf-8")

def DeWeGetParamStruct_strLEN(Target: str, Command: str) -> tuple[int, int]:
    """Dewe get param struct str len"""
    if f_dewe_get_param_struct_strlen is None:
        raise NotImplementedError
    val = c_int()
    error_code = f_dewe_get_param_struct_strlen(
        c_char_p(Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), byref(val)
    )
    return error_code, val.value

def DeWeSetParamXML_str(Target: str, Command: str, Var: str) -> int:
    """Dewe set param XML str"""
    if f_dewe_set_param_xml_str is None:
        raise NotImplementedError
    return f_dewe_set_param_xml_str(c_char_p(
        Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), c_char_p(Var.encode("utf-8"))
    )

def DeWeGetParamXML_str(Target: str, Command: str) -> tuple[int, str]:
    """Dewe get param XML str"""
    if f_dewe_get_param_xml_str is None:
        raise NotImplementedError
    s = create_string_buffer(__MAX_STRING_BUF)
    error_code = f_dewe_get_param_xml_str(c_char_p(
        Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), s, c_int(__MAX_STRING_BUF)
    )

    if error_code == ERROR_BUFFER_TOO_SMALL:
        error_code, result_len = DeWeGetParamXML_strLEN(Target, Command)
        s = create_string_buffer(result_len + 2)
        error_code = f_dewe_get_param_xml_str(c_char_p(
            Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), s, c_int(result_len + 1)
        )
    return error_code, bytes.decode(s.value, "utf-8")

def DeWeGetParamXML_strLEN(Target: str, Command: str) -> tuple[int, int]:
    """Dewe get param XML str len"""
    if f_dewe_get_param_xml_strlen is None:
        raise NotImplementedError
    val = c_int()
    error_code = f_dewe_get_param_xml_strlen(
        c_char_p(Target.encode("utf-8")), c_char_p(Command.encode("utf-8")), byref(val)
    )
    return error_code, val.value


# CAN API
def DeWeOpenCAN(nBoardNo: int) -> int:
    """Dewe open CAN"""
    if f_dewe_open_can is None:
        raise NotImplementedError
    return f_dewe_open_can(c_int(nBoardNo))

def DeWeCloseCAN(nBoardNo: int) -> int:
    """Dewe close CAN"""
    if f_dewe_close_can is None:
        raise NotImplementedError
    return f_dewe_close_can(c_int(nBoardNo))

def DeWeGetChannelPropCAN(nBoardNo: int, nChannelNo: int):
    """Dewe get channel prop CAN"""
    if f_dewe_get_channel_prop_can is None:
        raise NotImplementedError
    c_prop = BOARD_CAN_CHANNEL_PROP()
    error_code = f_dewe_get_channel_prop_can(c_int(nBoardNo), c_int(nChannelNo), byref(c_prop))
    return error_code, c_prop

def DeWeSetChannelPropCAN(nBoardNo: int, nChannelNo: int, cProp) -> int:
    """Dewe set channel prop CAN"""
    if f_dewe_set_channel_prop_can is None:
        raise NotImplementedError
    return f_dewe_set_channel_prop_can(c_int(nBoardNo), c_int(nChannelNo), cProp)

def DeWeStartCAN(nBoardNo: int, nChannelNo: int) -> int:
    """Dewe start CAN"""
    if f_dewe_start_can is None:
        raise NotImplementedError
    return f_dewe_start_can(c_int(nBoardNo), c_int(nChannelNo))

def DeWeStopCAN(nBoardNo: int , nChannelNo: int) -> int:
    """Dewe stop CAN"""
    if f_dewe_stop_can is None:
        raise NotImplementedError
    return f_dewe_stop_can(c_int(nBoardNo), c_int(nChannelNo))

def DeWeReadCAN(nBoardNo: int, nMaxFrameCount: int):
    """Dewe read CAN"""
    if f_dewe_read_can is None:
        raise NotImplementedError
    p_can_frames = (BOARD_CAN_FRAME * nMaxFrameCount)()
    n_real_frame_count = c_int()
    error_code = f_dewe_read_can(
        c_int(nBoardNo), byref(p_can_frames), c_int(nMaxFrameCount), byref(n_real_frame_count)
    )
    return error_code, p_can_frames, n_real_frame_count.value

def DeWeReadCANRawFrame(nBoardNo: int):
    """Dewe read CAN raw frame"""
    if f_dewe_read_can_raw_frame is None:
        raise NotImplementedError
    # f_dewe_read_can_raw_frame.argtypes = [c_int, POINTER(POINTER(BOARD_CAN_RAW_FRAME)), POINTER(c_int)]
    # f_dewe_read_can_raw_frame.restype  = c_int
    p_can_frames = POINTER(BOARD_CAN_RAW_FRAME)()
    n_real_frame_count = c_int(0)
    error_code = f_dewe_read_can_raw_frame(c_int(nBoardNo), byref(p_can_frames), byref(n_real_frame_count))

    return error_code, p_can_frames, n_real_frame_count.value

def DeWeFreeFramesCAN(nBoardNo: int, nFrameCount: int) -> int:
    """Dewe free frames CAN"""
    if f_dewe_free_can_frames is None:
        raise NotImplementedError
    return f_dewe_free_can_frames(c_int(nBoardNo), c_int(nFrameCount))

def DeWeWriteCAN(nBoardNo: int, CanFrameList: list) -> tuple[int, int]:
    """Dewe write CAN"""
    if f_dewe_write_can is None:
        raise NotImplementedError

    n_real_frame_count = c_int()
    n_frame_count = len(CanFrameList)
    p_can_frames = (BOARD_CAN_FRAME * n_frame_count)()

    i = 0
    for CanFrame in CanFrameList:
        p_can_frames[i] = CanFrame
        i += 1

    error_code = f_dewe_write_can(c_int(
        nBoardNo), byref(p_can_frames), c_int(n_frame_count), byref(n_real_frame_count)
    )
    return error_code, n_real_frame_count.value

def DeWeErrorCntCAN(nBoardNo: int, nChannelNo: int) -> tuple[int, int]:
    """Dewe error cnt CAN"""
    if f_dewe_error_cnt_can is None:
        raise NotImplementedError
    n_error_count = c_int()
    error_code = f_dewe_error_cnt_can(c_int(nBoardNo), c_int(nChannelNo), byref(n_error_count))
    return error_code, n_error_count

# Async UART API
def DeWeOpenDmaUart(nBoardNo: int) -> int:
    """Dewe open dmd uart"""
    if f_dewe_open_dma_uart is None:
        raise NotImplementedError
    return f_dewe_open_dma_uart(c_int(nBoardNo))

def DeWeCloseDmaUart(nBoardNo: int) -> int:
    """Dewe close dma uart"""
    if f_dewe_close_dma_uart is None:
        raise NotImplementedError
    return f_dewe_close_dma_uart(c_int(nBoardNo))

def DeWeGetChannelPropDmaUart(nBoardNo: int, nChannelNo: int):
    """Dewe get channel prop dma uart"""
    if f_dewe_get_channel_prop_dma_uart is None:
        raise NotImplementedError
    p_prop = BOARD_UART_CHANNEL_PROP()
    error_code = f_dewe_get_channel_prop_dma_uart(c_int(nBoardNo), c_int(nChannelNo), byref(p_prop))
    return error_code, p_prop

def DeWeSetChannelPropDmaUart(nBoardNo: int, nChannelNo: int, pProp) -> int:
    """Dewe set channel prop dma uart"""
    if f_dewe_set_channel_prop_dma_uart is None:
        raise NotImplementedError
    return f_dewe_set_channel_prop_dma_uart(c_int(nBoardNo), c_int(nChannelNo), pProp)

def DeWeStartDmaUart(nBoardNo: int, nChannelNo: int) -> int:
    """Dewe start dma uart"""
    if f_dewe_start_dma_uart is None:
        raise NotImplementedError
    return f_dewe_start_dma_uart(c_int(nBoardNo), c_int(nChannelNo))

def DeWeStopDmaUart(nBoardNo: int, nChannelNo: int) -> int:
    """Dewe stop dma uart"""
    if f_dewe_stop_dma_uart is None:
        raise NotImplementedError
    return f_dewe_stop_dma_uart(c_int(nBoardNo), c_int(nChannelNo))

def DeWeReadDmaUart(nBoardNo: int, nMaxFrameCount: int):
    """Dewe read dma uart"""
    if f_dewe_read_dma_uart is None:
        raise NotImplementedError
    p_uart_frames = (BOARD_UART_FRAME * nMaxFrameCount)()
    n_real_frame_count = c_int()
    error_code = f_dewe_read_dma_uart(
        c_int(nBoardNo), byref(p_uart_frames), c_int(nMaxFrameCount), byref(n_real_frame_count)
    )
    return error_code, p_uart_frames, n_real_frame_count.value

def DeWeReadDmaUartRawFrame(nBoardNo: int):
    """Dewe read dma uart raw frame"""
    if f_dewe_read_dma_uart_raw_frame is None:
        raise NotImplementedError
    f_dewe_read_dma_uart_raw_frame.argtypes = [c_int, POINTER(POINTER(BOARD_UART_RAW_FRAME)), POINTER(c_int)]
    f_dewe_read_dma_uart_raw_frame.restype = c_int
    p_uart_frames = POINTER(BOARD_UART_RAW_FRAME)()
    n_real_frame_count = c_int(0)
    error_code = f_dewe_read_dma_uart_raw_frame(c_int(nBoardNo), byref(p_uart_frames), byref(n_real_frame_count))
    return error_code, p_uart_frames

def DeWeWriteDmaUart(nBoardNo: int, pUartFrames, nFrameCount: int) -> tuple[int, int]:
    """Dewe write dma uart"""
    if f_dewe_write_dma_uart is None:
        raise NotImplementedError
    n_real_frame_count = c_int()
    error_code = f_dewe_write_dma_uart(c_int(nBoardNo), pUartFrames, c_int(nFrameCount), byref(n_real_frame_count))
    return error_code, n_real_frame_count

def DeWeErrorConstantToString(ErrorCode: int) -> str:
    """Obtain readable ErrorMessage from ErrorCode"""
    if f_dewe_error_constant_to_string is None:
        raise NotImplementedError
    raw = c_char_p(f_dewe_error_constant_to_string(c_int(ErrorCode)))
    return raw.value.decode("utf-8")

# API helper
def DeWeGetSampleData(nReadPos: int):
    """Raw access the DMA buffer at the given nReadPos."""
    p = cast(nReadPos, POINTER(c_int))
    return p[0]
    # return 1


def DeWeGetSampleDataArray(nReadPos: int):
    """Raw access the DMA buffer at the given nReadPos."""
    p = cast(nReadPos, POINTER(c_int))
    return p
    # return 1


def DeWeGetSampleArray(nReadPos: int, res: int | str):
    """Raw access the DMA buffer at the given nReadPos."""
    p = None
    if res in (24, "24"):
        p = cast(nReadPos, POINTER(c_int))
    elif res in (16, "16"):
        p = cast(nReadPos, POINTER(c_short))
    return p
