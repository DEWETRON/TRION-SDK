using System;
using System.Threading;
using TrionApiUtils;
using trion_api = Trion;

namespace Examples
{
    class MultiBoardDigitalChannels
    {
        private const int SAMPLE_RATE = 2000;
        private const int BLOCK_SIZE = 1000;
        private const int BLOCK_COUNT = 100;
        private const int NUM_OF_BOARDS = 2;
        private const int NUM_OF_CHANNELS = 1; // e.g., Discret0
        private const int CHANNEL_SIZE = 1024 * 32; // 32k samples per buffer
        private const int BUFFER_OFFSET_BOARD_1 = CHANNEL_SIZE * NUM_OF_CHANNELS;
        private const int MEMSIZE = CHANNEL_SIZE * NUM_OF_CHANNELS * sizeof(Int32);
        private static readonly int[] CHANNEL_BUFFER = new int[MEMSIZE];


        private static string ByteArrayToString(byte[] arr)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetString(arr);
        }

        unsafe private static Trion.TrionError ReadSR(string target, out string sample_rate)
        {
            //fixed byte srate[27];
            byte[] srate = new byte[255];
            Trion.TrionError error = trion_api.API.DeWeGetParamStruct_str(target, "SampleRate", srate, 255);
            sample_rate = ByteArrayToString(srate);
            return error;
        }

        unsafe private static Int32 GetDataAtPos(Int64 read_pos)
        {
            // Get the sample value at the read pointer of the circular buffer
            // The sample value is 24Bit (little endian, encoded in 32bit).
            return *(Int32*)read_pos;
        }

        private static int GetPollingIntervalMs(int block_size, int sample_rate)
        {
            // Returns interval in milliseconds
            return (int)(block_size / (double)sample_rate * 1000);
        }

        static int Main(string[] args)
        {

            var number_of_boards = TrionApi.Initialize();

            if (number_of_boards == 0)
            {
                Console.WriteLine("No TRION board found");
                TrionApi.Uninitialize();
                return 1;
            }
            if (number_of_boards < 0)
            {
                Console.WriteLine($"Found {Math.Abs(number_of_boards)} Simulated TRION boards");
            }
            else
            {
                Console.WriteLine($"Found {number_of_boards} real TRION boards");
            }
            number_of_boards = Math.Abs(number_of_boards);

            // if a board ID is provided, validate it
            int board_id = 0;
            if (args.Length > 0)
            {
                board_id = Convert.ToInt32(args[0]);
                if ((board_id >= number_of_boards) || (board_id < 0))
                {
                    Console.WriteLine($"Invalid board ID: {board_id}");
                    Console.WriteLine($"Board count: {number_of_boards}");
                    Console.WriteLine("Please provide a valid board ID as an argument.");
                    TrionApi.Uninitialize();
                    return 1;
                }
            }
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Using board ID: {board_id}");
            Console.ResetColor();

            // if no boards are found, exit the program
            if (number_of_boards < 2)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Too few Trion cards found. Aborting...");
                Console.WriteLine("Please configure a system using the DEWE2 Explorer.");
                Console.ResetColor();

                return 1;
            }

