#
# TRION SDK Util lib

#
# TRION API Util
#
set(LIBNAME trion_sdk_util_C)

add_library(${LIBNAME} STATIC
  trion_sdk_util.h
  trion_sdk_util.c
  )

if (USECXX)
  set_source_files_properties(trion_sdk_util.c
    PROPERTIES LANGUAGE CXX)
endif()

target_link_libraries(${LIBNAME}
  trion_api_inc
  )

if (TRION_STATIC_LIB)
  set_property(TARGET ${LIBNAME}
    APPEND
    PROPERTY COMPILE_DEFINITIONS
    STATIC_DLL
    )
endif()

target_include_directories(${LIBNAME}
  PUBLIC ${CMAKE_CURRENT_LIST_DIR}
)

set_target_properties(${LIBNAME} PROPERTIES FOLDER "lib")


#
# TRIONET API Util - only difference is to load and use dwpxi_netapi_x64.dll
# allowing to access TRIONET devices.
set(LIBNAME trionet_sdk_util)

add_library(${LIBNAME} STATIC
  trion_sdk_util.h
  trion_sdk_util.c
  )

if (USE_CXX)
  set_source_files_properties(trion_sdk_util.c
    PROPERTIES LANGUAGE CXX)
endif()


target_link_libraries(${LIBNAME}
  trion_api_inc
  )

if (TRION_STATIC_LIB)
  set_property(TARGET ${LIBNAME}
    APPEND
    PROPERTY COMPILE_DEFINITIONS
    STATIC_DLL
    )
endif()

set_property(TARGET ${LIBNAME}
  APPEND
  PROPERTY COMPILE_DEFINITIONS
  USE_TRIONET_API
)

target_include_directories(${LIBNAME}
  PUBLIC ${CMAKE_CURRENT_LIST_DIR}
)

set_target_properties(${LIBNAME} PROPERTIES FOLDER "lib")
