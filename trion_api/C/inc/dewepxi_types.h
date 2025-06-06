/*
 * Copyright (c) 2013 DEWETRON
 * License: MIT
 *
 * API interface type definitions
 */


#ifndef __DEWEPXI_TYPES_H
#define __DEWEPXI_TYPES_H

#ifdef WIN32

typedef signed char       sint8;
typedef signed short      sint16;
typedef signed int        sint32;
typedef signed long long  sint64;

typedef unsigned char       uint8;
typedef unsigned short      uint16;
typedef unsigned int        uint32;
typedef unsigned long long  uint64;

#else

#include <stdint.h>

typedef int8_t  sint8;
typedef int16_t sint16;
typedef int32_t sint32;
typedef int64_t sint64;

typedef uint8_t     uint8;
typedef uint16_t    uint16;
typedef uint32_t    uint32;
typedef uint64_t    uint64;

#endif

// deprecated (dirty grin) convenience types
typedef uint8 BOOLEAN;
typedef long long INT64;
typedef int INT;
typedef int BOOL;
typedef unsigned long DWORD;
typedef void* HANDLE;
typedef HANDLE* PHANDLE;

#ifndef TRUE
#define TRUE 1
#endif

#ifndef FALSE
#define FALSE 0
#endif


/**********************************************
*  Type define
**********************************************/



// Structure for CAN Channels

/**
 * CAN Frame data structure
 */
typedef struct tagBOARD_CAN_FRAME {
    uint8       CanNo;
    uint8       reserved0;
    uint8       reserved1;
    uint8       reserved2;
    uint32      MessageId;          // Arbitration ID
    uint32      DataLength;         // DLC
    uint8       CanData[8];         // Data of CAN frame
    uint32      StandardExtended;   // Identifier Type : Standard ID or Extended ID
    uint32      FrameType;          // Frame Type : Normal Frame or Remote Frame
    uint32      SyncCounter;        // Depending on Config: Sample CNT or 10MHz CNT (will be used)
    uint32      ErrorCounter;       // reseted at start of acquisition?
    uint64      SyncCounterEx;      // 64 Bit timestamp with internal roll-over handling
} BOARD_CAN_FRAME, *PBOARD_CAN_FRAME;


/**
 * CAN-FD Frame FrameType Bit-Encoding
 */
// Normal / Remote Flag
// RTR
#define CAN_FD_FRAMETYPE_NORMAL_REMOTE_OFFSET   0
#define CAN_FD_FRAMETYPE_NORMAL_REMOTE_MASK     (1 << CAN_FD_FRAMETYPE_NORMAL_REMOTE_OFFSET)
#define CAN_FD_FRAMETYPE_NORMAL_REMOTE_NORMAL   (0 << CAN_FD_FRAMETYPE_NORMAL_REMOTE_OFFSET)
#define CAN_FD_FRAMETYPE_NORMAL_REMOTE_REMOTE   (1 << CAN_FD_FRAMETYPE_NORMAL_REMOTE_OFFSET)

// CAN / CAN-FD flag
// used for TX only
// FDF Flexible Data-rate Format. Distinguishes between CAN 2.0 and CAN FD Frames.
#define CAN_FD_FRAMETYPE_CAN_CAN_FD_OFFSET      1
#define CAN_FD_FRAMETYPE_CAN_FDF_MASK           (1 << CAN_FD_FRAMETYPE_CAN_CAN_FD_OFFSET)
#define CAN_FD_FRAMETYPE_CAN_NO_FDF             (0 << CAN_FD_FRAMETYPE_CAN_CAN_FD_OFFSET)
#define CAN_FD_FRAMETYPE_CAN_FDF                (1 << CAN_FD_FRAMETYPE_CAN_CAN_FD_OFFSET)

// BRS Bitrate Switch. In case of CAN FD frames indicates whether bit rate is switched.
#define CAN_FD_FRAMETYPE_BRS_OFFSET             2
#define CAN_FD_FRAMETYPE_BRS_MASK               (1 << CAN_FD_FRAMETYPE_BRS_OFFSET)
#define CAN_FD_FRAMETYPE_NO_BRS                 (0 << CAN_FD_FRAMETYPE_BRS_OFFSET)
#define CAN_FD_FRAMETYPE_BRS                    (1 << CAN_FD_FRAMETYPE_BRS_OFFSET)

