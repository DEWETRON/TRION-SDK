//DewePXI_apiCore.h
//API core Interface
//

#ifndef DewePXI_apiCore_h__
#define DewePXI_apiCore_h__

#include <stdlib.h>

//Include rest of API
#include "dewepxi_const.h"
#include "dewepxi_err.h"
#include "dewepxi_types.h"

#ifdef _WIN32
# include <windows.h>
# ifndef RT_IMPORT
#  define RT_IMPORT WINAPI //__declspec(dllimport)
# endif
#else
# ifndef RT_IMPORT
#  define RT_IMPORT
# endif
#endif

//for C++ - compliance.
#ifndef __DEWE_PXI_LOAD
    // declare symbols
    #undef RTLOAD
	#define RTLOAD	extern
#else
    // define symbols
    #undef RTLOAD
	#define RTLOAD
#endif

#ifndef STATIC_DLL

// Driver Init
typedef int (RT_IMPORT *PDEWEDRIVERINIT)(int *num_boards);
typedef int (RT_IMPORT *PDEWEDRIVERDEINIT)( void );

// _i32 functions
typedef int (RT_IMPORT *PDEWEGETPARAM_I32)(int board_no, unsigned int command_id, sint32 *val);
typedef int (RT_IMPORT *PDEWESETPARAM_I32)(int board_no, unsigned int command_id, sint32 val);

// _i64 functions
typedef int (RT_IMPORT *PDEWEGETPARAM_I64)(int board_no, unsigned int command_id, sint64 *val);
typedef int (RT_IMPORT *PDEWESETPARAM_I64)(int board_no, unsigned int command_id, sint64 val);

// string based functions
typedef int (RT_IMPORT *PDEWESETPARAMSTRUCT_STR)( const char *target, const char *command, const char *val);
typedef int (RT_IMPORT *PDEWEGETPARAMSTRUCT_STR)( const char *target, const char *command, char *val, uint32 val_size);
typedef int (RT_IMPORT *PDEWEGETPARAMSTRUCT_STRLEN)( const char *target, const char *command, uint32 *val_size);

typedef int (RT_IMPORT *PDEWEGETPARAMSTRUCTEX_STR)( const char *target, const char *command, const char *arg, char *val, uint32 val_size);

typedef int (RT_IMPORT *PDEWESETPARAMXML_STR)( const char *target, const char *command, const char *val);
typedef int (RT_IMPORT *PDEWEGETPARAMXML_STR)( const char *target, const char *command, char *val, uint32 val_size);
typedef int (RT_IMPORT *PDEWEGETPARAMXML_STRLEN)( const char *target, const char *command, uint32 *val_size);

// CAN functions
typedef int (RT_IMPORT *PDEWEOPENCAN)(int board_no);
typedef int (RT_IMPORT *PDEWECLOSECAN)(int board_no);
typedef int (RT_IMPORT *PDEWEGETCHANNELPROPCAN)(int board_no, int nChannelNo, PBOARD_CAN_CHANNEL_PROP pProp);
typedef int (RT_IMPORT *PDEWESETCHANNELPROPCAN)(int board_no, int nChannelNo, BOARD_CAN_CHANNEL_PROP rProp);
typedef int (RT_IMPORT *PDEWESTARTCAN)(int board_no, int nChannelNo);
typedef int (RT_IMPORT *PDEWESTOPCAN)(int board_no, int nChannelNo);
typedef int (RT_IMPORT *PDEWEFREEFRAMESCAN)(int board_no, int nFrameCount);
typedef int (RT_IMPORT *PDEWEERRORCNTCAN)(int board_no, int nChannelNo, int *nErrorCount);

// CAN Read/Write
typedef int (RT_IMPORT* PDEWEREADCAN)(int board_no, PBOARD_CAN_FRAME pCanFrames, int nMaxFrameCount, int* nRealFrameCount);
typedef int (RT_IMPORT* PDEWEREADCANRAWFRAME)(int board_no, PBOARD_CAN_RAW_FRAME* pCanFrames, int* nRealFrameCount);
typedef int (RT_IMPORT* PDEWEWRITECAN)(int board_no, PBOARD_CAN_FRAME pCanFrames, int nMaxFrameCount, int* nRealFrameCount);

// CAN-FD Read/Write
typedef int (RT_IMPORT *PDEWEREADCANEX)(int board_no, BOARD_CAN_FD_FRAME* pCanFrames, int nMaxFrameCount, int *nRealFrameCount);
typedef int (RT_IMPORT *PDEWEREADCANRAWFRAMEEX)(int board_no, BOARD_CAN_FD_RAW_FRAME* pCanFrames, int nMaxFrameCount, int *nRealFrameCount);
typedef int (RT_IMPORT *PDEWEWRITECANEX)(int board_no, BOARD_CAN_FD_FRAME* pCanFrames, int nMaxFrameCount, int *nRealFrameCount);

