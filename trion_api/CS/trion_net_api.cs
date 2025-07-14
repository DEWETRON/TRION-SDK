using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Trion
{

    [StructLayout(LayoutKind.Sequential), Serializable]
    public struct BOARD_CAN_FRAME
    {
        public byte CanNo;
        public byte reserved0;
        public byte reserved1;
        public byte reserved2;
        public uint MessageId;          // Arbitration ID
        public uint DataLength;         // DLC
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] CanData;
        public uint StandardExtended;   // Identifier Type : Standard ID or Extended ID
        public uint FrameType;          // Frame Type : Normal Frame or Remote Frame
        public uint SyncCounter;        // Depending on Config: Sample CNT or 10MHz CNT (will be used)
        public uint ErrorCounter;       // reseted at start of acquisition?
        public ulong SyncCounterEx;     //64 Bit timestamp with internal roll-over handling
    };

    public class API
    {
        public enum Backend
        {
            TRION,
            TRIONET
        };


        public static Trion.TrionError DeWeConfigure(Backend backend)
        {
            bool is64bit = System.Environment.Is64BitProcess;

            if (is64bit)
            {
                if (backend == Backend.TRION)
                {
                    _dewe_driver_init = Trion_x64.API.DeWeDriverInit;
                    _dewe_driver_deinit = Trion_x64.API.DeWeDriverDeInit;
                    _dewe_get_param_i32 = Trion_x64.API.DeWeGetParam_i32;
                    _dewe_set_param_i32 = Trion_x64.API.DeWeSetParam_i32;
                    _dewe_get_param_i64 = Trion_x64.API.DeWeGetParam_i64;
                    _dewe_set_param_i64 = Trion_x64.API.DeWeSetParam_i64;
                    _dewe_set_param_struct_str = Trion_x64.API.DeWeSetParamStruct_str;
                    _dewe_get_param_struct_str = Trion_x64.API.DeWeGetParamStruct_str;
                    _dewe_get_param_struct_str_len = Trion_x64.API.DeWeGetParamStruct_strLEN;
                    _dewe_set_param_xml_str = Trion_x64.API.DeWeSetParamXML_str;
                    _dewe_get_param_xml_str = Trion_x64.API.DeWeGetParamXML_str;
                    _dewe_get_param_xml_str_len = Trion_x64.API.DeWeGetParamXML_strLEN;

                    _dewe_open_can = Trion_x64.API.DeWeOpenCAN;
                    _dewe_close_can = Trion_x64.API.DeWeCloseCAN;
                    _dewe_start_can = Trion_x64.API.DeWeStartCAN;
                    _dewe_stop_can = Trion_x64.API.DeWeStopCAN;
                    _dewe_read_can = Trion_x64.API.DeWeReadCAN;
                    //_dewe_error_cnt_can;

                    //_dewe_open_dma_uart;
                    //_dewe_close_dma_uart;
                    //_dewe_start_dma_uart;
                    //_dewe_stop_dma_uart;
                    //_dewe_free_dma_uart_raw_frame;

                    _dewe_error_constant_to_string = Trion_x64.API.DeWeErrorConstantToString;

                    return Trion.TrionError.NONE;

                }
                else if (backend == Backend.TRIONET)
                {
                    _dewe_driver_init = TrionNET_x64.API.DeWeDriverInit;
                    _dewe_driver_deinit = TrionNET_x64.API.DeWeDriverDeInit;
                    _dewe_get_param_i32 = TrionNET_x64.API.DeWeGetParam_i32;
                    _dewe_set_param_i32 = TrionNET_x64.API.DeWeSetParam_i32;
                    _dewe_get_param_i64 = TrionNET_x64.API.DeWeGetParam_i64;
                    _dewe_set_param_i64 = TrionNET_x64.API.DeWeSetParam_i64;
                    _dewe_set_param_struct_str = TrionNET_x64.API.DeWeSetParamStruct_str;
                    _dewe_get_param_struct_str = TrionNET_x64.API.DeWeGetParamStruct_str;
                    _dewe_get_param_struct_str_len = TrionNET_x64.API.DeWeGetParamStruct_strLEN;
                    _dewe_set_param_xml_str = TrionNET_x64.API.DeWeSetParamXML_str;
                    _dewe_get_param_xml_str = TrionNET_x64.API.DeWeGetParamXML_str;
                    _dewe_get_param_xml_str_len = TrionNET_x64.API.DeWeGetParamXML_strLEN;

                    _dewe_open_can = TrionNET_x64.API.DeWeOpenCAN;
                    _dewe_close_can = TrionNET_x64.API.DeWeCloseCAN;
                    _dewe_start_can = TrionNET_x64.API.DeWeStartCAN;
                    _dewe_stop_can = TrionNET_x64.API.DeWeStopCAN;
                    _dewe_read_can = TrionNET_x64.API.DeWeReadCAN;

                    _dewe_error_constant_to_string = TrionNET_x64.API.DeWeErrorConstantToString;

                    return Trion.TrionError.NONE;
                }
            }
            else
            {
                if (backend == Backend.TRION)
                {
                    _dewe_driver_init = Trion_x86.API.DeWeDriverInit;
                    _dewe_driver_deinit = Trion_x86.API.DeWeDriverDeInit;
                    _dewe_get_param_i32 = Trion_x86.API.DeWeGetParam_i32;
                    _dewe_set_param_i32 = Trion_x86.API.DeWeSetParam_i32;
                    _dewe_get_param_i64 = Trion_x86.API.DeWeGetParam_i64;
                    _dewe_set_param_i64 = Trion_x86.API.DeWeSetParam_i64;
                    _dewe_set_param_struct_str = Trion_x86.API.DeWeSetParamStruct_str;
                    _dewe_get_param_struct_str = Trion_x86.API.DeWeGetParamStruct_str;
                    _dewe_get_param_struct_str_len = Trion_x86.API.DeWeGetParamStruct_strLEN;
                    _dewe_set_param_xml_str = Trion_x86.API.DeWeSetParamXML_str;
                    _dewe_get_param_xml_str = Trion_x86.API.DeWeGetParamXML_str;
                    _dewe_get_param_xml_str_len = Trion_x86.API.DeWeGetParamXML_strLEN;

                    _dewe_open_can = Trion_x86.API.DeWeOpenCAN;
                    _dewe_close_can = Trion_x86.API.DeWeCloseCAN;
                    _dewe_start_can = Trion_x86.API.DeWeStartCAN;
                    _dewe_stop_can = Trion_x86.API.DeWeStopCAN;
                    _dewe_read_can = Trion_x86.API.DeWeReadCAN;

                    _dewe_error_constant_to_string = Trion_x86.API.DeWeErrorConstantToString;

                    return Trion.TrionError.NONE;
                }
                else if (backend == Backend.TRIONET)
                {
                    _dewe_driver_init = TrionNET_x86.API.DeWeDriverInit;
                    _dewe_driver_deinit = TrionNET_x86.API.DeWeDriverDeInit;
                    _dewe_get_param_i32 = TrionNET_x86.API.DeWeGetParam_i32;
                    _dewe_set_param_i32 = TrionNET_x86.API.DeWeSetParam_i32;
                    _dewe_get_param_i64 = TrionNET_x86.API.DeWeGetParam_i64;
                    _dewe_set_param_i64 = TrionNET_x86.API.DeWeSetParam_i64;
                    _dewe_set_param_struct_str = TrionNET_x86.API.DeWeSetParamStruct_str;
                    _dewe_get_param_struct_str = TrionNET_x86.API.DeWeGetParamStruct_str;
                    _dewe_get_param_struct_str_len = TrionNET_x86.API.DeWeGetParamStruct_strLEN;
                    _dewe_set_param_xml_str = TrionNET_x86.API.DeWeSetParamXML_str;
                    _dewe_get_param_xml_str = TrionNET_x86.API.DeWeGetParamXML_str;
                    _dewe_get_param_xml_str_len = TrionNET_x86.API.DeWeGetParamXML_strLEN;

                    _dewe_open_can = TrionNET_x86.API.DeWeOpenCAN;
                    _dewe_close_can = TrionNET_x86.API.DeWeCloseCAN;
                    _dewe_start_can = TrionNET_x86.API.DeWeStartCAN;
                    _dewe_stop_can = TrionNET_x86.API.DeWeStopCAN;
                    _dewe_read_can = TrionNET_x86.API.DeWeReadCAN;

                    _dewe_error_constant_to_string = TrionNET_x86.API.DeWeErrorConstantToString;

                    return Trion.TrionError.NONE;
                }
            }

            return Trion.TrionError.API_NOT_LOADED;
        }


        public static Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard)
        {
            nNumOfBoard = 0;
            if (_dewe_driver_init != null)
            {
                return _dewe_driver_init(out nNumOfBoard);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeDriverDeInit()
        {
            if (_dewe_driver_deinit != null)
            {
                return _dewe_driver_deinit();
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, Trion.TrionCommand nCommandId, out Int32 pVal)
        {
            pVal = 0;
            if (_dewe_get_param_i32 != null)
            {
                return _dewe_get_param_i32(nBoardNo, nCommandId, out pVal);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, Trion.TrionCommand nCommandId, Int32 nVal)
        {
            if (_dewe_set_param_i32 != null)
            {
                return _dewe_set_param_i32(nBoardNo, nCommandId, nVal);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, Trion.TrionCommand nCommandId, out Int64 pVal)
        {
            pVal = 0;
            if (_dewe_get_param_i64 != null)
            {
                return _dewe_get_param_i64(nBoardNo, nCommandId, out pVal);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }


        public static Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, Trion.TrionCommand nCommandId, Int64 nVal)
        {
            if (_dewe_set_param_i64 != null)
            {
                return _dewe_set_param_i64(nBoardNo, nCommandId, nVal);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeSetParamStruct_str(string Target, string Item, string Var)
        {
            if (_dewe_set_param_struct_str != null)
            {
                return _dewe_set_param_struct_str(Target, Item, Var);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }


        public static Trion.TrionError DeWeGetParamStruct_str(string Target, string Item, byte[] Var, UInt32 num)
        {
            if (_dewe_get_param_struct_str != null)
            {
                return _dewe_get_param_struct_str(Target, Item, Var, num);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeGetParamStructEx_str(string Target, string Item, string Arg, byte[] Var, UInt32 num)
        {
            if (_dewe_get_param_struct_ex_str != null)
            {
                return _dewe_get_param_struct_ex_str(Target, Item, Arg, Var, num);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeGetParamStruct_strLEN(string Target, string Item, out UInt32 Len)
        {
            Len = 0;
            if (_dewe_get_param_struct_str_len != null)
            {
                return _dewe_get_param_struct_str_len(Target, Item, out Len);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeSetParamXML_str(string Target, string Item, string Var)
        {
            if (_dewe_set_param_xml_str != null)
            {
                return _dewe_set_param_xml_str(Target, Item, Var);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeGetParamXML_str(string Target, string Item, byte[] Var, UInt32 num)
        {
            if (_dewe_get_param_xml_str != null)
            {
                return _dewe_get_param_xml_str(Target, Item, Var, num);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeGetParamXML_strLEN(string Target, string Item, out UInt32 Len)
        {
            Len = 0;
            if (_dewe_get_param_xml_str_len != null)
            {
                return _dewe_get_param_xml_str_len(Target, Item, out Len);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }


        public static Trion.TrionError DeWeOpenCAN(Int32 nBoardNo)
        {
            if (_dewe_open_can != null)
            {
                return _dewe_open_can(nBoardNo);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeCloseCAN(Int32 nBoardNo)
        {
            if (_dewe_close_can != null)
            {
                return _dewe_close_can(nBoardNo);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeStartCAN(Int32 nBoardNo, Int32 nChannelNo)
        {
            if (_dewe_start_can != null)
            {
                return _dewe_start_can(nBoardNo, nChannelNo);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeStopCAN(Int32 nBoardNo, Int32 nChannelNo)
        {
            if (_dewe_stop_can != null)
            {
                return _dewe_stop_can(nBoardNo, nChannelNo);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeReadCAN(Int32 nBoardNo, ref BOARD_CAN_FRAME[] pCanFrames, Int32 nMaxFrameCount, ref Int32 nRealFrameCount)
        {
            nRealFrameCount = 0;
            if (_dewe_read_can != null)
            {
                Trion.TrionError error = Trion.TrionError.NONE;
                error = _dewe_read_can(nBoardNo, pCanFrames, nMaxFrameCount, out nRealFrameCount);
                return error;
            }

            return Trion.TrionError.API_NOT_LOADED;
        }


        //public static Trion.TrionError DeWeReadCANRawFrame(Int32 nBoardNo, PBOARD_CAN_RAW_FRAME* pCanFrames, out Int32 nRealFrameCount);
        //public static Trion.TrionError DeWeWriteCAN(Int32 nBoardNo, PBOARD_CAN_FRAME pCanFrames, Int32 nMaxFrameCount, out Int32 nRealFrameCount);
        public static Trion.TrionError DeWeErrorCntCAN(Int32 nBoardNo, Int32 nChannelNo, out Int32 nErrorCount)
        {
            nErrorCount = 0;
            if (_dewe_error_cnt_can != null)
            {
                return _dewe_error_cnt_can(nBoardNo, nChannelNo, out nErrorCount);
            }
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num)
        {
            return Trion.TrionError.API_NOT_LOADED;
        }




        public static Trion.TrionError DeWeOpenDmaUart(Int32 nBoardNo)
        {
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeCloseDmaUart(Int32 nBoardNo)
        {
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeStartDmaUart(Int32 nBoardNo, Int32 nChannelNo)
        {
            return Trion.TrionError.API_NOT_LOADED;
        }

        public static Trion.TrionError DeWeStopDmaUart(Int32 nBoardNo, Int32 nChannelNo)
        {
            return Trion.TrionError.API_NOT_LOADED;
        }

        //public static Trion.TrionError DeWeReadDmaUart(Int32 nBoardNo, PBOARD_UART_FRAME pUartFrames, Int32 nMaxFrameCount, Int32* nRealFrameCount);
        //public static Trion.TrionError DeWeReadDmaUartRawFrame(Int32 nBoardNo, PBOARD_UART_RAW_FRAME* pUartFrames, Int32* nRealFrameCount);
        public static Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 framestofree)
        {
            return Trion.TrionError.API_NOT_LOADED;
        }

        //public static Trion.TrionError DeWeWriteDmaUart(Int32 nBoardNo, PBOARD_UART_FRAME pUartFrames, Int32 nFrameCount, Int32* nRealFrameCount);



        public static string DeWeErrorConstantToString(Trion.TrionError nErrorCode)
        {
            if (_dewe_error_constant_to_string != null)
            {
                return _dewe_error_constant_to_string(nErrorCode);
            }
            return "API_NOT_LOADED";
        }


        public static bool CheckError(Trion.TrionError nErrorCode)
        {
            if (nErrorCode > 0)
            {
                System.Console.WriteLine(nErrorCode.ToString());
                return true;
            }
            return false;
        }

        public delegate Trion.TrionError DeWeDriverInitType(out Int32 nNumOfBoard);
        static DeWeDriverInitType _dewe_driver_init = null;
        public delegate Trion.TrionError DeWeDriverDeInitType();
        static DeWeDriverDeInitType _dewe_driver_deinit = null;

        public delegate Trion.TrionError DeWeGetParam_i32Type(Int32 nBoardNo, Trion.TrionCommand nCommandId, out Int32 pVal);
        static DeWeGetParam_i32Type _dewe_get_param_i32 = null;
        public delegate Trion.TrionError DeWeSetParam_i32Type(Int32 nBoardNo, Trion.TrionCommand nCommandId, Int32 pVal);
        static DeWeSetParam_i32Type _dewe_set_param_i32 = null;

        public delegate Trion.TrionError DeWeGetParam_i64Type(Int32 nBoardNo, Trion.TrionCommand nCommandId, out Int64 pVal);
        static DeWeGetParam_i64Type _dewe_get_param_i64 = null;
        public delegate Trion.TrionError DeWeSetParam_i64Type(Int32 nBoardNo, Trion.TrionCommand nCommandId, Int64 pVal);
        static DeWeSetParam_i64Type _dewe_set_param_i64 = null;

        public delegate Trion.TrionError DeWeSetParamStruct_strType(string Target, string Command, string Var);
        static DeWeSetParamStruct_strType _dewe_set_param_struct_str = null;
        public delegate Trion.TrionError DeWeGetParamStruct_strType(string Target, string Item, byte[] Var, UInt32 num);
        static DeWeGetParamStruct_strType _dewe_get_param_struct_str = null;
        public delegate Trion.TrionError DeWeGetParamStructEx_strType(string Target, string Item, string Arg, byte[] val, UInt32 num);
        static DeWeGetParamStructEx_strType _dewe_get_param_struct_ex_str = null;
        public delegate Trion.TrionError DeWeGetParamStruct_strLENType(string Target, string Item, out UInt32 Len);
        static DeWeGetParamStruct_strLENType _dewe_get_param_struct_str_len = null;

        public delegate Trion.TrionError DeWeSetParamXML_strType(string Target, string Command, string Var);
        static DeWeSetParamXML_strType _dewe_set_param_xml_str = null;
        public delegate Trion.TrionError DeWeGetParamXML_strType(string Target, string Item, byte[] Var, UInt32 num);
        static DeWeGetParamXML_strType _dewe_get_param_xml_str = null;
        public delegate Trion.TrionError DeWeGetParamXML_strLENType(string Target, string Item, out UInt32 Len);
        static DeWeGetParamXML_strLENType _dewe_get_param_xml_str_len = null;

        public delegate Trion.TrionError DeWeOpenCANType(Int32 nBoardNo);
        static DeWeOpenCANType _dewe_open_can = null;
        public delegate Trion.TrionError DeWeCloseCANType(Int32 nBoardNo);
        static DeWeCloseCANType _dewe_close_can = null;
        public delegate Trion.TrionError DeWeStartCANType(Int32 nBoardNo, Int32 nChannelNo);
        static DeWeStartCANType _dewe_start_can = null;
        public delegate Trion.TrionError DeWeStopCANType(Int32 nBoardNo, Int32 nChannelNo);
        static DeWeStopCANType _dewe_stop_can = null;
        public delegate Trion.TrionError DeWeReadCANType(Int32 nBoardNo, Trion.BOARD_CAN_FRAME[] pCanFrames, Int32 nMaxFrameCount, out Int32 nRealFrameCount);
        static DeWeReadCANType _dewe_read_can = null;

        //public delegate Trion.TrionError DeWeReadCANRawFrame(Int32 nBoardNo, PBOARD_CAN_RAW_FRAME* pCanFrames, out Int32 nRealFrameCount);
        //public delegate Trion.TrionError DeWeWriteCAN(Int32 nBoardNo, PBOARD_CAN_FRAME pCanFrames, Int32 nMaxFrameCount, out Int32 nRealFrameCount);
        public delegate Trion.TrionError DeWeErrorCntCANType(Int32 nBoardNo, Int32 nChannelNo, out Int32 nErrorCount);
        static DeWeErrorCntCANType _dewe_error_cnt_can = null;

        public delegate Trion.TrionError DeWeOpenDmaUartType(Int32 nBoardNo);
        //static DeWeOpenDmaUartType _dewe_open_dma_uart = null;
        public delegate Trion.TrionError DeWeCloseDmaUartType(Int32 nBoardNo);
        //static DeWeCloseDmaUartType _dewe_close_dma_uart = null;
        public delegate Trion.TrionError DeWeStartDmaUartType(Int32 nBoardNo, Int32 nChannelNo);
        //static DeWeStartDmaUartType _dewe_start_dma_uart = null;
        public delegate Trion.TrionError DeWeStopDmaUartType(Int32 nBoardNo, Int32 nChannelNo);
        //static DeWeStopDmaUartType _dewe_stop_dma_uart = null;
        //public delegate Trion.TrionError DeWeReadDmaUart(Int32 nBoardNo, PBOARD_UART_FRAME pUartFrames, Int32 nMaxFrameCount, Int32* nRealFrameCount);
        //public delegate Trion.TrionError DeWeReadDmaUartRawFrame(Int32 nBoardNo, PBOARD_UART_RAW_FRAME* pUartFrames, Int32* nRealFrameCount);
        public delegate Trion.TrionError DeWeFreeDmaUartRawFrameType(Int32 nBoardNo, Int32 framestofree);
        //static DeWeFreeDmaUartRawFrameType _dewe_free_dma_uart_raw_frame = null;
        //public delegate Trion.TrionError DeWeWriteDmaUart(Int32 nBoardNo, PBOARD_UART_FRAME pUartFrames, Int32 nFrameCount, Int32* nRealFrameCount);


        public delegate string DeWeErrorConstantToStringType(Trion.TrionError nErrorCode);
        static DeWeErrorConstantToStringType _dewe_error_constant_to_string = null;
    }

}

namespace Trion_x86
{

    // TRION 32bit
    public class API
    {
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverDeInit();

        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int32 pVal);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int32 nVal);

        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int64 pVal);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int64 nVal);

        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);


        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeOpenCAN(Int32 nBoardNo);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeCloseCAN(Int32 nBoardNo);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStartCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStopCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_api.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeReadCAN(Int32 nBoardNo,
            [Out] Trion.BOARD_CAN_FRAME[] pCanFrames,
            Int32 nMaxFrameCount,
            [Out] out Int32 nRealFrameCount);


        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_api.dll", CallingConvention=CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] Trion.TrionError error_code);

    }
}

namespace Trion_x64
{
    // TRION 64bit
    public class API
    {
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverDeInit();

        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int32 pVal);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int32 nVal);

        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int64 pVal);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int64 nVal);

        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);

        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);


        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeOpenCAN(Int32 nBoardNo);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeCloseCAN(Int32 nBoardNo);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStartCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStopCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_api_x64.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeReadCAN(Int32 nBoardNo,
            [Out] Trion.BOARD_CAN_FRAME[] pCanFrames,
            Int32 nMaxFrameCount,
            [Out] out Int32 nRealFrameCount);


        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_api_x64.dll", CallingConvention=CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] Trion.TrionError error_code);

    }
}


namespace TrionNET_x86
{
    // TRIONET 32bit
    public class API
    {
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverDeInit();

        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int32 pVal);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int32 nVal);

        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int64 pVal);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int64 nVal);

        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);

        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);


        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeOpenCAN(Int32 nBoardNo);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeCloseCAN(Int32 nBoardNo);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStartCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStopCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeReadCAN(Int32 nBoardNo,
            [Out] Trion.BOARD_CAN_FRAME[] pCanFrames,
            Int32 nMaxFrameCount,
            [Out] out Int32 nRealFrameCount);

        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_netapi.dll", CallingConvention=CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] Trion.TrionError error_code);

    }
}


namespace TrionNET_x64
{
    // TRIONET 64bit
    public class API
    {
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeDriverDeInit();

        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int32 pVal);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int32 nVal);

        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int64 pVal);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int64 nVal);

        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);

        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeOpenCAN(Int32 nBoardNo);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeCloseCAN(Int32 nBoardNo);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStartCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeStopCAN(Int32 nBoardNo, Int32 nChannelNo);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeReadCAN(Int32 nBoardNo,
            [Out] Trion.BOARD_CAN_FRAME[] pCanFrames,
            Int32 nMaxFrameCount,
            [Out] out Int32 nRealFrameCount);


        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_netapi_x64.dll", CallingConvention=CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] Trion.TrionError error_code);

    }
}
