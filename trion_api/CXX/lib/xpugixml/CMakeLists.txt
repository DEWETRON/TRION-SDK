#
# CMakeLists.txt for xpugixml
#

set(LIBNAME xpugixml)

#
# define REPO_ROOT
get_filename_component(REPO_ROOT ../../../.. ABSOLUTE)

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



if (NOT TARGET pugixml)
  add_subdirectory(${REPO_ROOT}/3rdparty/pugixml-1.9/scripts 3rdparty/pugixml-1.9)
endif()

include_directories(
  SYSTEM
  ${Boost_INCLUDE_DIRS}
)

include_directories(
  inc
  src
  ../uni/inc
)


set(XML_PUBLIC_HEADER_FILES
  inc/xpugixml.h
  inc/xpugixml_fwd.h
)

set(XML_PUBLIC_SOURCE_FILES
  src/xpugixml.cpp
)

source_group("uni\\XML" FILES ${XML_PUBLIC_HEADER_FILES})

add_library(${LIBNAME} STATIC
  ${XML_PUBLIC_HEADER_FILES}
  ${XML_PUBLIC_SOURCE_FILES}
)

if (USE_CXX17)
  set_property(TARGET ${LIBNAME}
    APPEND PROPERTY COMPILE_DEFINITIONS
    USE_CXX17
  )
endif()
if (USE_BOOST)
  set_property(TARGET ${LIBNAME}
    APPEND PROPERTY COMPILE_DEFINITIONS
    USE_BOOST
  )
endif()

target_link_libraries(${LIBNAME}
  uni_base
)

target_include_directories(${LIBNAME} SYSTEM
  PUBLIC ${REPO_ROOT}/3rdparty/pugixml-1.9/src
  )

target_include_directories(${LIBNAME}
  PUBLIC ${CMAKE_CURRENT_SOURCE_DIR}/inc
)

set_target_properties(${LIBNAME} PROPERTIES LINKER_LANGUAGE CXX)


#
# add this to Visual Studio group lib
set_target_properties(${LIBNAME} PROPERTIES FOLDER "lib/xpugixml")