// Asynchronous channel(UART) functions
typedef int (RT_IMPORT *PDEWEOPENDMAUART)(int board_no);
typedef int (RT_IMPORT *PDEWECLOSEDMAUART)(int board_no);
typedef int (RT_IMPORT *PDEWEGETCHANNELPROPDMAUART)(int board_no, int nChannelNo, PBOARD_UART_CHANNEL_PROP pProp);
typedef int (RT_IMPORT *PDEWESETCHANNELPROPDMAUART)(int board_no, int nChannelNo, BOARD_UART_CHANNEL_PROP rProp);
typedef int (RT_IMPORT *PDEWESTARTDMAUART)(int board_no, int nChannelNo);
typedef int (RT_IMPORT *PDEWESTOPDMAUART)(int board_no, int nChannelNo);
typedef int (RT_IMPORT *PDEWEREADDMAUART)(int board_no, PBOARD_UART_FRAME pUartFrames, int nMaxFrameCount, int *nRealFrameCount);
typedef int (RT_IMPORT *PDEWEREADDMAUARTRAWFRAME)(int board_no, PBOARD_UART_RAW_FRAME *pUartFrames, int *nRealFrameCount);
typedef int (RT_IMPORT *PDEWEFREEDMAUARTRAWFRAME)(int board_no, int framestofree);
typedef int (RT_IMPORT *PDEWEWRITEDMAUART)(int board_no, PBOARD_UART_FRAME pUartFrames, int nFrameCount, int *nRealFrameCount);

// Obtain readable ErrorMessage from ErrorCode
typedef const char* (RT_IMPORT *PDEWEERRORCONSTANTTOSTRING) ( int );


//###############################################################################################################################################

// Driver Init
RTLOAD PDEWEDRIVERINIT				DeWeDriverInit;
RTLOAD PDEWEDRIVERDEINIT			DeWeDriverDeInit;

// _i32 functions
RTLOAD PDEWEGETPARAM_I32			DeWeGetParam_i32;
RTLOAD PDEWESETPARAM_I32			DeWeSetParam_i32;

// _i64 functions
RTLOAD PDEWEGETPARAM_I64			DeWeGetParam_i64;
RTLOAD PDEWESETPARAM_I64			DeWeSetParam_i64;

// string based functions
RTLOAD PDEWESETPARAMSTRUCT_STR			DeWeSetParamStruct_str;
RTLOAD PDEWEGETPARAMSTRUCT_STR			DeWeGetParamStruct_str;
RTLOAD PDEWEGETPARAMSTRUCT_STRLEN		DeWeGetParamStruct_strLEN;

RTLOAD PDEWEGETPARAMSTRUCTEX_STR		DeWeGetParamStructEx_str;

RTLOAD PDEWESETPARAMXML_STR				DeWeSetParamXML_str;
RTLOAD PDEWEGETPARAMXML_STR				DeWeGetParamXML_str;
RTLOAD PDEWEGETPARAMXML_STRLEN		    DeWeGetParamXML_strLEN;

// CAN functions
RTLOAD PDEWEOPENCAN					DeWeOpenCAN;
RTLOAD PDEWECLOSECAN				DeWeCloseCAN;
RTLOAD PDEWEGETCHANNELPROPCAN		DeWeGetChannelPropCAN;
RTLOAD PDEWESETCHANNELPROPCAN		DeWeSetChannelPropCAN;
RTLOAD PDEWESTARTCAN				DeWeStartCAN;
RTLOAD PDEWESTOPCAN					DeWeStopCAN;
RTLOAD PDEWEFREEFRAMESCAN           DeWeFreeFramesCAN;
RTLOAD PDEWEERRORCNTCAN				DeWeErrorCntCAN;

// CAN Read/Write
RTLOAD PDEWEREADCAN					DeWeReadCAN;
RTLOAD PDEWEREADCANRAWFRAME			DeWeReadCANRawFrame;
RTLOAD PDEWEWRITECAN				DeWeWriteCAN;

// CAN FD Read/Write
RTLOAD PDEWEREADCANEX               DeWeReadCANEx;
RTLOAD PDEWEREADCANRAWFRAMEEX       DeWeReadCANRawFrameEx;
RTLOAD PDEWEWRITECANEX				DeWeWriteCANEx;

// Asynchronous channel(UART) functions
RTLOAD PDEWEOPENDMAUART				DeWeOpenDmaUart;
RTLOAD PDEWECLOSEDMAUART			DeWeCloseDmaUart;
RTLOAD PDEWEGETCHANNELPROPDMAUART	DeWeGetChannelPropDmaUart;
RTLOAD PDEWESETCHANNELPROPDMAUART	DeWeSetChannelPropDmaUart;
RTLOAD PDEWESTARTDMAUART			DeWeStartDmaUart;
RTLOAD PDEWESTOPDMAUART				DeWeStopDmaUart;
RTLOAD PDEWEREADDMAUART				DeWeReadDmaUart;
RTLOAD PDEWEREADDMAUARTRAWFRAME		DeWeReadDmaUartRawFrame;
RTLOAD PDEWEFREEDMAUARTRAWFRAME     DeWeFreeDmaUartRawFrame;
RTLOAD PDEWEWRITEDMAUART			DeWeWriteDmaUart;

// Obtain readable ErrorMessage from ErroCode
RTLOAD PDEWEERRORCONSTANTTOSTRING		DeWeErrorConstantToString;



