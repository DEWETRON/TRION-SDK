using System;
using Trion;
using TrionApiUtils;


public class TrionApi
{
    public static int Initialize()
    {
        // Configure the TRION API backend (choose TRION or TRIONET as needed)
        Trion.API.DeWeConfigure(Trion.API.Backend.TRION);

        // Call DeWeDriverInit on startup
        int nNoOfBoards;
        TrionError nErrorCode = Trion.API.DeWeDriverInit(out nNoOfBoards);

        // Optional: handle error or log result
        if (nErrorCode != TrionError.NONE)
        {
            // Handle error (e.g., log, show message, etc.)
            System.Diagnostics.Debug.WriteLine($"TRION API Init failed: {Trion.API.DeWeErrorConstantToString(nErrorCode)}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"TRION API Init succeeded. Number of boards: {nNoOfBoards}");
        }
        return nNoOfBoards;
    }

    public static void CloseBoards()
    {
        var error = TrionApi.DeWeSetParam_i32(0, Trion.TrionCommand.CLOSE_BOARD_ALL, 0);
        if (error != TrionError.NONE)
        {
            System.Diagnostics.Debug.WriteLine($"TRION API CloseBoards failed");
        }
    }

    public static void Uninitialize()
    {
        TrionError nErrorCode = Trion.API.DeWeDriverDeInit();
        if (nErrorCode != TrionError.NONE)
        {
            System.Diagnostics.Debug.WriteLine($"TRION API Uninit failed: {Trion.API.DeWeErrorConstantToString(nErrorCode)}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("TRION API Uninit succeeded.");
        }
    }


    public static (Trion.TrionError error, Int32 value) DeWeGetParam_i32(Int32 nBoardNo, Trion.TrionCommand nCommandId)
    {
        Int32 value = 0;
        Trion.TrionError error = Trion.API.DeWeGetParam_i32(nBoardNo, nCommandId, out value);
        return (error, value);
    }

    public static Trion.TrionError DeWeSetParam_i32(Int32 nBoardNo, Trion.TrionCommand nCommandId, Int32 value)
    {
        Trion.TrionError error = Trion.API.DeWeSetParam_i32(nBoardNo, nCommandId, value);
        return (error);
    }

    public static (Trion.TrionError error, Int64 value) DeWeGetParam_i64(Int32 nBoardNo, Trion.TrionCommand nCommandId)
    {
        Int64 value = 0;
        Trion.TrionError error = Trion.API.DeWeGetParam_i64(nBoardNo, nCommandId, out value);
        return (error, value);
    }

    public static Trion.TrionError DeWeSetParam_i64(Int32 nBoardNo, Trion.TrionCommand nCommandId, Int64 value)
    {
        Trion.TrionError error = Trion.API.DeWeSetParam_i64(nBoardNo, nCommandId, value);
        return (error);
    }


    public static (Trion.TrionError error, string value) DeWeGetParamStruct_String(string target, string item)
    {
        // First, get the required buffer size
        Trion.TrionError error = Trion.API.DeWeGetParamStruct_strLEN(target, item, out uint requiredLength);

        if (error != Trion.TrionError.NONE) // Assuming None represents success
        {
            return (error, string.Empty);
        }

        // Handle edge case where length is 0
        if (requiredLength == 0)
        {
            return (Trion.TrionError.NONE, string.Empty);
        }

        // Create buffer with exact required size
        byte[] buffer = new byte[requiredLength + 1];

        // Get the actual string data
        error = Trion.API.DeWeGetParamStruct_str(target, item, buffer, requiredLength + 1);

        if (error != Trion.TrionError.NONE)
        {
            return (error, string.Empty);
        }

        // Convert byte array to string, handling null termination
        int nullIndex = Array.IndexOf(buffer, (byte)0);
        if (nullIndex >= 0)
        {
            // Null terminator found, convert only up to that point
            return (error, System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex));
        }
        else
        {
            // No null terminator found, convert entire buffer
            return (error, System.Text.Encoding.UTF8.GetString(buffer));
        }
    }

    public static Trion.TrionError DeWeSetParamStruct(string target, string item, string var)
    {
        return Trion.API.DeWeSetParamStruct_str(target, item, var);
    }

    // Alternative version with manual buffer size (for backwards compatibility or special cases)
    public static (Trion.TrionError error, string value) DeWeGetParamStruct_String(string target, string item, uint bufferSize)
    {
        byte[] buffer = new byte[bufferSize];

        Trion.TrionError error = Trion.API.DeWeGetParamStruct_str(target, item, buffer, bufferSize);

        if (error != Trion.TrionError.NONE)
        {
            return (error, string.Empty);
        }

        int nullIndex = Array.IndexOf(buffer, (byte)0);
        if (nullIndex >= 0)
        {
            return (error, System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex));
        }
        else
        {
            return (error, System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0'));
        }
    }

    public static (Trion.TrionError error, string value) DeWeGetParamXML_String(string target, string item)
    {
        // First, get the required buffer size
        Trion.TrionError error = Trion.API.DeWeGetParamXML_strLEN(target, item, out uint requiredLength);

        if (error != Trion.TrionError.NONE) // Assuming None represents success
        {
            return (error, string.Empty);
        }

        // Handle edge case where length is 0
        if (requiredLength == 0)
        {
            return (Trion.TrionError.NONE, string.Empty);
        }

        // Create buffer with exact required size
        byte[] buffer = new byte[requiredLength + 1];

        // Get the actual string data
        error = Trion.API.DeWeGetParamXML_str(target, item, buffer, requiredLength + 1);

        if (error != Trion.TrionError.NONE)
        {
            return (error, string.Empty);
        }

        // Convert byte array to string, handling null termination
        int nullIndex = Array.IndexOf(buffer, (byte)0);
        if (nullIndex >= 0)
        {
            // Null terminator found, convert only up to that point
            return (error, System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex));
        }
        else
        {
            // No null terminator found, convert entire buffer
            return (error, System.Text.Encoding.UTF8.GetString(buffer));
        }
    }

    // Alternative version with manual buffer size (for backwards compatibility or special cases)
    public static (Trion.TrionError error, string value) DeWeGetParamXML_String(string target, string item, uint bufferSize)
    {
        byte[] buffer = new byte[bufferSize];

        Trion.TrionError error = Trion.API.DeWeGetParamXML_str(target, item, buffer, bufferSize);

        if (error != Trion.TrionError.NONE)
        {
            return (error, string.Empty);
        }

        int nullIndex = Array.IndexOf(buffer, (byte)0);
        if (nullIndex >= 0)
        {
            return (error, System.Text.Encoding.UTF8.GetString(buffer, 0, nullIndex));
        }
        else
        {
            return (error, System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0'));
        }
    }
}
