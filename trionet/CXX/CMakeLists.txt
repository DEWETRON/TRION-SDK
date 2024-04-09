#
# Project DEWETRON TRION SDK C++
#
cmake_minimum_required(VERSION 3.8)

#
# studio project name
project(TRIONET_SDK_CXX)

#
# common settings
get_filename_component(TRION_SDK_ROOT ../.. ABSOLUTE)

# C++ for trionet_sdk_util
set(USECXX TRUE)

#
# Select used libraries: one of following
if (NOT DEFINED USE_BOOST)
  set(USE_BOOST FALSE)
  set(USE_CXX17 TRUE)
endif()

if (USE_CXX17)
  #
  # Force C++17
  # needed for filesystem, shared_mutex
  set(CMAKE_CXX_STANDARD 17)
endif()

#
# Configure part start

# 
include(${TRION_SDK_ROOT}/cmake/CMakeSettings.cmake)


# Common build settings
SetCommonOutputDirectory()


if (EXISTS ${TRION_SDK_ROOT}/trion_api/C/inc)
  if (NOT TARGET trion_api_interface)
    add_subdirectory(${TRION_SDK_ROOT}/trion_api/C/inc trion_api)
  endif()
else()
  message(status "${TRION_SDK_ROOT}/trion_api/C/inc not found")
endif()

# Add CXX language bindings and utility libraries
if (EXISTS ${TRION_SDK_ROOT}/trion_api/CXX/trion_api_cxx/CMakeLists.txt)
  if (NOT TARGET dw_modules_api_cxx)
    add_subdirectory(${TRION_SDK_ROOT}/trion_api/CXX/trion_api_cxx trion_api_cxx)
  endif()
else()
  message(status "${TRION_SDK_ROOT}/trion_api/CXX/trion_api_cxx not found")
endif()

if (NOT TARGET trionet_sdk_util)
  add_subdirectory(${TRION_SDK_ROOT}/trion_sdk_util/C trion_sdk_util)
endif()

# Add XML processing library
if (NOT TARGET pugixml)
  add_subdirectory(${TRION_SDK_ROOT}/3rdparty/pugixml-1.9 pugixml)
endif()

# Add XML processing utility library
if (NOT TARGET xpugixml)
  add_subdirectory(${TRION_SDK_ROOT}/trion_api/CXX/lib/xpugixml xpugixml)
endif()

# macro for standars sample project definition
macro(TRIONET_SampleBuildSettings SAMPLE FOLDER)
  set_target_properties(${SAMPLE} PROPERTIES FOLDER ${FOLDER})
  target_link_libraries(${SAMPLE}
    trion_api_interface
    trion_api_cxx
    trionet_sdk_util
    xpugixml
    pugixml
    )
  if(UNIX)
    target_link_libraries(${SAMPLE}
      dl
      pthread
      )
  endif()
  if (TARGET dwpxi_api)
    add_dependencies(${SAMPLE} dwpxi_api)
  endif()
  if (TRION_STATIC_LIB)
    set_property(TARGET ${SAMPLE}
      APPEND
      PROPERTY COMPILE_DEFINITIONS
      STATIC_DLL
      )
    target_link_libraries(
      ${SAMPLE}
      dwpxi_api
      )
  endif()
  set_property(TARGET ${SAMPLE}
    APPEND PROPERTY COMPILE_DEFINITIONS
    USE_TRIONET_API
  )
  if (USE_CXX17)
    set_property(TARGET ${SAMPLE}
      APPEND PROPERTY COMPILE_DEFINITIONS
      USE_CXX17
    )
  endif()
  if (USE_BOOST)
    set_property(TARGET ${SAMPLE}
      APPEND PROPERTY COMPILE_DEFINITIONS
      USE_BOOST
    )
  endif()
endmacro()




# examples
add_subdirectory(analog)
