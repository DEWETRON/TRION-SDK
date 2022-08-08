#
# Project DEWETRON TRION SDK
#
cmake_minimum_required(VERSION 2.8)

#
# studio project name
project(TRION_SDK_CXX)

#
# common settings
get_filename_component(TRION_SDK_ROOT ../.. ABSOLUTE)

#
# Enable target folders in IDEs
set_property(GLOBAL
  PROPERTY USE_FOLDERS ON)

#
# Speed up compile time
set(MSVC_BUILD_USING_MP TRUE)

#
# Configure part start

#
# Check for 64 bit build
if(CMAKE_SIZEOF_VOID_P EQUAL 8)
  set(BUILD_X64 TRUE)
  set(BUILD_X86 FALSE)
else()
  set(BUILD_X64 FALSE)
  set(BUILD_X86 TRUE)
endif()


# Settings for GCC (UNIX)
if(UNIX)

  # set UNIX flag
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DUNIX")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -DUNIX")

  if ("${CMAKE_CXX_COMPILER_ID}" STREQUAL "Clang")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wno-multichar -std=c++11 -Wno-unused-variable")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wmultichar -Wno-unused-variable")
  else()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wno-multichar -std=c++0x")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -Wmultichar")
  endif()


  if(BUILD_X64)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DBUILD_X64")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -DBUILD_X64")
  elseif (BUILD_X86)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -DBUILD_X86")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -DBUILD_X86")
  endif()

  #
  # Allow function pointers to void* assignments
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fpermissive")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS}")

  #
  # Position Independent Code
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fPIC")

endif()




if (EXISTS ${TRION_SDK_ROOT}/trion_api/C/inc)
  if (NOT TARGET trion_api_inc)
    add_subdirectory(${TRION_SDK_ROOT}/trion_api/C/inc trion_api)
  endif()
else()
  message(status "${TRION_SDK_ROOT}/trion_api/C/inc not found")
endif()


include_directories(
  ${TRION_SDK_INCLUDE_DIR}
  ${TRIONET_SDK_INCLUDE_DIR}
  ${TRION_SDK_ROOT}/trion_sdk_util/C/
)

if(MSVC)
  add_definitions(-D_CRT_SECURE_NO_WARNINGS)
  
  if(BUILD_X64)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /DBUILD_X64")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /DBUILD_X64")
  elseif(BUILD_X86)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /DBUILD_X86")
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /DBUILD_X86")
  endif()
  
endif()

# Add CXX language bindings and utility libraries
if (EXISTS ${TRION_SDK_ROOT}/trion_api/CXX/trion_api_cxx/CMakeLists.txt)
  if (NOT TARGET dw_modules_api_cxx)
    add_subdirectory(${TRION_SDK_ROOT}/trion_api/CXX/trion_api_cxx trion_api_cxx)
  endif()
else()
  message(status "${TRION_SDK_ROOT}/trion_api/CXX/trion_api_cxx not found")
endif()

# Add XML processing library
if (NOT TARGET pugixml)
  add_subdirectory(${TRION_SDK_ROOT}/3rdparty/pugixml-1.9 pugixml)
endif()

# Add XML processing utility library
if (NOT TARGET xpugixml)
  add_subdirectory(${TRION_SDK_ROOT}/trion_api/CXX/lib/xpugixml xpugixml)
endif()


macro(SampleBuildSettings SAMPLE)
  target_link_libraries(${SAMPLE}
    trion_api_inc
    trion_api_cxx
    trion_sdk_util_C
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
endmacro()

macro(SampleBuildSettingsFolder SAMPLE FOLDER)
  set_target_properties(${SAMPLE} PROPERTIES FOLDER ${FOLDER})
  target_link_libraries(${SAMPLE}
    trion_api_inc
    trion_api_cxx
    trion_sdk_util_C
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
endmacro()

macro(TRIONET_SampleBuildSettings SAMPLE)
  set_target_properties(${SAMPLE} PROPERTIES FOLDER "Examples TRIONET")
  target_link_libraries(${SAMPLE}
    trion_api_inc
    trion_api_cxx
    trion_sdk_util_C
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
    APPEND
    PROPERTY COMPILE_DEFINITIONS
    USE_TRIONET_API
  )
endmacro()

if (NOT TARGET trion_sdk_util_C)
  add_subdirectory(${TRION_SDK_ROOT}/trion_sdk_util/C trion_sdk_util)
endif()


add_subdirectory(quickstart)
add_subdirectory(synchronization)

