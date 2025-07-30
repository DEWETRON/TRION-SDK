using System;
using System.Threading;
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

        private static byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }

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

            // select the TRION backend
            // this is required to use the TRION API
            trion_api.API.DeWeConfigure(trion_api.API.Backend.TRION);

            // initialize the driver
            // this will also detect the number of TRION boards connected
            Trion.TrionError error_code = trion_api.API.DeWeDriverInit(out Int32 board_count);
            board_count = Math.Abs(board_count); // ensure board count is positive

            // check for errors during initialization
            if (error_code != Trion.TrionError.NONE)
            { Console.WriteLine($"Driver initialization error: {error_code}"); return 1; }


            // if no boards are found, exit the program
            if (board_count < 2)
            {
                Console.WriteLine("Too few Trion cards found. Aborting...\n");
                Console.WriteLine("Please configure a system using the DEWE2 Explorer.\n");

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
                if ((board_ids[1] >= Math.Abs(board_count)) || (board_ids[0] < 0) || (board_ids[1] < 0) || (board_ids[0] >= Math.Abs(board_count)))
                {
                    Console.WriteLine($"Invalid board ID: {board_ids[0]} or {board_ids[1]}");
                    Console.WriteLine($"Board count: {board_count}");
                    Console.WriteLine("Please provide a valid board ID as an argument.");
                    return 1;
                }
            }

            // Open & Reset the first board
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.OPEN_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to open board: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.RESET_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to reset board: {error_code}"); return 1; }

            // Open & Reset the second board
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.OPEN_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to open board: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.RESET_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to reset board: {error_code}"); return 1; }

            // Set configuration to use one board in standalone operation
            string target_01 = $"BoardID{board_ids[0]}/AcqProp"; // master
            string target_02 = $"BoardID{board_ids[1]}/AcqProp"; // slave

            // set first board as master and second board as slave
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "OperationMode", "Master");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set OperationMode: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "ExtTrigger", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtTrigger: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "ExtClk", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtClk: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "SampleRate", SAMPLE_RATE.ToString());
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set SampleRate: {error_code}"); return 1; }

            string discret0_target = $"BoardID{board_ids[0]}/Discret0";
            error_code = trion_api.API.DeWeSetParamStruct_str(discret0_target, "Used", "True");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to enable Discret0 on board {board_ids[0]}: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(discret0_target, "Mode", "DIO");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set Discret0 mode on board {board_ids[0]}: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set BUFFER_0_BLOCK_SIZE: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.BUFFER_0_BLOCK_COUNT, BLOCK_COUNT);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set BUFFER_0_BLOCK_COUNT: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to update parameters: {error_code}"); return 1; }

            // set second board as slave
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "OperationMode", "Slave");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set OperationMode: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "ExtTrigger", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtTrigger: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "ExtClk", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtClk: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "SampleRate", SAMPLE_RATE.ToString());
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set SampleRate: {error_code}"); return 1; }

            string discret1_target = $"BoardID{board_ids[1]}/Discret0";
            error_code = trion_api.API.DeWeSetParamStruct_str(discret1_target, "Used", "True");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to enable Discret0 on board {board_ids[1]}: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(discret1_target, "Mode", "DIO");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set Discret0 mode on board {board_ids[1]}: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set BUFFER_0_BLOCK_SIZE: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.BUFFER_0_BLOCK_COUNT, BLOCK_COUNT);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set BUFFER_0_BLOCK_COUNT: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to update parameters: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeGetParam_i64(board_ids[0], Trion.TrionCommand.BUFFER_0_ONE_SCAN_SIZE, out scan_size[0]);
            error_code = trion_api.API.DeWeGetParam_i64(board_ids[1], Trion.TrionCommand.BUFFER_0_ONE_SCAN_SIZE, out scan_size[1]);

            byte[] scan_descriptor = new byte[5000];

            error_code = trion_api.API.DeWeSetParam_i64(board_ids[1], Trion.TrionCommand.START_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition: {error_code}");

                // Stop data acquisition
                error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.STOP_ACQUISITION, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }

                // Close the board connection
                error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.CLOSE_BOARD, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }

                // Uninitialize
                error_code = trion_api.API.DeWeDriverDeInit();
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }

                return (int)error_code;
            }

            // start master
            error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.START_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition: {error_code}");

                // Stop data acquisition
                error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.STOP_ACQUISITION, 0);
                error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.STOP_ACQUISITION, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }

                // Close the board connection
                error_code = trion_api.API.DeWeSetParam_i32(board_ids[1], Trion.TrionCommand.CLOSE_BOARD, 0);
                error_code = trion_api.API.DeWeSetParam_i32(board_ids[0], Trion.TrionCommand.CLOSE_BOARD, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }

                // Uninitialize
                error_code = trion_api.API.DeWeDriverDeInit();
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }

                return (int)error_code;
            }

            long[] avail_samples = new long[NUM_OF_BOARDS];
            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);

            while (!Console.KeyAvailable)
            {
                for (int board_number = 0; board_number < NUM_OF_BOARDS; ++board_number)
                {
                    int board_id = board_ids[board_number];

                    // get buffer details
                    error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER, out long buffer_end_pos);
                    if (error_code != Trion.TrionError.NONE) continue;
                    error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE, out long buffer_size);
                    if (error_code != Trion.TrionError.NONE) continue;

                    bool use_wait = true; // use wait for available samples
                    if (use_wait)
                    {
                        // Wait for available samples
                        error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE, out avail_samples[board_number]);
                        if (error_code != Trion.TrionError.NONE || avail_samples[board_number] <= 0) continue;
                    }
                    else
                    {
                        // Polling for available samples
                        System.Threading.Thread.Sleep(polling_interval_ms);
                        error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE, out avail_samples[board_number]);
                        if (error_code != Trion.TrionError.NONE || avail_samples[board_number] <= 0) continue;
                    }

                    // Get the current read position of the circular buffer
                    error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS, out long read_pos);
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