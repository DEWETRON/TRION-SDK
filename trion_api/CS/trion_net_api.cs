using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Trion
{
    // TRION 32bit
    public class API
    {
        [DllImport("dwpxi_api.dll")] 
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);

        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] TrionCommand nCommandId, out Int32 pVal); 
        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] TrionCommand nCommandId, Int32 nVal); 

        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] TrionCommand nCommandId, out Int64 pVal); 
        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] TrionCommand nCommandId, Int64 nVal); 

        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);

        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api.dll")]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_api.dll")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] TrionError error_code);

    }
}

namespace Trion_x64
{
    // TRION 64bit
    public class API
    {
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);

        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int32 pVal);
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int32 nVal);

        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int64 pVal);
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int64 nVal);

        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);

        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_api_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_api_x64.dll")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] Trion.TrionError error_code);

    }
}


namespace TrionNET
{
    // TRIONET 32bit
    public class API
    {
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);

        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int32 pVal);
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int32 nVal);

        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int64 pVal);
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int64 nVal);

        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);

        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi.dll")]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_netapi.dll")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] Trion.TrionError error_code);

    }
}


namespace TrionNET_x64
{
    // TRIONET 64bit
    public class API
    {
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeDriverInit(out Int32 nNumOfBoard);

        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeGetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int32 pVal);
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int32 nVal);

        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeGetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, out Int64 pVal);
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, [MarshalAs(UnmanagedType.I4)] Trion.TrionCommand nCommandId, Int64 nVal);

        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeSetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);

        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamStruct_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeSetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Command, [MarshalAs(UnmanagedType.LPStr)] string Var);
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_str([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, [MarshalAs(UnmanagedType.LPArray)]  byte[] Var, UInt32 num);
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamXML_strLEN([MarshalAs(UnmanagedType.LPStr)] string Target, [MarshalAs(UnmanagedType.LPStr)] string Item, out UInt32 Len);

        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeFreeFramesCAN(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeFreeDmaUartRawFrame(Int32 nBoardNo, Int32 num);
        [DllImport("dwpxi_netapi_x64.dll")]
        public static extern Trion.TrionError DeWeGetParamStructEx_str([MarshalAs(UnmanagedType.LPStr)] string target, [MarshalAs(UnmanagedType.LPStr)] string command, [MarshalAs(UnmanagedType.LPStr)] string arg, [MarshalAs(UnmanagedType.LPArray)]  byte[] val, UInt32 num);

        [DllImport("dwpxi_netapi_x64.dll")]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string DeWeErrorConstantToString([MarshalAs(UnmanagedType.I4)] Trion.TrionError error_code);

    }
}
