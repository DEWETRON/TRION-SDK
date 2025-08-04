using System;
using Trion;

class ListBoardsOptions
{
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

    static void PrintBoardInfo(int nBoardID, bool showKey)
    {
        string boardTarget = $"BoardID{nBoardID}";
        string sysInfoTarget = $"{boardTarget}/boardproperties/SystemInfo";
        string boardInfoTarget = $"{boardTarget}/boardproperties/BoardInfo";
        string encInfoTarget = $"{boardTarget}/boardproperties/SystemInfo/EnclosureInfo";

        var (errSlot, slotNoStr) = TrionApi.DeWeGetParamXML_String(sysInfoTarget, "SlotNo");
        int.TryParse(slotNoStr, out global::System.Int32 slotNo);

        var (errEnc, encIdStr) = TrionApi.DeWeGetParamXML_String(encInfoTarget, "EnclosureID");
        int.TryParse(encIdStr, out global::System.Int32 encId);

        var (errName, boardName) = TrionApi.DeWeGetParamStruct_String(boardTarget, "BoardName");
        var (errSerial, serialNo) = TrionApi.DeWeGetParamXML_String(boardInfoTarget, "SerialNumber");

        if (showKey)
        {
            var (errKey, key) = TrionApi.DeWeGetParamStruct_String(boardTarget, "key");
            Console.WriteLine("{0,-8} {1,-7} {2,-7} {3,-30} {4} <{5}>", encId, slotNo, nBoardID, boardName, serialNo, key);
        }
        else
        {
            Console.WriteLine("{0,-8} {1,-7} {2,-7} {3,-30} {4}", encId, slotNo, nBoardID, boardName, serialNo);
        }
    }

    static void Main(string[] args)
    {
        var opts = ParseArguments(args);
        for (int loop = 0; loop < opts.TestLoop; ++loop)
        {
            Console.WriteLine($"Test Loop: {loop}\n");
            int nNoOfBoards = TrionApi.Initialize();
            if (nNoOfBoards == 0)
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

            if (nNoOfBoards < 0)
            {
                Console.WriteLine("Trion API is set to use simulated boards");
            }
            else
            {
                Console.WriteLine("Trion API is set to use real boards");
            }
            nNoOfBoards = Math.Abs(nNoOfBoards);
            Console.WriteLine("{0,-8} {1,-7} {2,-7} {3,-30} {4}", "EncID", "SlotNo", "BoardID", "Name", "SerialNo");
            Console.WriteLine("--------------------------------------------------------------------------");
            for (int nBoardID = 0; nBoardID < nNoOfBoards; ++nBoardID)
            {
                PrintBoardInfo(nBoardID, opts.ShowKey);
            }
            TrionApi.Uninitialize();
        }
    }
}