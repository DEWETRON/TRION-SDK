#
# Project DEWETRON TRION SDK - discret examples
#

add_executable(OneDigitalChannel
  OneDigitalChannel/one_digital_channel.cs
  )
SampleBuildSettings(OneDigitalChannel "discret")

add_executable(MultiBoardDigitalChannels
  MultiBoardDigitalChannels/multi_board_digital_channels.cs
  )
SampleBuildSettings(MultiBoardDigitalChannels "discret")

