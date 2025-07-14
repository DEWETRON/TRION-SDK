#
# Project DEWETRON TRION SDK C# Wrapper
#
cmake_minimum_required(VERSION 3.8)

#
# studio project name
project(TRIONET_SDK_CSHARP CSharp C)

#
# common settings
get_filename_component(TRION_SDK_ROOT ../.. ABSOLUTE)

#
# Enable target folders in IDEs
set_property(GLOBAL
  PROPERTY USE_FOLDERS ON)


#
# Wrapper sources
set(TRION_API_SRC
    trion_net_api.cs
    ${CMAKE_CURRENT_LIST_DIR}/trion_errors.cs
    ${CMAKE_CURRENT_LIST_DIR}/trion_commands.cs
    ${CMAKE_CURRENT_LIST_DIR}/templates/trion_errors.cs.template
    ${CMAKE_CURRENT_LIST_DIR}/templates/trion_commands.cs.template
)

#
# Auto generate trion_errors.cs
add_custom_command(
    OUTPUT ${CMAKE_CURRENT_LIST_DIR}/trion_errors.cs
    COMMAND "${CMAKE_C_COMPILER}" -C -EP ${CMAKE_CURRENT_LIST_DIR}/templates/trion_errors.cs.template -I ${TRION_SDK_ROOT}/trion_api/C/inc > ${CMAKE_CURRENT_LIST_DIR}/trion_errors.cs
    MAIN_DEPENDENCY ${TRION_SDK_ROOT}/trion_api/C/inc/dewepxi_err.inc
    DEPENDS ${CMAKE_CURRENT_LIST_DIR}/templates/trion_errors.cs.template
    COMMENT "Creating trion_errors.cs"
    )

#
# Auto generate trion_commands.cs
add_custom_command(
    OUTPUT ${CMAKE_CURRENT_LIST_DIR}/trion_commands.cs
    COMMAND "${CMAKE_C_COMPILER}" -C -EP ${CMAKE_CURRENT_LIST_DIR}/templates/trion_commands.cs.template -I ${TRION_SDK_ROOT}/trion_api/C/inc > ${CMAKE_CURRENT_LIST_DIR}/trion_commands.cs
    MAIN_DEPENDENCY ${TRION_SDK_ROOT}/trion_api/C/inc/dewepxi_commands.inc
    DEPENDS ${CMAKE_CURRENT_LIST_DIR}/templates/trion_commands.cs.template
    COMMENT "Creating trion_commands.cs"
    )

#
# Wrapper library
add_library(trion_api_csharp SHARED
    ${TRION_API_SRC}
)

#
# Unsafe needed
target_compile_options(trion_api_csharp
    PUBLIC "/unsafe"
)

#
# .Net Framwork wanted
set_property(TARGET trion_api_csharp PROPERTY VS_DOTNET_TARGET_FRAMEWORK_VERSION "v4.8")