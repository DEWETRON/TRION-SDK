#
# CMakeLists.txt for uni_base
#

set(LIBNAME uni_base)

include_directories(
  SYSTEM
  ${Boost_INCLUDE_DIRS}
)

include_directories(
  inc
  src
)

set(UNI_HEADER_FILES
  inc/uni_assert.h
  inc/uni_defines.h
  inc/uni_stdio.h
  inc/uni_string_buffer.h 
)
source_group("Public Header Files" FILES ${UNI_HEADER_FILES})


set(UNI_SOURCE_FILES
  src/uni_assert.cpp
)

source_group("Source Files" FILES ${UNI_SOURCE_FILES})

add_library(${LIBNAME}
  ${UNI_HEADER_FILES}
  ${UNI_SOURCE_FILES}
)


target_include_directories(${LIBNAME}
  PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/inc
)

#
# add this to Visual Studio group uni
set_target_properties(${LIBNAME} PROPERTIES FOLDER "lib/uni_base")
