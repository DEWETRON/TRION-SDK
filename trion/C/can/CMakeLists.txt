#
# Project DEWETRON TRION SDK - CAN examples
#

add_executable(CANChannelDecoded
    CANChannelDecoded/can_channel_decoded.c
  )
SampleBuildSettings(CANChannelDecoded "can")

add_executable(CANChannelRaw
    CANChannelRaw/can_channel_raw.c
  )
SampleBuildSettings(CANChannelRaw "can")

add_executable(CANComplexExample
    CANComplexExample/can_complex_example.c
  )
SampleBuildSettings(CANComplexExample "can")

add_executable(CANWriteFrame
    CANWriteFrame/can_write_frame.c
  )
SampleBuildSettings(CANWriteFrame "can")
