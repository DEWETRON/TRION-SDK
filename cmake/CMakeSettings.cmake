if(my_module_CmakeSettings_included)
  return()
endif(my_module_CmakeSettings_included)
set(my_module_CmakeSettings_included true)

#
# Enable target folders in IDEs
set_property(GLOBAL
  PROPERTY USE_FOLDERS ON)

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



#
# Use this macro to set a common output directory for all artifacts
# of a build.
#
macro(SetCommonOutputDirectory)
  if(MSVC)
    if (NOT CMAKE_LIBRARY_OUTPUT_DIRECTORY)
      set(CMAKE_LIBRARY_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR})
    endif()
    if (NOT CMAKE_ARCHIVE_OUTPUT_DIRECTORY)
      set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR})
    endif()
    if (NOT CMAKE_RUNTIME_OUTPUT_DIRECTORY)
      set(CMAKE_RUNTIME_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR})
    endif()
  elseif(APPLE)
    if (XCODE_VERSION)
      if (NOT CMAKE_LIBRARY_OUTPUT_DIRECTORY)
        set(CMAKE_LIBRARY_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR})
      endif()
      if (NOT CMAKE_ARCHIVE_OUTPUT_DIRECTORY)
        set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR})
      endif()
      if (NOT CMAKE_RUNTIME_OUTPUT_DIRECTORY)
        set(CMAKE_RUNTIME_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR})
      endif()
    else()
      if (NOT CMAKE_LIBRARY_OUTPUT_DIRECTORY)
        set(CMAKE_LIBRARY_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
      endif()
      if (NOT CMAKE_ARCHIVE_OUTPUT_DIRECTORY)
        set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
      endif()
      if (NOT CMAKE_RUNTIME_OUTPUT_DIRECTORY)
        set(CMAKE_RUNTIME_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
      endif()
    endif()
  else()
    if (NOT CMAKE_LIBRARY_OUTPUT_DIRECTORY)
      set(CMAKE_LIBRARY_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
    endif()
    if (NOT CMAKE_ARCHIVE_OUTPUT_DIRECTORY)
      set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
    endif()
    if (NOT CMAKE_RUNTIME_OUTPUT_DIRECTORY)
      set(CMAKE_RUNTIME_OUTPUT_DIRECTORY    ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE})
    endif()
  endif()

endmacro()
