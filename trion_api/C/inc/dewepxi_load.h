/*
 * Copyright (c) 2018 DEWETRON
 * License: MIT
 * 
 * Header file for automatic function loading
 */

#ifndef __DEWE_PXI_LOAD_H__
#define __DEWE_PXI_LOAD_H__


//
// Define BUILD_X64 for 64bit builds
// Define BUILD_X86 for 32bit builds (default)
//
#ifndef BUILD_X64
  #ifndef BUILD_X86
    // if nothing is defined default to 32bit
    #define BUILD_X86
  #endif
#endif

#ifdef BUILD_X64
  // 64bit API
  #ifdef UNDER_RTSS
    static const char DEWE_TRION_DLL_NAME[]   = "dwpxi_api_x64.rtdll";
  #elif defined WIN32
    static const char DEWE_TRION_DLL_NAME[]   = "dwpxi_api_x64.dll";
  #elif defined(__APPLE__)
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api_x64.dylib";
  #elif defined(UNIX)
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api_x64.so";
  #endif //UNIX
#endif

#ifdef BUILD_X86
  // 32bit API
  #ifdef UNDER_RTSS
    static const char DEWE_TRION_DLL_NAME[]   = "dwpxi_api.rtdll";
  #elif defined WIN32
    static const char DEWE_TRION_DLL_NAME[]   = "dwpxi_api.dll";
  #elif defined(__APPLE__)
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api.dylib";
  #elif defined(UNIX)
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api.so";
  #endif //UNIX
#endif

// Tell dewepxi_load_core.h that its included by the public load.h header
// Declare or define API symbols in dewepxi_apicore.h
#define __DEWE_PXI_LOAD

// common symbol load code
#include "dewepxi_loadcore.h"


#endif //__DEWE_PXI_LOAD_H__
