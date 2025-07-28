#
# Project DEWETRON TRION SDK - analog examples
#

add_executable(OneAnalogChannel
  OneAnalogChannel/one_analog_channel.cs
  )
SampleBuildSettings(OneAnalogChannel "analog")

add_executable(MultiBoardAnalogChannels
  MultiBoardAnalogChannels/multi_board_analog_channels.cs
  )
SampleBuildSettings(MultiBoardAnalogChannels "analog")

add_executable(ADCDelay
  ADCDelay/adc_delay.cs
  )
SampleBuildSettings(ADCDelay "analog")