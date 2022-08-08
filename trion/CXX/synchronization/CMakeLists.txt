#
# Project DEWETRON TRION SDK - synchronization examples
#

add_executable(SynchronizationPTP
  synchronization_ptp.cpp
  )
SampleBuildSettings(SynchronizationPTP)

add_executable(SynchronizationReadTime
  synchronization_read_time.cpp
  )
SampleBuildSettings(SynchronizationReadTime)