#else


//#############################################################
//#
//#
//# PXI API Functions - Static
//#
//#
//#############################################################

#ifdef __cplusplus
extern "C"{
#endif

// Driver Init
int RT_IMPORT DeWeDriverInit(int *nNumOfBoard);
int RT_IMPORT DeWeDriverDeInit( void );

// _i32 functions
int RT_IMPORT DeWeGetParam_i32(int board_no, unsigned int command_id, sint32 *val);
int RT_IMPORT DeWeSetParam_i32(int board_no, unsigned int command_id, sint32 val);

// _i64 functions
int RT_IMPORT DeWeGetParam_i64(int board_no, unsigned int command_id, sint64 *val);
int RT_IMPORT DeWeSetParam_i64(int board_no, unsigned int command_id, sint64 val);

// string based functions
int RT_IMPORT DeWeSetParamStruct_str (const char *target, const char *command, const char *val);
int RT_IMPORT DeWeGetParamStruct_str (const char *target, const char *command, char *val, uint32 num);
int RT_IMPORT DeWeGetParamStruct_strLEN( const char	*target, const char	*command, uint32 *val_size);

int RT_IMPORT DeWeGetParamStructEx_str (const char *target, const char *command, const char *arg, char *val, uint32 val_size);

int RT_IMPORT DeWeSetParamXML_str (const char *target, const char *command, const char *val);
int RT_IMPORT DeWeGetParamXML_str (const char *target, const char *command, char *val, uint32 num);
int RT_IMPORT DeWeGetParamXML_strLEN( const char *target, const char *command, uint32 *val_size);

// CAN functions
int RT_IMPORT DeWeOpenCAN(int board_no);
int RT_IMPORT DeWeCloseCAN(int board_no);
int RT_IMPORT DeWeGetChannelPropCAN(int board_no, int nChannelNo, PBOARD_CAN_CHANNEL_PROP pProp);
int RT_IMPORT DeWeSetChannelPropCAN(int board_no, int nChannelNo, BOARD_CAN_CHANNEL_PROP rProp);
int RT_IMPORT DeWeStartCAN(int board_no, int nChannelNo);
int RT_IMPORT DeWeStopCAN(int board_no, int nChannelNo);
int RT_IMPORT DeWeFreeFramesCAN(int board_no, int nFrameCount);
int RT_IMPORT DeWeErrorCntCAN(int board_no, int nChannelNo, int *nErrorCount);

// CAN Read/Write
int RT_IMPORT DeWeReadCAN(int board_no, PBOARD_CAN_FRAME pCanFrames, int nMaxFrameCount, int* nRealFrameCount);
int RT_IMPORT DeWeReadCANRawFrame(int board_no, PBOARD_CAN_RAW_FRAME* pCanFrames, int* nRealFrameCount);
int RT_IMPORT DeWeWriteCAN(int board_no, PBOARD_CAN_FRAME pCanFrames, int nFrameCount, int* nRealFrameCount);

// CAN FD Read/Write
int RT_IMPORT DeWeReadCANEx(int board_no, BOARD_CAN_FD_FRAME* pCanFrames, int nMaxFrameCount, int *nRealFrameCount);
int RT_IMPORT DeWeReadCANRawFrameEx(int board_no, PBOARD_CAN_FD_RAW_FRAME* pCanFrames, int nMaxFrameCount, int *nRealFrameCount);
int RT_IMPORT DeWeWriteCANEx(int board, BOARD_CAN_FD_FRAME* frames, int max_frame_cnt, int* frame_cnt);

// Asynchronous channel(UART) functions
int RT_IMPORT DeWeOpenDmaUart(int board_no);
int RT_IMPORT DeWeCloseDmaUart(int board_no);
int RT_IMPORT DeWeGetChannelPropDmaUart(int board_no, int nChannelNo, PBOARD_UART_CHANNEL_PROP pProp);
int RT_IMPORT DeWeSetChannelPropDmaUart(int board_no, int nChannelNo, BOARD_UART_CHANNEL_PROP rProp);
int RT_IMPORT DeWeStartDmaUart(int board_no, int nChannelNo);
int RT_IMPORT DeWeStopDmaUart(int board_no, int nChannelNo);
int RT_IMPORT DeWeReadDmaUart(int board_no, PBOARD_UART_FRAME pUartFrames, int nMaxFrameCount, int *nRealFrameCount);
int RT_IMPORT DeWeReadDmaUartRawFrame(int board_no, PBOARD_UART_RAW_FRAME *pUartFrames, int *nRealFrameCount);
int RT_IMPORT DeWeFreeDmaUartRawFrame(int board_no, int nFrameCount);
int RT_IMPORT DeWeWriteDmaUart(int board_no, PBOARD_UART_FRAME pUartFrames, int nFrameCount, int *nRealFrameCount);

// Obtain readable ErrorMessage from ErrorCode
const char* RT_IMPORT DeWeErrorConstantToString ( int nErrorCode );

#ifdef __cplusplus
}
#endif

#endif  // not STATIC_DLL

#endif // DewePXI_apiCore_h__
