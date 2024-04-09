/*
Header file for automatic function loading
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
  #ifdef WIN32
    #include <windows.h>
      static const char DEWE_TRION_DLL_NAME[] = "dwpxi_api_x64.dll";
  #elif defined(__APPLE__)
    #include <dlfcn.h>
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api_x64.dylib";
  #elif defined(UNIX)
    #include <dlfcn.h>
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api_x64.so";
  #endif //UNIX
#endif

#ifdef BUILD_X86
  // 32bit API
  #ifdef WIN32
    #include <windows.h>
      static const char DEWE_TRION_DLL_NAME[] = "dwpxi_api.dll";
  #elif defined(__APPLE__)
    #include <dlfcn.h>
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api.dylib";
  #elif defined(UNIX)
    #include <dlfcn.h>
    static const char DEWE_TRION_DLL_NAME[]   = "libdwpxi_api.so";
  #endif //UNIX
#endif

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

static int     LoadedRevision = 0;



#ifdef WIN32
static HINSTANCE   hLib = 0;
#endif //WIN32

#ifdef UNIX
static void*       hLib = 0;
#endif //UNIX


#ifndef STATIC_DLL

//*************************************************************************************
// Main Load / Unload Interface
//*************************************************************************************

// Load DLL
int DeWePxiLoad(void);

// Unload DLL
void DeWePxiUnload(void);

//*************************************************************************************
// Main Load / Unload Interface
//*************************************************************************************

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




#endif  //STATIC_DLL

#ifdef DEWEPXITESTINTERFACE
#include "DeWePxi_load_test.h"
#endif

#ifndef STATIC_DLL

#ifndef DEWEPXITESTINTERFACE
BOOLEAN DewePxiLoadTestInterface( BOOLEAN*  bTotalOK )  { return (bTotalOK != 0) ? *bTotalOK : FALSE; }
void    DewePxiUnloadTestInterface() {}
#endif

#ifdef WIN32
static
void*
loadFunction(
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

//######################################################################################################################################################
// Load
//######################################################################################################################################################
int DeWePxiLoad(void)
{
    BOOLEAN        bTotResult = TRUE;
    char*          error = NULL;
    int revision = 0;
    //Trap multiple Loads
    if ( LoadedRevision > 0 ) {
        return LoadedRevision;
    }

    hLib = LOAD_LIBRARY(DEWE_TRION_DLL_NAME);
#if !defined(WIN32) && !defined(__APPLE__) && defined(__linux__)
    if (!hLib)
    {
        char executable_path[PATH_MAX];
        ssize_t count = readlink("/proc/self/exe", executable_path, PATH_MAX);
        if (count != -1)
        {
            const char *search_path = dirname(executable_path);
            char real_plugin_filename[PATH_MAX];
            snprintf(real_plugin_filename, PATH_MAX, "%s/%s", search_path, DEWE_TRION_DLL_NAME);
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

        // Obtain readable ErrorMessage from ErroCode
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

/**
 * DeWePxiLoad
 * empty implementation for static linking
 */
int DeWePxiLoad(void)
{
    int revision = 5;

    LoadedRevision = revision;

    return revision;
}

/**
 * DeWePxiUnload
 * empty implementation for static linking
 */
void DeWePxiUnload(void)
{
    if (LoadedRevision) {
        DeWeDriverDeInit();
    }
    LoadedRevision = 0;
}

#endif

#ifndef STATIC_DLL

#undef LOADDLLFUNCTION
#undef UNLOADDLLFUNCTION

#endif //STATIC_DLL

#endif //DEWEPXI_NO_API_INJECTION

#endif //__DEWE_PXI_LOAD_H__
