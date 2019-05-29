/*
Type - Definitions Used externaly aswell...
*/

#ifndef __DEWEPXI_TYPES_H
#define __DEWEPXI_TYPES_H

typedef signed char       sint8;
typedef signed short      sint16;
typedef signed int        sint32;
typedef signed long long  sint64;

typedef unsigned char       uint8;
typedef unsigned short      uint16;
typedef unsigned int        uint32;
typedef unsigned long long  uint64;

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

typedef struct tagBOARD_CAN_CHANNEL_PROP {
    uint32           Used;           // Only allowed if DeWeStopCAN is performed...
    uint32           Mode;
    uint32           BaudRate;       // CAN Baudrate
    uint32           ListenOnly;     // Sending no ACK
    uint32           Termination;    // Switching on the termination
    uint32           PhyLayer;       // Reserved for: High_Speed, Low_Speed, Single_Wire
    uint32           SyncCounter;    // SampleCount, 10MHz count
    unsigned char   Sjw;
    unsigned char   Tseg1;
    unsigned char   Tseg2;
    unsigned char   Sam;  // 1 or 3
} BOARD_CAN_CHANNEL_PROP, *PBOARD_CAN_CHANNEL_PROP;


typedef struct tagBOARD_UART_CHANNEL_PROP {
    uint32          Used;           // Only allowed if DeWeStopAsync is performed...
    uint32          Mode;
    uint32          BaudRate;       // Baud Rate
    uint32          LastSetBaudrate;// holds the last sucessfully set Baudrate (0 if none has been set yet)
    uint32          DataBit;        // Reserved, current Uart implementation supports only 8 data bits
    uint32          StopBit;        // Reserved, current Uart implementation supports only 1 stop bit
    uint32          Handshake;      // Reserved, current Uart implementation supports only no handshake
    uint32          SyncCounter;    // Off, SampleCount, 1MHz count (10MHz count)
    uint32          ReceiverDynamic;
    uint32          PositionSmoothing; // Normal, Glide
    uint32          SBAS;           // WAAS, EGNOS, MSAS...
    uint32          VelocityType;   // Position or Doppler
    uint32          Msg_ZDA;        // ZDA Message enabled or disabled
    uint32          Msg_ZDA_Rate;   // Update rate of Message in Hz
    uint32          Msg_GGA;        // GGA Message enabled or disabled
    uint32          Msg_GGA_Rate;   // Update rate of Message in Hz
    uint32          Msg_GLL;        // GLL Message enabled or disabled
    uint32          Msg_GLL_Rate;   // Update rate of Message in Hz
    uint32          Msg_GSA;        // GSA Message enabled or disabled
    uint32          Msg_GSA_Rate;   // Update rate of Message in Hz
    uint32          Msg_GSV;        // GSV Message enabled or disabled
    uint32          Msg_GSV_Rate;   // Update rate of Message in Hz
    uint32          Msg_RMC;        // RMC Message enabled or disabled
    uint32          Msg_RMC_Rate;   // Update rate of Message in Hz
    uint32          Msg_VTG;        // VTG Message enabled or disabled
    uint32          Msg_VTG_Rate;   // Update rate of Message in Hz
} BOARD_UART_CHANNEL_PROP, *PBOARD_UART_CHANNEL_PROP;

/*****************************************/
typedef struct tagBOARD_CAN_FRAME {
    uint8       CanNo;
    uint8       reserved0;
    uint8       reserved1;
    uint8       reserved2;
    uint32      MessageId;          // Aribtration ID
    uint32      DataLength;         // DLC
    uint8       CanData[8];
    uint32      StandardExtended;   // Identifier Type : Standard ID or Extended ID
    uint32      FrameType;          // Frame Type : Normal Frame or Remote Frame
    uint32      SyncCounter;        // Depending on Config: Sample CNT or 10MHz CNT (will be used)
    uint32      ErrorCounter;       // reseted at start of acquisition?
    uint64      SyncCounterEx;      //64 Bit timestamp with internal roll-over handling
} BOARD_CAN_FRAME, *PBOARD_CAN_FRAME;


typedef struct tagBOARD_CAN_RAW_FRAME {
    uint32 Hdr;
    uint32 Err;
    uint32 Pos;
    uint8  Data[8];
	uint32 Dummy[3];
} BOARD_CAN_RAW_FRAME, *PBOARD_CAN_RAW_FRAME;


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

/*
typedef struct tagBOARD_UART_RAW_FRAME {
    uint8		Data;
	uint32		SyncCounter;
	uint32		LastPPS;
} BOARD_UART_RAW_FRAME, *PBOARD_UART_RAW_FRAME;
*/




#endif //__DEWEPXI_TYPES_H
