#
# dw_modules_api
# Userland CXX
#

set(LIBNAME_CXX trion_api_cxx)

include_directories(
  inc
  src
)

#
# C++ interface
set(TRION_CXX_API_HEADER_FILES
    inc/dewepxi_apicxx.h
)

set(TRION_CXX_API_SOURCE_FILES
    src/dewepxi_apicxx.cpp
)

add_library(${LIBNAME_CXX}
  ${TRION_CXX_API_HEADER_FILES}
  ${TRION_CXX_API_SOURCE_FILES}
)

target_link_libraries(${LIBNAME_CXX}
    trion_api_interface
)

target_include_directories(${LIBNAME_CXX}
  PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/inc
)



#
# add utility libraries
if (NOT TARGET uni_base)
  add_subdirectory(../lib/uni_base uni_base)
endif()
