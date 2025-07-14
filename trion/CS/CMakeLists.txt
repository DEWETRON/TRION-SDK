#
# Project DEWETRON TRION SDK C#
#
cmake_minimum_required(VERSION 3.8)

#
# studio project name
project(TRION_SDK_CS LANGUAGES CSharp)

#
# common settings
get_filename_component(TRION_SDK_ROOT ../.. ABSOLUTE)

#
# Enable target folders in IDEs
set_property(GLOBAL
  PROPERTY USE_FOLDERS ON)

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



if (EXISTS "${TRION_SDK_ROOT}/trion_api/CS")
  if (NOT TARGET trion_api_csharp)
    add_subdirectory("${TRION_SDK_ROOT}/trion_api/CS" trion_api)
  endif()
else()
  message(status "${TRION_SDK_ROOT}/trion_api/CS  not found")
endif()



include_directories(
  ${TRION_SDK_INCLUDE_DIR}
  ${TRIONET_SDK_INCLUDE_DIR}
  /TRION_SDK_DIR        # fake path used in sdk releases
  lib/TRION_SDK_Util
  lib
)


macro(SampleBuildSettings SAMPLE FOLDER)
  set_target_properties(${SAMPLE} PROPERTIES FOLDER ${FOLDER})
  target_link_libraries(${SAMPLE}
    trion_api_csharp
    )
  if (TARGET dwpxi_api)
    add_dependencies(${SAMPLE} dwpxi_api)
  endif()
  set_property(TARGET ${SAMPLE} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.8")
endmacro()

macro(TRIONET_SampleBuildSettings SAMPLE)
  set_target_properties(${SAMPLE} PROPERTIES FOLDER "Examples TRION")
  target_link_libraries(${SAMPLE}
    trion_api_csharp
    )
  if (TARGET dwpxi_api)
    add_dependencies(${SAMPLE} dwpxi_api)
  endif()
  set_property(TARGET ${SAMPLE}
    APPEND
    PROPERTY COMPILE_DEFINITIONS
    USE_TRIONET_API
  )
  set_property(TARGET ${SAMPLE} PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.8")
endmacro()

add_subdirectory(analog)