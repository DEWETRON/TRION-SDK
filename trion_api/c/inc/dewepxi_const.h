/*! \file dewepxi_const.h
    \brief File containing all externally used constants

   Error-Constants are stored seperatly
   in dewepxi_err.h, dewepxi_err.pas
   (Both files will also be used as internal sources for compiling the API itself)
*/

#ifndef __DEWEPXI_CONST_H
#define __DEWEPXI_CONST_H


//---------------------------------------------------------------------------
//--------------------------------
// Command ID for i32
//--------------------------------
enum
{

#define TRION_COMMAND(name, val) CMD_##name = val,
#include "dewepxi_commands.inc"
#undef TRION_COMMAND
};

// Update Parameter Value
#define PARAM_BUFFER_START_POS            0x0510  // Get
#define PARAM_BUFFER_END_POS              0x0511  // Get
#define PARAM_BUFFER_CLEAR_ERROR          0x0512  // Set

#define PARAM_BUFFER_TOTAL_MEM_SIZE       0x0513  // Get
#define PARAM_BUFFER_ONE_BLOCK_SIZE       0x0514  // Get
#define PARAM_BUFFER_ONE_SCAN_SIZE        0x0515  // Get

#define PARAM_ASYNC_POLLING_TIME          0x0553  // Set dword ms
#define PARAM_ASYNC_FRAME_SIZE            0x0554  // Set

#define PARAM_TIME_OUT                    0x0a01  // Set dword
#define PARAM_LAST_ERROR                  0x0a02  // Get

#define	PARAM_NOT_USED  				0x7FFFFC00
#define	PARAM_NOT_KNOWN					0x7FFFF800
#define	PARAM_AUTO						0x7FFFF400
#define	PARAM_OFF						0x7FFFF200

#define UPDATE_ALL_CHANNELS	        	100000
#define UPDATE_GROUP_CHANNELS       	100001
#define UPDATE_ALL_BOARDS	        	100010

//Returncodes for CMD_ACQ_STATE
#define ACQ_STATE_ERROR                 0xFFFF
#define ACQ_STATE_IDLE                  0x0000
#define ACQ_STATE_SYNCED                0x0001
#define ACQ_STATE_RUNNING               0x0003

//Returncodes for CMD_TIMING_STATE
#define TIMINGSTATE_LOCKED				0x0001	//Locked
#define TIMINGSTATE_NOTRESYNCED			0x0002	//NotReSynced
#define TIMINGSTATE_UNLOCKED			0x0003	//Unlocked
#define TIMINGSTATE_LOCKEDOOR           0x0004	//OOR
#define TIMINGSTATE_TIMEERROR			0x0005	//TimeError
#define TIMINGSTATE_RELOCKOOR			0x0006	//RelockOOR
#define TIMINGSTATE_NOTIMINGMODE		0xFFFF	//NoTimingMode

//FilterBits for CMD_PXI_LINE_STATE
#define PXI_LINE_STATE_TRIG0            (0x00000001 << 0)
#define PXI_LINE_STATE_TRIG1            (0x00000001 << 1)
#define PXI_LINE_STATE_TRIG2            (0x00000001 << 2)
#define PXI_LINE_STATE_TRIG3            (0x00000001 << 3)
#define PXI_LINE_STATE_TRIG4            (0x00000001 << 4)
#define PXI_LINE_STATE_TRIG5            (0x00000001 << 5)
#define PXI_LINE_STATE_TRIG6            (0x00000001 << 6)
#define PXI_LINE_STATE_TRIG7            (0x00000001 << 7)
#define PXI_LINE_STATE_LBR0             (0x00000010 << 0)
#define PXI_LINE_STATE_LBR1             (0x00000010 << 1)
#define PXI_LINE_STATE_LBR2             (0x00000010 << 2)
#define PXI_LINE_STATE_LBR3             (0x00000010 << 3)
#define PXI_LINE_STATE_LBR4             (0x00000010 << 4)
#define PXI_LINE_STATE_LBR5             (0x00000010 << 5)
#define PXI_LINE_STATE_LBR6             (0x00000010 << 6)
#define PXI_LINE_STATE_STAR             (0x00000080)
#define PXI_LINE_STATE_LBL0             (0x00000100 << 0)
#define PXI_LINE_STATE_LBL1             (0x00000100 << 1)
#define PXI_LINE_STATE_LBL2             (0x00000100 << 2)
#define PXI_LINE_STATE_LBL3             (0x00000100 << 3)
#define PXI_LINE_STATE_LBL4             (0x00000100 << 4)
#define PXI_LINE_STATE_LBL5             (0x00000100 << 5)
#define PXI_LINE_STATE_LBL6             (0x00000100 << 6)
#define PXI_LINE_STATE_LBL7             (0x00000100 << 7)
#define PXI_LINE_STATE_LBL8             (0x00000100 << 8)
#define PXI_LINE_STATE_LBL9             (0x00000100 << 9)
#define PXI_LINE_STATE_LBL10            (0x00000100 << 10)
#define PXI_LINE_STATE_LBL11            (0x00000100 << 11)
#define PXI_LINE_STATE_LBL12            (0x00000100 << 12)

//LED - defines for ID_LED command
#define IDLED_COL_OFF                   0
#define IDLED_COL_RED                   1
#define IDLED_COL_GREEN                 2
#define IDLED_COL_ORANGE                3

#endif //__DEWEPXI_CONT_H
