/*
 * Copyright (c) 2020 DEWETRON
 * License: MIT
 *
 * Header file for automatic function loading
 * Private header: Do not include directly!
 */

#ifndef __DEWE_PXI_LOAD_CORE_H__
#define __DEWE_PXI_LOAD_CORE_H__


#ifndef DEWEPXI_NO_API_INJECTION


#include "dewepxi_apicore.h"
#include "dewepxi_types.h"
#ifdef WIN32
#include "dewepxi_apiutil.h"
#endif
#include <stdio.h>
#if !defined(WIN32) && !defined(__APPLE__) && defined(__linux__)
#include <linux/limits.h>
#include <libgen.h>
#include <unistd.h>
#endif



#ifdef __cplusplus
#  ifdef DEWE_PXI_NS
#    ifdef DEWE_PXI_NS_NAME
using namespace DEWE_PXI_NS_NAME;
#    else
using namespace trion_c_api;
#    endif
#  endif
#endif


//*************************************************************************************
// Main Load / Unload Interface
//*************************************************************************************

#ifdef __cplusplus
#  ifdef DEWE_PXI_NS
#    ifdef DEWE_PXI_NS_NAME
namespace DEWE_PXI_NS_NAME {
#    else
namespace trion_c_api {
#    endif
#  endif
#endif

// Load DLL
int DeWePxiLoad(void);

// Unload DLL
void DeWePxiUnload(void);

// Load DLL by name
int DeWePxiLoadByName(const char* name);


#ifdef __cplusplus
#  ifdef DEWE_PXI_NS
}
#  endif
#endif


#ifndef DEWE_PXI_NS_EXTRA_SOURCE
//

#ifdef WIN32
#  include <windows.h>
#elif defined(__APPLE__)
#  include <dlfcn.h>
#elif defined(UNIX)
#  include <dlfcn.h>
#endif //UNIX

static int     LoadedRevision = 0;

#ifdef WIN32
static HINSTANCE   hLib = 0;
#endif //WIN32

#ifdef UNIX
static void*       hLib = 0;
#endif //UNIX


//*************************************************************************************
// TEST interface (deprecated)
//*************************************************************************************
#ifdef DEWEPXITESTINTERFACE
#include "DeWePxi_load_test.h"
#endif

#ifndef DEWEPXITESTINTERFACE

#ifdef __cplusplus
#  ifdef DEWE_PXI_NS
#    ifdef DEWE_PXI_NS_NAME
namespace DEWE_PXI_NS_NAME {
#    else
namespace trion_c_api {
#    endif
#  endif
#endif

BOOLEAN DewePxiLoadTestInterface( BOOLEAN*  bTotalOK )  { return (bTotalOK != 0) ? *bTotalOK : FALSE; }
void    DewePxiUnloadTestInterface() {}

#ifdef __cplusplus
#  ifdef DEWE_PXI_NS
}
#  endif
#endif

#endif





//*************************************************************************************
// Main Load / Unload Interface
//*************************************************************************************
#ifdef WIN32
static void* loadFunction(
             const char*        pcName,
             BOOLEAN*           bTotalOK
             )
{
    void*       pf = NULL;
    BOOLEAN     bDummy = FALSE;

    if ( NULL == bTotalOK ){
        bTotalOK = &bDummy;
    }
    if ( !hLib ) {
        *bTotalOK = FALSE;
        return pf;
    }
    if ( NULL == pcName ){
        *bTotalOK = FALSE;
        return pf;
    }

    pf = GetProcAddress(hLib, pcName);

    if ( NULL == pf ){
        *bTotalOK = FALSE;
#ifndef NDEBUG
        {
            char szMsg[255] = {0};
            DWORD err;
            err = GetLastError();
            snprintf( szMsg, sizeof(szMsg), "%s failed to load (ERR %d).", pcName, err );
            OutputDebugStringA(szMsg);
        }
#endif

    }
    return pf;
}
#endif //WIN32


#ifdef WIN32
  #ifdef LOADDLLFUNCTION
    #error "LOADDLLFUNCTION already defined"
  #else
    #define LOADDLLFUNCTION(lib, type, name) name=(type)loadFunction(#name, &bTotResult);

    #define LOAD_LIBRARY(dll_name) LoadLibraryA(dll_name)
    #define CLOSE_LIBRARY(so_name) FreeLibrary(hLib);
  #endif //LOADDLLFUNCTION

