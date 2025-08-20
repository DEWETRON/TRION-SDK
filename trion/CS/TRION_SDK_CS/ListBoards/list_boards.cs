using System;
using Trion;

class ListBoardsOptions
{
    public int NumberOfBoards { get; set; } = 0;
    public bool UseAllCommands { get; set; }
    public bool UseBoardReset { get; set; }
    public bool UseThreads { get; set; }
    public bool UseMergeCache { get; set; }
    public bool ShowKey { get; set; }
    public int TestLoop { get; set; } = 1;
    public string ThreadPoolSize { get; set; }
}

class ListBoards
{
    static ListBoardsOptions ParseArguments(string[] args)
    {
        var opts = new ListBoardsOptions();
        for (int i = 0; i < args.Length; ++i)
        {
            switch (args[i])
            {
                case "--all": opts.UseAllCommands = true; break;
                case "--reset": opts.UseBoardReset = true; break;
                case "--thread": opts.UseThreads = true; break;
                case "--cache": opts.UseMergeCache = true; break;
                case "--key": opts.ShowKey = true; break;
                case "--pool":
                    if (i + 1 < args.Length)
                    {
                        opts.ThreadPoolSize = args[i + 1];
                    }
                    break;
                case "--runs":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int runs))
                    {
                        opts.TestLoop = runs;
                    }
                    break;
                case "--help":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
            }
        }
        return opts;
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: ListBoards [-all] [--reset] [--thread] [--pool NUM] [--cache]");
        Console.WriteLine("  --all         use _ALL commands for open,close,reset");
        Console.WriteLine("  --reset       do a board reset");
        Console.WriteLine("  --thread      use threads in the TRION API for performance improvements");
        Console.WriteLine("  --pool NUM    used number of threads in the TRION API for performance improvements");
        Console.WriteLine("  --runs NUM    number of re-runs of this example");
        Console.WriteLine("  --cache       use cached merge documents");
        Console.WriteLine("  --key         show board key info");
        Console.WriteLine("  --help        show this usage screen");
    }

    static void PrintOptionsUsed(ListBoardsOptions opts)
    {
        Console.WriteLine("Options used:");
        Console.WriteLine("  --all    => {0}", opts.UseAllCommands ? "yes" : "no");
        Console.WriteLine("  --reset  => {0}", opts.UseBoardReset ? "yes" : "no");
        Console.WriteLine("  --thread => {0}", opts.UseThreads ? "yes" : "no");
        Console.WriteLine("  --pool   => {0}", opts.ThreadPoolSize ?? "(null)");
        Console.WriteLine("  --cache  => {0}", opts.UseMergeCache ? "yes" : "no");
    }

    static void DoListBoards(ListBoardsOptions opts)
    {
        var current_number_of_boards = opts.NumberOfBoards;
        Trion.TrionError error;
        if (opts.NumberOfBoards == 0)
        {
            Console.WriteLine("No TRION board found");
            TrionApi.Uninitialize();
            Environment.Exit(0);
        }
        if (opts.NumberOfBoards > 0)
        {
            Console.WriteLine("Trion API is set to use real boards");
        }
        else
        {
            Console.WriteLine("Trion API is set to use simulated boards");
        }

        current_number_of_boards = Math.Abs(current_number_of_boards);

        // get the config path
        (error, string config_path) = TrionApi.DeWeGetParamStruct_String("System/config", "Path");
        if (error != TrionError.NONE || string.IsNullOrEmpty(config_path))
        {
            Console.WriteLine("Error retrieving config path: " + Trion.API.DeWeErrorConstantToString(error));
        }
        else
        {
            Console.WriteLine("Trion API config path       : " + config_path);
        }

        // get the log path
        (error, string log_path) = TrionApi.DeWeGetParamStruct_String("System/log", "Path");
        if (error != TrionError.NONE || string.IsNullOrEmpty(log_path))
        {
            Console.WriteLine("Error retrieving log path : " + Trion.API.DeWeErrorConstantToString(error));
        }
        else
        {
            Console.WriteLine("Trion API log path          : " + log_path);
        }

        // get the api backup path
        (error, string backup_path) = TrionApi.DeWeGetParamStruct_String("System/backup", "Path");
        if (error != TrionError.NONE || string.IsNullOrEmpty(backup_path))
        {
            Console.WriteLine("Error retrieving backup path: " + Trion.API.DeWeErrorConstantToString(error));
        }
        else
        {
            Console.WriteLine("Trion API backup path       : " + backup_path);
        }

        // get enclosure name
        (error, string enclosure_name) = TrionApi.DeWeGetParamXML_String("BoardID0/boardproperties/SystemInfo/EnclosureInfo", "Name");
        if (error != TrionError.NONE || string.IsNullOrEmpty(enclosure_name))
        {
            Console.WriteLine("Error retrieving enclosure name: " + Trion.API.DeWeErrorConstantToString(error));
        }
        else
        {
            Console.WriteLine("Enclosure name: " + enclosure_name);
        }

        Console.WriteLine("{0,-8} {1,-7} {2,-7} {3,-30} {4}", "EncID", "SlotNo", "BoardID", "Name", "SerialNo");
        Console.WriteLine("--------------------------------------------------------------------------");

        if (opts.UseAllCommands)
        {
            error = TrionApi.DeWeSetParam_i32(0, Trion.TrionCommand.OPEN_BOARD_ALL, 0);
            if (error != TrionError.NONE)
            {
                Console.WriteLine("Failed to open all boards: " + Trion.API.DeWeErrorConstantToString(error));
                return;
            }
        }
        else
        {
            for (int current_board_id = 0; current_board_id < current_number_of_boards; ++current_board_id)
            {
                error = TrionApi.DeWeSetParam_i32(current_board_id, Trion.TrionCommand.OPEN_BOARD, 0);
                if (error != TrionError.NONE)
                {
                    Console.WriteLine($"Failed to open board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                    continue;
                }
            }
        }

        // iterate all boards for reset
        if (opts.UseBoardReset)
        {
            if (opts.UseAllCommands)
            {
                error = TrionApi.DeWeSetParam_i32(0, Trion.TrionCommand.RESET_BOARD_ALL, 0);
                if (error != TrionError.NONE)
                {
                    Console.WriteLine("Failed to reset all boards: " + Trion.API.DeWeErrorConstantToString(error));
                    return;
                }
            }
            else
            {
                for (int current_board_id = 0; current_board_id < current_number_of_boards; ++current_board_id)
                {
                    error = TrionApi.DeWeSetParam_i32(current_board_id, Trion.TrionCommand.RESET_BOARD, 0);
                    if (error != TrionError.NONE)
                    {
                        Console.WriteLine($"Failed to reset board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                        continue;
                    }
                }
            }
        }

        // iterate all boards for info
        for (int current_board_id = 0; current_board_id < current_number_of_boards; ++current_board_id)
        {
            // get the enclosure ID:
            // Path to <SystemInfo> element in Board[0..127]Properties.xml
            (error, string enclosure_id) = TrionApi.DeWeGetParamXML_String($"BoardID{current_board_id}/boardproperties/SystemInfo/EnclosureInfo", "EnclosureID");
            if (error != TrionError.NONE || string.IsNullOrEmpty(enclosure_id))
            {
                Console.WriteLine($"Failed to get enclosure ID for board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                continue;
            }

            // get the slot number
            // Path to <SystemInfo> element in Board[0..127]Properties.xml
            //   -> BoardID%d/SystemInfo  (root element <Properies> is ignored by API)
            (error, string slot_no) = TrionApi.DeWeGetParamXML_String($"BoardID{current_board_id}/boardproperties/SystemInfo", "SlotNo");
            if (error != TrionError.NONE || string.IsNullOrEmpty(slot_no))
            {
                Console.WriteLine($"Failed to get slot number for board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                continue;
            }

            // slot id for internal controller device (DEWE3)
            (error, string segment_no) = TrionApi.DeWeGetParamXML_String($"BoardID{current_board_id}/boardproperties/SystemInfo", "InternalSegmentNo");
            if (error != TrionError.NONE || string.IsNullOrEmpty(segment_no))
            {
                Console.WriteLine($"Failed to get internal segment number for board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                continue;
            }

            // request the TRION board name
            (error, string board_name) = TrionApi.DeWeGetParamStruct_String($"BoardID{current_board_id}", "BoardName");
            if (error != TrionError.NONE || string.IsNullOrEmpty(board_name))
            {
                Console.WriteLine($"Failed to get board name for board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                continue;
            }

            // get the board serial number (for simulated boards this is always 12345678)
            (error, string serial_no) = TrionApi.DeWeGetParamXML_String($"BoardID{current_board_id}/boardproperties/BoardInfo", "SerialNumber");
            if (error != TrionError.NONE || string.IsNullOrEmpty(serial_no))
            {
                Console.WriteLine($"Failed to get serial number for board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                continue;
            }

            // print properties
            if (opts.ShowKey)
            {
                (error, string key) = TrionApi.DeWeGetParamStruct_String($"BoardID{current_board_id}", "key");
                if (error != TrionError.NONE || string.IsNullOrEmpty(key))
                {
                    Console.WriteLine($"Failed to get key for board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                    continue;
                }
                Console.WriteLine($"  {enclosure_id,-7} {slot_no,-2}[{segment_no}] {current_board_id,5}      {board_name,-30} {serial_no,-12} <{key}>");
            }
            else
            {
                Console.WriteLine($"  {enclosure_id,-7} {slot_no,-2}[{segment_no}] {current_board_id,5}      {board_name,-30} {serial_no,-12}");
            }
        }
    }

    static void Main(string[] args)
    {
        var opts = ParseArguments(args);
        for (int loop = 0; loop < opts.TestLoop; ++loop)
        {
            Console.WriteLine($"Test Loop: {loop}\n");
            opts.NumberOfBoards = TrionApi.Initialize();
            if (opts.NumberOfBoards == 0)
            {
                Console.WriteLine("No TRION board found");
                continue;
            }

            // enable threading if requested --------------------------------------------------------------------------
            if (opts.UseThreads)
            {
                var errorCode = TrionApi.DeWeSetParamStruct("driver/api/config/thread", "Enabled", "true");
                if (errorCode != TrionError.NONE)
                {
                    Console.WriteLine("Failed to enable threading: " + Trion.API.DeWeErrorConstantToString(errorCode));
                }
                if (!string.IsNullOrEmpty(opts.ThreadPoolSize))
                {
                    errorCode = TrionApi.DeWeSetParamStruct("driver/api/config/thread", "PoolSize", opts.ThreadPoolSize);
                    if (errorCode != TrionError.NONE)
                    {
                        Console.WriteLine("Failed to set thread pool size: " + Trion.API.DeWeErrorConstantToString(errorCode));
                    }
                }
            }

            // enable merge cache if requested ------------------------------------------------------------------------
            if (opts.UseMergeCache)
            {
                var errorCode = TrionApi.DeWeSetParamStruct("driver/api/config/xml", "AllowCachedMergeResult", "true");
                if (errorCode != TrionError.NONE)
                {
                    Console.WriteLine("Failed to enable merge cache: " + Trion.API.DeWeErrorConstantToString(errorCode));
                }
            }
            else
            {
                var errorCode = TrionApi.DeWeSetParamStruct("driver/api/config/xml", "AllowCachedMergeResult", "false");
                if (errorCode != TrionError.NONE)
                {
                    Console.WriteLine("Failed to disable merge cache: " + Trion.API.DeWeErrorConstantToString(errorCode));
                }
            }

            PrintOptionsUsed(opts);
            DoListBoards(opts);

            if (opts.UseAllCommands)
            {
                var error = TrionApi.DeWeSetParam_i32(0, Trion.TrionCommand.CLOSE_BOARD_ALL, 0);
                if (error != TrionError.NONE)
                {
                    Console.WriteLine("Failed to close all boards: " + Trion.API.DeWeErrorConstantToString(error));
                }
            }
            else
            {
                for (int current_board_id = 0; current_board_id < opts.NumberOfBoards; ++current_board_id)
                {
                    var error = TrionApi.DeWeSetParam_i32(current_board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                    if (error != TrionError.NONE)
                    {
                        Console.WriteLine($"Failed to close board {current_board_id}: " + Trion.API.DeWeErrorConstantToString(error));
                    }
                }
            }

            TrionApi.Uninitialize();
        }
    }
}