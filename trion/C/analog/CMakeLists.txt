#
# Project DEWETRON TRION SDK - analog examples
#

add_executable(MinimalExample
  MinimalExample/minimal_example.c
  )
SampleBuildSettings(MinimalExample "minimal")

add_executable(OneAnalogChannel
  OneAnalogChannel/one_analog_channel.c
  )
SampleBuildSettings(OneAnalogChannel "analog")

add_executable(ADCDelay
  ADCDelay/adc_delay.c
  )
SampleBuildSettings(ADCDelay "analog")

add_executable(AIGainSelfJustage
  AIGainSelfJustage/ai_gain_selfjustage.c
  )
SampleBuildSettings(AIGainSelfJustage "analog")

add_executable(AmplifierBalanceAutonomous
  AmplifierBalanceAutonomous/amp_balance_autonomous.c
  )
SampleBuildSettings(AmplifierBalanceAutonomous "analog")

add_executable(AmplifierBalanceDuringMeasurement
  AmplifierBalanceDuringMeasurement/amp_balance_measurement.c
  )
SampleBuildSettings(AmplifierBalanceDuringMeasurement "analog")

add_executable(AnalogBridgeScaling
  AnalogBridgeScaling/analog_bridge_scaling.c
  )
SampleBuildSettings(AnalogBridgeScaling "analog")

add_executable(AnalogScaling
  AnalogScaling/analog_scaling.c
  )
SampleBuildSettings(AnalogScaling "analog")

add_executable(ChangeInputOffsetDuringMeas
  ChangeInputOffsetDuringMeas/change_inputoffset_during_meas.c
  )
SampleBuildSettings(ChangeInputOffsetDuringMeas "analog")

add_executable(CurrentMeas
  CurrentMeas/currentmeas.c
  )
SampleBuildSettings(CurrentMeas "analog")

add_executable(dSTGBackgroundModeCheck
  dSTGBackgroundModeCheck/dstg_background_modecheck.c
  )
SampleBuildSettings(dSTGBackgroundModeCheck "analog")

add_executable(dSTGBridge
  dSTGBridge/dstg_bridge.c
  )
SampleBuildSettings(dSTGBridge "analog")

add_executable(dSTGModeCheck
  dSTGModeCheck/dstg_modecheck.c
  )
SampleBuildSettings(dSTGModeCheck "analog")

add_executable(EnhancedAnalogMeasurement
  EnhancedAnalogMeasurement/enhanced_analog_measurement.c
  )
SampleBuildSettings(EnhancedAnalogMeasurement "analog")

add_executable(ExcCurMonitor
  ExcCurMonitor/exccurmonitor.c
  )
SampleBuildSettings(ExcCurMonitor "analog")

add_executable(ExcVoltMonitor
  ExcVoltMonitor/excvoltmonitor.c
  )
SampleBuildSettings(ExcVoltMonitor "analog")

add_executable(InterfacingTEDS
  InterfacingTEDS/interfacingTEDS.c
  )
SampleBuildSettings(InterfacingTEDS "analog/teds")

add_executable(InterfacingTEDSCal
  InterfacingTEDSCal/interfacingTEDSCal.c
  )
SampleBuildSettings(InterfacingTEDSCal "analog/teds")

add_executable(InterfacingTEDSEx
  InterfacingTEDSEx/interfacingTEDSEx.c
  )
SampleBuildSettings(InterfacingTEDSEx "analog/teds")

add_executable(InternalVoltageReference
  InternalVoltageReference/internal_voltage_reference.c
  )
SampleBuildSettings(InternalVoltageReference "analog")

add_executable(InternalVoltageReferenceEx
  InternalVoltageReferenceEx/internal_voltage_reference_ex.c
  )
SampleBuildSettings(InternalVoltageReferenceEx "analog")

add_executable(MSIMode
  MSIMode/msi_mode.c
  )
SampleBuildSettings(MSIMode "analog")

add_executable(MultiBoardAnalogChannels
  MultiBoardAnalogChannels/multi_board_analog_channels.c
  )
SampleBuildSettings(MultiBoardAnalogChannels "analog")

add_executable(PotiMode
  PotiMode/potimode.c
  )
SampleBuildSettings(PotiMode "analog")

add_executable(ResistanceMode
  ResistanceMode/resistance_mode.c
  )
SampleBuildSettings(ResistanceMode "analog")

add_executable(ScanDescriptor
  ScanDescriptor/scan_descriptor.c
  )
SampleBuildSettings(ScanDescriptor "analog")

add_executable(SensorBalanceAutonomous
  SensorBalanceAutonomous/sensor_balance_autonomous.c
  )
SampleBuildSettings(SensorBalanceAutonomous "analog")

add_executable(SensorBalanceDuringMeasGroupCommand
  SensorBalanceDuringMeasGroupCommand/sensor_balance_measurement_gc.c
  )
SampleBuildSettings(SensorBalanceDuringMeasGroupCommand "analog")

add_executable(SensorBalanceDuringMeasurement
  SensorBalanceDuringMeasurement/sensor_balance_measurement.c
  )
SampleBuildSettings(SensorBalanceDuringMeasurement "analog")

add_executable(TemperatureMode
  TemperatureMode/temperature_mode.c
  )
SampleBuildSettings(TemperatureMode "analog")

add_executable(ThreeWireOffset
  ThreeWireOffset/threewireoffset.c
  )
SampleBuildSettings(ThreeWireOffset "analog")