  //local helper function
  static void* loadFunction( const char* pcName, BOOLEAN* bTotalOK );
#endif //WIN32


#ifdef UNIX
  #ifdef LOADDLLFUNCTION
    #error "LOADDLLFUNCTION already defined"
  #else
    #define LOADDLLFUNCTION(lib, type, name)  name = (type) dlsym(lib, #name); \
    if ((error = dlerror()) != NULL) {                                  \
        fprintf(stderr, "%s\n", error);                                 \
        bTotResult = FALSE;                                             \
    }

    #ifdef __APPLE__
      #define DW_DLOPEN_FLAGS RTLD_LAZY | RTLD_LOCAL
    #else
      #define DW_DLOPEN_FLAGS RTLD_LAZY | RTLD_LOCAL | RTLD_DEEPBIND
    #endif

    #define LOAD_LIBRARY(so_name) dlopen(so_name, DW_DLOPEN_FLAGS);   \
    if (!hLib) {                                                \
        error = dlerror();                                      \
        fprintf(stderr, "%s\n", error);                         \
    }


    #define CLOSE_LIBRARY(so_name) dlclose(so_name)
  #endif //LOADDLLFUNCTION
#endif //UNIX


#ifdef UNLOADDLLFUNCTION
  #error "UNLOADDLLFUNCTION already defined"
#else
  #define UNLOADDLLFUNCTION(f)       f = NULL;
#endif


//######################################################################################################################################################
// Load
//######################################################################################################################################################


#ifdef __cplusplus
#  ifdef DEWE_PXI_NS
#    ifdef DEWE_PXI_NS_NAME
namespace DEWE_PXI_NS_NAME {
#    else
namespace trion_c_api {
#    endif
#  endif
#endif

#ifdef __DEWE_PXI_LOAD
int DeWePxiLoad(void)
{
    return DeWePxiLoadByName(DEWE_TRION_DLL_NAME);
}
#endif

#ifndef STATIC_DLL
int DeWePxiLoadByName(const char* name)
{
    BOOLEAN        bTotResult = TRUE;
    char*          error = NULL;
    int revision = 0;
    //Trap multiple Loads
    if ( LoadedRevision > 0 ) {
        return LoadedRevision;
    }

    hLib = LOAD_LIBRARY(name);
#if !defined(WIN32) && !defined(__APPLE__) && defined(__linux__)
    if (!hLib)
    {
        char executable_path[PATH_MAX];
        ssize_t count = readlink("/proc/self/exe", executable_path, PATH_MAX);
        if (count != -1)
        {
            const char *search_path = dirname(executable_path);
            char real_plugin_filename[PATH_MAX];
            snprintf(real_plugin_filename, PATH_MAX, "%s/%s", search_path, name);
            hLib = LOAD_LIBRARY(real_plugin_filename);
        }
    }
#endif
    if (!hLib)
    {
        LoadedRevision = 0;
    }

    bTotResult = ((!hLib) ? FALSE : TRUE);

    if (bTotResult)
    {
        // Driver Init
        LOADDLLFUNCTION( hLib, PDEWEDRIVERINIT, DeWeDriverInit );
        LOADDLLFUNCTION( hLib, PDEWEDRIVERDEINIT, DeWeDriverDeInit );

        // _i32 functions
        LOADDLLFUNCTION( hLib, PDEWEGETPARAM_I32, DeWeGetParam_i32 );
        LOADDLLFUNCTION( hLib, PDEWESETPARAM_I32, DeWeSetParam_i32 );

        // _i64 functions
        LOADDLLFUNCTION( hLib, PDEWEGETPARAM_I64, DeWeGetParam_i64 );
        LOADDLLFUNCTION( hLib, PDEWESETPARAM_I64, DeWeSetParam_i64 );

        // string based functions
        LOADDLLFUNCTION( hLib, PDEWESETPARAMSTRUCT_STR, DeWeSetParamStruct_str );
        LOADDLLFUNCTION( hLib, PDEWEGETPARAMSTRUCT_STR, DeWeGetParamStruct_str );
        LOADDLLFUNCTION( hLib, PDEWEGETPARAMSTRUCT_STRLEN, DeWeGetParamStruct_strLEN );

        LOADDLLFUNCTION( hLib, PDEWESETPARAMXML_STR, DeWeSetParamXML_str );
        LOADDLLFUNCTION( hLib, PDEWEGETPARAMXML_STR, DeWeGetParamXML_str );

        // CAN functions
        LOADDLLFUNCTION( hLib, PDEWEOPENCAN, DeWeOpenCAN );
        LOADDLLFUNCTION( hLib, PDEWECLOSECAN, DeWeCloseCAN );
        LOADDLLFUNCTION( hLib, PDEWEGETCHANNELPROPCAN, DeWeGetChannelPropCAN );
        LOADDLLFUNCTION( hLib, PDEWESETCHANNELPROPCAN, DeWeSetChannelPropCAN );
        LOADDLLFUNCTION( hLib, PDEWESTARTCAN, DeWeStartCAN );
        LOADDLLFUNCTION( hLib, PDEWESTOPCAN, DeWeStopCAN );
        LOADDLLFUNCTION( hLib, PDEWEREADCAN, DeWeReadCAN );
        LOADDLLFUNCTION( hLib, PDEWEREADCANRAWFRAME, DeWeReadCANRawFrame );
        LOADDLLFUNCTION( hLib, PDEWEWRITECAN, DeWeWriteCAN );
        LOADDLLFUNCTION( hLib, PDEWEERRORCNTCAN, DeWeErrorCntCAN );

        // Asynchronous channel(UART) functions
        LOADDLLFUNCTION( hLib, PDEWEOPENDMAUART, DeWeOpenDmaUart );
        LOADDLLFUNCTION( hLib, PDEWECLOSEDMAUART, DeWeCloseDmaUart );
        LOADDLLFUNCTION( hLib, PDEWEGETCHANNELPROPDMAUART, DeWeGetChannelPropDmaUart );
        LOADDLLFUNCTION( hLib, PDEWESETCHANNELPROPDMAUART, DeWeSetChannelPropDmaUart );
        LOADDLLFUNCTION( hLib, PDEWESTARTDMAUART, DeWeStartDmaUart );
        LOADDLLFUNCTION( hLib, PDEWESTOPDMAUART, DeWeStopDmaUart );
        LOADDLLFUNCTION( hLib, PDEWEREADDMAUART, DeWeReadDmaUart );
        LOADDLLFUNCTION( hLib, PDEWEREADDMAUARTRAWFRAME, DeWeReadDmaUartRawFrame );
        LOADDLLFUNCTION( hLib, PDEWEWRITEDMAUART, DeWeWriteDmaUart );

        // Obtain readable ErrorMessage from ErrorCode
        LOADDLLFUNCTION( hLib, PDEWEERRORCONSTANTTOSTRING, DeWeErrorConstantToString );

        //Load the Test interface, if provided
        bTotResult = DewePxiLoadTestInterface( &bTotResult );
    }

    if (bTotResult) //all revision 1 functions loaded => check for rev2
    {
        revision = 1;
        // string based functions
        LOADDLLFUNCTION( hLib, PDEWEGETPARAMXML_STRLEN, DeWeGetParamXML_strLEN );
    }

    if (bTotResult)  //all revision 2 functions loaded => check for rev3
    {
        revision = 2;
        LOADDLLFUNCTION( hLib, PDEWEFREEFRAMESCAN, DeWeFreeFramesCAN);
    }

    if (bTotResult)  //all revision 3 functions loaded => check for rev4
    {
        revision = 3;
        LOADDLLFUNCTION(hLib, PDEWEFREEDMAUARTRAWFRAME, DeWeFreeDmaUartRawFrame);
    }

    if (bTotResult)  //all revision 4 functions loaded
    {
        revision = 4;
        LOADDLLFUNCTION(hLib, PDEWEGETPARAMSTRUCTEX_STR, DeWeGetParamStructEx_str);
    }

    if (bTotResult)  //all revision 4 functions loaded
    {
        revision = 5;
        LOADDLLFUNCTION(hLib, PDEWEREADCANEX, DeWeReadCANEx);
        LOADDLLFUNCTION(hLib, PDEWEREADCANRAWFRAMEEX, DeWeReadCANRawFrameEx);
        LOADDLLFUNCTION(hLib, PDEWEWRITECANEX, DeWeWriteCANEx );
        LOADDLLFUNCTION(hLib, PDEWEREADCANNG, DeWeReadCANNg);
    }


    if (bTotResult)  //all revision 4 functions loaded
    {
        revision = 6;
    }

    LoadedRevision = revision;

    if ( 0 == revision) //no valid dll found
    {
        DeWePxiUnload();
    }

    return revision;
}



//######################################################################################################################################################
// Unload
//######################################################################################################################################################
void DeWePxiUnload(void)
{
    if (LoadedRevision > 0) {
        if(DeWeDriverDeInit != 0) {
            DeWeDriverDeInit();
        }
        CLOSE_LIBRARY(hLib);
        LoadedRevision = 0;
        hLib    = 0;
    }

    // Driver Init
    UNLOADDLLFUNCTION(DeWeDriverInit);
    UNLOADDLLFUNCTION(DeWeDriverDeInit);

    // _i32 functions
    UNLOADDLLFUNCTION(DeWeGetParam_i32);
    UNLOADDLLFUNCTION(DeWeSetParam_i32);

    // _i64 functions
    UNLOADDLLFUNCTION(DeWeGetParam_i64);
    UNLOADDLLFUNCTION(DeWeSetParam_i64);


    // string based functions
    UNLOADDLLFUNCTION(DeWeSetParamStruct_str);
    UNLOADDLLFUNCTION(DeWeGetParamStruct_str);
    UNLOADDLLFUNCTION(DeWeGetParamStruct_strLEN);
    UNLOADDLLFUNCTION(DeWeGetParamStructEx_str);

    UNLOADDLLFUNCTION(DeWeSetParamXML_str);
    UNLOADDLLFUNCTION(DeWeGetParamXML_str);
    UNLOADDLLFUNCTION(DeWeGetParamXML_strLEN);

    // CAN functions
    UNLOADDLLFUNCTION(DeWeOpenCAN);
    UNLOADDLLFUNCTION(DeWeCloseCAN);
    UNLOADDLLFUNCTION(DeWeGetChannelPropCAN);
    UNLOADDLLFUNCTION(DeWeSetChannelPropCAN);
    UNLOADDLLFUNCTION(DeWeStartCAN);
    UNLOADDLLFUNCTION(DeWeStopCAN);
    UNLOADDLLFUNCTION(DeWeReadCAN);
    UNLOADDLLFUNCTION(DeWeFreeFramesCAN);
    UNLOADDLLFUNCTION(DeWeReadCANRawFrame);
    UNLOADDLLFUNCTION(DeWeWriteCAN);
    UNLOADDLLFUNCTION(DeWeErrorCntCAN);
    UNLOADDLLFUNCTION(DeWeReadCANEx);
    UNLOADDLLFUNCTION(DeWeReadCANRawFrameEx);
    UNLOADDLLFUNCTION(DeWeWriteCANEx);
    UNLOADDLLFUNCTION(DeWeReadCANNg);

    // Asynchronous channel(UART) functions
    UNLOADDLLFUNCTION(DeWeOpenDmaUart);
    UNLOADDLLFUNCTION(DeWeCloseDmaUart);
    UNLOADDLLFUNCTION(DeWeGetChannelPropDmaUart);
    UNLOADDLLFUNCTION(DeWeSetChannelPropDmaUart);
    UNLOADDLLFUNCTION(DeWeStartDmaUart);
    UNLOADDLLFUNCTION(DeWeStopDmaUart);
    UNLOADDLLFUNCTION(DeWeReadDmaUart);
    UNLOADDLLFUNCTION(DeWeReadDmaUartRawFrame);
    UNLOADDLLFUNCTION(DeWeFreeDmaUartRawFrame);
    UNLOADDLLFUNCTION(DeWeWriteDmaUart);

    // Obtain readable ErrorMessage from ErroCode
    UNLOADDLLFUNCTION(DeWeErrorConstantToString);

    //Unload the Test-Interface, if provided
    DewePxiUnloadTestInterface();

}

#else // STATIC_DLL

int DeWePxiLoadByName(const char* name)
{
    int revision = 6;

    LoadedRevision = revision;

    return revision;
}

void DeWePxiUnload(void)
{
    if (LoadedRevision) {
        DeWeDriverDeInit();
    }
    LoadedRevision = 0;
}

#endif // STATIC_DLL


#ifdef __cplusplus
#  ifdef DEWE_PXI_NS
}
#  endif
#endif

#endif

#endif //DEWEPXI_NO_API_INJECTION

#endif //__DEWE_PXI_LOAD_CORE_H__