            // if a board ID is provided, validate it
            // otherwise, default to the first board (ID 1 and 2)
            int[] board_ids = { 1, 2 };
            long[] scan_size = new long[NUM_OF_BOARDS];
            if (args.Length >= 2)
            {
                board_ids[0] = Convert.ToInt32(args[0]);
                board_ids[1] = Convert.ToInt32(args[1]);
                if ((board_ids[1] >= Math.Abs(number_of_boards)) || (board_ids[0] < 0) || (board_ids[1] < 0) || (board_ids[0] >= Math.Abs(number_of_boards)))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Invalid board ID: {board_ids[0]} or {board_ids[1]}");
                    Console.WriteLine($"Board count: {number_of_boards}");
                    Console.WriteLine("Please provide a valid board ID as an argument.");
                    Console.ResetColor();
                    return 1;
                }
            }

            // Open & Reset the first board
            var error_code = TrionApi.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.OPEN_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to open board {board_ids[0]}");
            error_code = TrionApi.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to reset board {board_ids[0]}");

            // Open & Reset the second board
            error_code = TrionApi.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.OPEN_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to open board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to reset board {board_ids[1]}");

            // Set configuration to use one board in standalone operation
            string target_01 = $"BoardID{board_ids[0]}/AcqProp"; // master
            string target_02 = $"BoardID{board_ids[1]}/AcqProp"; // slave

            // set first board as master and second board as slave
            error_code = TrionApi.DeWeSetParamStruct(target_01, "OperationMode", "Master");
            Utils.CheckErrorCode(error_code, $"Failed to set OperationMode for board {board_ids[0]}");
            error_code = TrionApi.DeWeSetParamStruct(target_01, "ExtTrigger", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtTrigger for board {board_ids[0]}");
            error_code = TrionApi.DeWeSetParamStruct(target_01, "ExtClk", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtClk for board {board_ids[0]}");
            error_code = TrionApi.DeWeSetParamStruct(target_01, "SampleRate", SAMPLE_RATE.ToString());
            Utils.CheckErrorCode(error_code, $"Failed to set SampleRate for board {board_ids[0]}");

            string discret0_target = $"BoardID{board_ids[0]}/Discret0";
            error_code = TrionApi.DeWeSetParamStruct(discret0_target, "Used", "True");
            Utils.CheckErrorCode(error_code, $"Failed to enable Discret0 on board {board_ids[0]}");
            error_code = TrionApi.DeWeSetParamStruct(discret0_target, "Mode", "DIO");
            Utils.CheckErrorCode(error_code, $"Failed to set Discret0 mode on board {board_ids[0]}");

            error_code = TrionApi.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
            Utils.CheckErrorCode(error_code, $"Failed to set BUFFER_0_BLOCK_SIZE for board {board_ids[0]}");
            error_code = TrionApi.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.BUFFER_0_BLOCK_COUNT, BLOCK_COUNT);
            Utils.CheckErrorCode(error_code, $"Failed to set BUFFER_0_BLOCK_COUNT for board {board_ids[0]}");
            error_code = TrionApi.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error_code, $"Failed to update parameters for board {board_ids[0]}");

            // set second board as slave
            error_code = TrionApi.DeWeSetParamStruct(target_02, "OperationMode", "Slave");
            Utils.CheckErrorCode(error_code, $"Failed to set OperationMode for board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParamStruct(target_02, "ExtTrigger", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtTrigger for board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParamStruct(target_02, "ExtClk", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtClk for board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParamStruct(target_02, "SampleRate", SAMPLE_RATE.ToString());
            Utils.CheckErrorCode(error_code, $"Failed to set SampleRate for board {board_ids[1]}");

            string discret1_target = $"BoardID{board_ids[1]}/Discret0";
            error_code = TrionApi.DeWeSetParamStruct(discret1_target, "Used", "True");
            Utils.CheckErrorCode(error_code, $"Failed to enable Discret0 on board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParamStruct(discret1_target, "Mode", "DIO");
            Utils.CheckErrorCode(error_code, $"Failed to set Discret0 mode on board {board_ids[1]}");

            error_code = TrionApi.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
            Utils.CheckErrorCode(error_code, $"Failed to set BUFFER_0_BLOCK_SIZE for board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.BUFFER_0_BLOCK_COUNT, BLOCK_COUNT);
            Utils.CheckErrorCode(error_code, $"Failed to set BUFFER_0_BLOCK_COUNT for board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error_code, $"Failed to update parameters for board {board_ids[1]}");
            (error_code, scan_size[0]) = TrionApi.DeWeGetParam_i64(board_ids[0], Trion.TrionCommand.BUFFER_0_ONE_SCAN_SIZE);
            (error_code, scan_size[1]) = TrionApi.DeWeGetParam_i64(board_ids[1], Trion.TrionCommand.BUFFER_0_ONE_SCAN_SIZE);

            string scan_descriptor = "";
            (error_code, scan_descriptor) = TrionApi.DeWeGetParamStruct_String(target_01, "ScanDescriptor");
            Utils.CheckErrorCode(error_code, $"Failed to get ScanDescriptor for board {board_ids[0]}");

            error_code = TrionApi.DeWeSetParam_i64(board_ids[1], Trion.TrionCommand.START_ACQUISITION, 0);
            Utils.CheckErrorCode(error_code, $"Failed to start acquisition on board {board_ids[1]}");
            error_code = TrionApi.DeWeSetParam_i64(board_ids[0], Trion.TrionCommand.START_ACQUISITION, 0);
            Utils.CheckErrorCode(error_code, $"Failed to start acquisition on board {board_ids[0]}");

            long[] avail_samples = new long[NUM_OF_BOARDS];
            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);

            while (!Console.KeyAvailable)
            {
                for (int board_number = 0; board_number < NUM_OF_BOARDS; ++board_number)
                {
                    int board_id = board_ids[board_number];

                    // get buffer details
                    (error_code, buffer_end_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER);
                    if (error_code != Trion.TrionError.NONE) continue;
                    (error_code, buffer_size) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE);
                    if (error_code != Trion.TrionError.NONE) continue;

                    bool use_wait = true; // use wait for available samples
                    if (use_wait)
                    {
                        // Wait for available samples
                        (error_code, avail_samples[board_number]) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
                        if (error_code != Trion.TrionError.NONE || avail_samples[board_number] <= 0) continue;
                    }
                    else
                    {
                        // Polling for available samples
                        System.Threading.Thread.Sleep(polling_interval_ms);
                        (error_code, avail_samples[board_number]) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
                        if (error_code != Trion.TrionError.NONE || avail_samples[board_number] <= 0) continue;
                    }

                    // Get the current read position of the circular buffer
                    (error_code, read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
                    if (error_code != Trion.TrionError.NONE) continue;

                    for (int i = 0; i < avail_samples[board_number]; i++)
                    {
                        CHANNEL_BUFFER[i + (BUFFER_OFFSET_BOARD_1 * board_number) + (CHANNEL_SIZE * 0)] = GetDataAtPos(read_pos + (0 * sizeof(uint))); // Discret0 board 1

                        read_pos += scan_size[board_number]; // Move to the next sample position

                        // Handle circular buffer wrap-around
                        if (read_pos >= buffer_end_pos)
                        {
                            read_pos -= buffer_size;
                        }
                    }
                    // Free the samples after reading
                    error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, avail_samples[board_number]);


                }
                long min_samples = Math.Min(avail_samples[0], avail_samples[1]);
                for (int i = 0; i < min_samples; i += 100)
                {
                    uint bit0 = (uint)(CHANNEL_BUFFER[CHANNEL_SIZE * 0 + i] & 0x1);
                    uint bit1 = (uint)(CHANNEL_BUFFER[CHANNEL_SIZE * 1 + i] & 0x1);
                    Console.Write($"\rB0_D0: {bit0,12}   B1_D0: {bit1,12}");
                }
            }


            // Stop and close boards
            for (int b = 0; b < NUM_OF_BOARDS; ++b)
            {
                trion_api.API.DeWeSetParam_i64(board_ids[b], Trion.TrionCommand.STOP_ACQUISITION, 0);
                trion_api.API.DeWeSetParam_i64(board_ids[b], Trion.TrionCommand.CLOSE_BOARD, 0);
            }
            trion_api.API.DeWeDriverDeInit();

            return 0;
        }
    }
}