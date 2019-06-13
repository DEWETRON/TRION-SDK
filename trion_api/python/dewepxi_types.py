# Copyright DEWETRON GmbH 2013
#
# dewepxi_types module
#


from ctypes import *

class BOARD_CAN_CHANNEL_PROP(Structure):
    """
    """
    _fields_ = [("Used", c_uint),
                ("Mode", c_uint),
                ("BaudRate", c_uint),
                ("ListenOnly", c_uint),
                ("Termination", c_uint),
                ("PhyLayer", c_uint),
                ("SyncCounter", c_uint),
                ("Sjw", c_ubyte),
                ("Tseg1", c_ubyte),
                ("Tseg2", c_ubyte),
                ("Sam", c_ubyte)]


class BOARD_CAN_FRAME(Structure):
    """
    """
    _fields_ = [("CanNo", c_ubyte),
                ("RFU1", c_ubyte),
                ("RFU2", c_ubyte),
                ("RFU3", c_ubyte),
                ("MessageId", c_uint),
                ("DataLength", c_uint),
                ("CanData", c_ubyte * 8),
                ("StandardExtended", c_uint),
                ("FrameType",c_uint),
                ("SyncCounter",c_uint),
                ("ErrorCounter",c_uint),
                ("TimeStampEx",c_uint64)
                ]


class BOARD_CAN_RAW_FRAME(Structure):
    """
    """
    _fields_ = [("Hdr", c_uint),
                ("Err", c_uint),
                ("Pos", c_uint),
                ("Data", c_ubyte * 8),
                ("Dummy", c_uint * 3)]



class BOARD_UART_CHANNEL_PROP(Structure):
    """
    """
    _fields_ = [("Used", c_uint),
                ("Mode", c_uint),
                ("BaudRate", c_uint),
                ("DataBit", c_uint),
                ("StopBit", c_uint),
                ("Handshake", c_uint),
                ("SyncCounter", c_uint)]


class BOARD_UART_FRAME(Structure):
    """
    """
    _fields_ = [("Data", c_ubyte),
                ("RFU1", c_ubyte),
                ("RFU2", c_ubyte),
                ("RFU3", c_ubyte),
                ("UartNo", c_uint),
                ("LastPPS", c_uint64),
                ("SyncCounter", c_uint64)]


class BOARD_UART_RAW_FRAME(Structure):
    """
    """
    _fields_ = [("Minute", c_ubyte),
                ("Second", c_ubyte),
                ("Data", c_ubyte),
                ("Hour", c_ubyte),
                ("SyncCounter", c_ushort),
                ("PPSCounter", c_ushort)]