// ESI_RSV Error State Indicator bit for received CAN FD frames. Bit has no meaning for CAN frames.
#define CAN_FD_FRAMETYPE_ESI_RSV_OFFSET         3
#define CAN_FD_FRAMETYPE_ESI_RSV_MASK           (1 << CAN_FD_FRAMETYPE_ESI_RSV_OFFSET)
#define CAN_FD_FRAMETYPE_NO_ESI_RSV             (0 << CAN_FD_FRAMETYPE_ESI_RSV_OFFSET)
#define CAN_FD_FRAMETYPE_ESI_RSV                (1 << CAN_FD_FRAMETYPE_ESI_RSV_OFFSET)

/**
 * CAN-FD Frame data structure
 */
typedef struct tagBOARD_CAN_FD_FRAME {
    uint8       CanNo;
    uint8       reserved0;
    uint8       reserved1;
    uint8       reserved2;
    uint32      MessageId;          // Arbitration ID
    uint32      DataLength;         // DLC
    uint32      StandardExtended;   // Identifier Type : Standard ID or Extended ID
    uint32      FrameType;          // Frame Type : see above for encoding
    uint32      SyncCounter;        // Depending on Config: Sample CNT or 10MHz CNT (will be used)
    uint32      ErrorCounter;       // reseted at start of acquisition?
    uint64      SyncCounterEx;      // 64 Bit timestamp with internal roll-over handling
    uint8       CanData[64];        // Data of CAN FD frame
} BOARD_CAN_FD_FRAME, *PBOARD_CAN_FD_FRAME;

#define CAN_RAW_FLAG_EXTENDED_TS        0x80000000  // tv_sec and tv_usec used
#define CAN_RAW_FLAG_CAN_DATA_REVERSED  0x40000000  // 1: Data is in reversed order, 0: in normal order

/**
 * CAN Raw Frame
 * The raw frame format provided by TRION CAN boards
 */
typedef struct tagBOARD_CAN_RAW_FRAME {
    uint32 Hdr;
    uint32 Err;
    uint32 Pos;
    uint8  Data[8];     // Data of CAN frame
    uint32 tv_sec;      // used for exact timestamps (seconds)
    uint32 tv_usec;     // used for exact timestamps (microseconds)
    uint32 flags;       // Extended ts, data ordering
} BOARD_CAN_RAW_FRAME, *PBOARD_CAN_RAW_FRAME;

/**
 * CAN-FD Raw Frame
 * The raw frame format provided by TRION CAN-FD boards
 */
typedef struct tagBOARD_CAN_FD_RAW_FRAME {
    uint32 Hdr;
    uint32 Err;
    uint32 Pos;
    uint32 tv_sec;      // used for exact timestamps (seconds)
    uint32 tv_usec;     // used for exact timestamps (microseconds)
    uint32 flags;
    uint8  Data[64];    // Data of CAN FD frame
} BOARD_CAN_FD_RAW_FRAME, *PBOARD_CAN_FD_RAW_FRAME;


typedef struct tagBOARD_UART_FRAME {
    uint8       Data;
    uint8       reserved0;
    uint8       reserved1;
    uint8       reserved2;
    uint32      UartNo;
    uint64      LastPPS;        //will hold the sync-counter value of the last PPS
    uint64      SyncCounter;
} BOARD_UART_FRAME, *PBOARD_UART_FRAME;

typedef struct tagBOARD_UART_RAW_FRAME {
    /*************************************
     | Byte 3 | Byte 2 | Byte 1 | Byte 0 |
     |-----------------------------------|
     |          lastpps         |  data  |
     *************************************/
    uint32      data_lastpps;     //dt|ts1|ts2|ts3
    uint32      SyncCounter;
}BOARD_UART_RAW_FRAME, *PBOARD_UART_RAW_FRAME;


/**
 * deprecated
 */
typedef struct tagBOARD_CAN_CHANNEL_PROP {
    uint32          _deprecated;
} BOARD_CAN_CHANNEL_PROP, * PBOARD_CAN_CHANNEL_PROP;

typedef struct tagBOARD_UART_CHANNEL_PROP {
    uint32          _deprecated;
} BOARD_UART_CHANNEL_PROP, * PBOARD_UART_CHANNEL_PROP;

#endif //__DEWEPXI_TYPES_H
