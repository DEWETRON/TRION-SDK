#
# Project DEWETRON TRION SDK - quickstart examples
#

add_executable(Quickstart
  quickstart.cpp
  )
SampleBuildSettings(Quickstart)

add_executable(QuickstartAcq
  quickstart_acq.cpp
  )
SampleBuildSettings(QuickstartAcq)

add_executable(QuickstartAcqScanDesc
  quickstart_acq_scan_desc.cpp
  )
SampleBuildSettings(QuickstartAcqScanDesc)
