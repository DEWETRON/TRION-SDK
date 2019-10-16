#
# Project DEWETRON TRION SDK - analog examples
#

add_executable(TN_LowLatency
    TN_LowLatency/tn_low_latency.cpp
  )
TRIONET_SampleBuildSettings(TN_LowLatency "analog")
