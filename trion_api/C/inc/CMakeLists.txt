#
# TRION API import project
#
cmake_minimum_required(VERSION 2.8.12)

set(LIBNAME trion_api_inc)
if (COMMAND SetProjectGuid)
  SetProjectGuid(${LIBNAME} "AC8A68FE-128A-4330-89DA-33DFF6D704FF")
endif()


set(API_HEADER_FILES
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_apicore.h
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_apiutil.h
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_commands.inc
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_const.h
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_err.h
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_err.inc
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_load.h
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxi_types.h
  ${CMAKE_CURRENT_SOURCE_DIR}/dewepxinet_load.h
)

source_group("Trion Header Files" FILES ${API_HEADER_FILES})

if (${CMAKE_VERSION} VERSION_LESS "3.1")
  add_library(${LIBNAME}
    ${API_HEADER_FILES}
    dewepxi_api.cpp
  )
else()
  add_library(${LIBNAME} INTERFACE)

#  adding source files to the import library adds those file to every dependent (direct or derived) project
#  => we do not want that
#  target_sources(${LIBNAME} INTERFACE
#    ${API_HEADER_FILES}
#  )

endif()


target_include_directories(${LIBNAME}
  INTERFACE ${CMAKE_CURRENT_SOURCE_DIR}
)

