using System;
using System.Threading;
using trion_api = Trion;

namespace Examples
{
    class ADCDelayExample
    {
        private const int SAMPLE_RATE = 50000; // Sample rate in Hz
        private const int BLOCK_SIZE = 200; // Number of samples per block
        private const int BLOCK_COUNT = 50; // Number of blocks in the circular buffer
        private const int NUM_OF_BOARDS = 2; // Number of boards to use
        private const int NUM_OF_CHANNELS = 3; // Number of channels to use per board
        private const int CHANNEL_SIZE = 1024 * 32; // Size of one channel buffer in samples
        private const int BUFFER_OFFSET_BOARD_1 = CHANNEL_SIZE * NUM_OF_CHANNELS; // Offset for the second board in the circular buffer
        private const int memsize = CHANNEL_SIZE * NUM_OF_CHANNELS * sizeof(Int32);
        private static readonly int[] channel_buffer = new int[memsize];
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
            // otherwise, default to the first board (ID 0 and 1)
            int board_id1 = 0;
            int board_id2 = 1;
            if (args.Length >= 2)
            {
                board_id1 = Convert.ToInt32(args[0]);
                board_id2 = Convert.ToInt32(args[1]);
                if ((board_id2 >= Math.Abs(board_count)) || (board_id1 < 0) || (board_id2 < 0) || (board_id1 >= Math.Abs(board_count)))
                {
                    Console.WriteLine($"Invalid board ID: {board_id1} or {board_id2}");
                    Console.WriteLine($"Board count: {board_count}");
                    Console.WriteLine("Please provide a valid board ID as an argument.");
                    return 1;
                }
            }

            // Open & Reset the first board
            error_code = trion_api.API.DeWeSetParam_i32(board_id1, Trion.TrionCommand.OPEN_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to open board: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_id1, Trion.TrionCommand.RESET_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to reset board: {error_code}"); return 1; }

            // Open & Reset the second board
            error_code = trion_api.API.DeWeSetParam_i32(board_id2, Trion.TrionCommand.OPEN_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to open board: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_id2, Trion.TrionCommand.RESET_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to reset board: {error_code}"); return 1; }

            // Set configuration to use one board in standalone operation
            string target_01 = $"BoardID{board_id1}/AcqProp"; // master
            string target_02 = $"BoardID{board_id2}/AcqProp"; // slave

            // set first board as master and second board as slave
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "OperationMode", "Master");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set OperationMode: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "ExtTrigger", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtTrigger: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "ExtClk", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtClk: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "SampleRate", SAMPLE_RATE.ToString());
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set SampleRate: {error_code}"); return 1; }
            error_code = ReadSR(target_01, out global::System.String sample_rate);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error reading sample rate: {error_code}"); return 1; }

            // set second board as slave
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "OperationMode", "Slave");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set OperationMode: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "ExtTrigger", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtTrigger: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "ExtClk", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set ExtClk: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "SampleRate", SAMPLE_RATE.ToString());
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set SampleRate: {error_code}"); return 1; }
            error_code = ReadSR(target_02, out global::System.String sample_rate2);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error reading sample rate: {error_code}"); return 1; }

            // disable all AI channels on the first board
            string target_ai_all = $"BoardID{board_id1}/AIAll";
            error_code = trion_api.API.DeWeSetParamStruct_str(target_ai_all, "Used", "False");
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to disable all AI channels on board {board_id1}: {error_code}");
                return 1;
            }

            // disable all AI channels on the second board
            target_ai_all = $"BoardID{board_id2}/AIAll";
            error_code = trion_api.API.DeWeSetParamStruct_str(target_ai_all, "Used", "False");
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to disable all AI channels on board {board_id2}: {error_code}");
                return 1;
            }

            // 3 analog channels will be enabled on the first and second board
            // Enable AI0, AI1, and AI2 on the first board (board_id1)
            for (int i = 0; i < 3; ++i)
            {
                string channel_target = $"BoardID{board_id1}/AI{i}";
                string channel_target_2 = $"BoardID{board_id2}/AI{i}";

                error_code = trion_api.API.DeWeSetParamStruct_str(channel_target, "Used", "True");
                if (error_code != Trion.TrionError.NONE)
                {
                    Console.WriteLine($"Failed to enable AI{i} on board {board_id1}: {error_code}");
                    return 1;
                }
                error_code = trion_api.API.DeWeSetParamStruct_str(channel_target_2, "Used", "True");
                if (error_code != Trion.TrionError.NONE)
                {
                    Console.WriteLine($"Failed to enable AI{i} on board {board_id2}: {error_code}");
                    return 1;
                }
            }


            // setup the acquisition buffer for both boards
            // The acquisition buffer is used to store the acquired samples.
            // the ADC delay and scan size are stored for later
            int[] board_ids = { board_id1, board_id2 };
            int[] adc_delay = new int[NUM_OF_BOARDS];
            int[] scan_size = new int[NUM_OF_BOARDS];

            for (int i = 0; i < NUM_OF_BOARDS; ++i)
            {
                int tmp_id = board_ids[i];

                // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
                // For the default samplerate 2000 samples per second, 200 is a buffer for
                // 0.1 seconds
                error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
                if (error_code != Trion.TrionError.NONE)
                {
                    Console.WriteLine($"Failed to set buffer block size for board {tmp_id}: {error_code}");
                    return 1;
                }

                // Set the circular buffer size to 50 blocks. So the circular buffer can store samples
                // for 5 seconds
                error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_BLOCK_COUNT, BLOCK_COUNT);
                if (error_code != Trion.TrionError.NONE)
                {
                    Console.WriteLine($"Failed to set buffer block count for board {tmp_id}: {error_code}");
                    return 1;
                }

                // Update the hardware with settings
                error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
                if (error_code != Trion.TrionError.NONE)
                {
                    Console.WriteLine($"Failed to update parameters for board {tmp_id}: {error_code}");
                    return 1;
                }

                // Get the ADC delay. The typical conversion time of the ADC.
                // The ADCDelay is the offset of analog samples to digital or counter samples.
                // It is measured in number of samples,
                error_code = trion_api.API.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BOARD_ADC_DELAY, out adc_delay[i]);
                if (error_code != Trion.TrionError.NONE)
                {
                    Console.WriteLine($"Failed to get ADC delay for board {tmp_id}: {error_code}");
                    return 1;
                }

                // Determine the size of a sample scan
                error_code = trion_api.API.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_ONE_SCAN_SIZE, out scan_size[i]);
                if (error_code != Trion.TrionError.NONE)
                {
                    Console.WriteLine($"Failed to get scan size for board {tmp_id}: {error_code}");
                    return 1;
                }
            }

            // start data acquisition start. slave first!!
            // see: /TRION-SDK/04_Synchronization/Synchronization.html
            error_code = trion_api.API.DeWeSetParam_i32(board_id2, Trion.TrionCommand.START_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition: {error_code}");

                // Stop data acquisition
                error_code = trion_api.API.DeWeSetParam_i32(board_id2, Trion.TrionCommand.STOP_ACQUISITION, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }

                // Close the board connection
                error_code = trion_api.API.DeWeSetParam_i32(board_id2, Trion.TrionCommand.CLOSE_BOARD, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }

                // Uninitialize
                error_code = trion_api.API.DeWeDriverDeInit();
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }

                return (int)error_code;
            }

            // start master
            error_code = trion_api.API.DeWeSetParam_i32(board_id1, Trion.TrionCommand.START_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition: {error_code}");

                // Stop data acquisition
                error_code = trion_api.API.DeWeSetParam_i32(board_id1, Trion.TrionCommand.STOP_ACQUISITION, 0);
                error_code = trion_api.API.DeWeSetParam_i32(board_id2, Trion.TrionCommand.STOP_ACQUISITION, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }

                // Close the board connection
                error_code = trion_api.API.DeWeSetParam_i32(board_id1, Trion.TrionCommand.CLOSE_BOARD, 0);
                error_code = trion_api.API.DeWeSetParam_i32(board_id2, Trion.TrionCommand.CLOSE_BOARD, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }

                // Uninitialize
                error_code = trion_api.API.DeWeDriverDeInit();
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }

                return (int)error_code;
            }

            int[] avail_samples = new int[board_ids.Length];
            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);
            Console.WriteLine($"Polling interval: {polling_interval_ms} ms");

            while (!Console.KeyAvailable)
            {
                for (int nbrd = 0; nbrd < NUM_OF_BOARDS; ++nbrd)
                {
                    int tmp_id = board_ids[nbrd];

                    // Get buffer details
                    error_code = trion_api.API.DeWeGetParam_i64(tmp_id, Trion.TrionCommand.BUFFER_0_END_POINTER, out Int64 buf_end_pos);
                    if (error_code != Trion.TrionError.NONE) continue;
                    error_code = trion_api.API.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE, out Int32 buf_size);
                    if (error_code != Trion.TrionError.NONE) continue;

                    bool use_wait = true;
                    // check if the user wants to use the wait command
                    if (!use_wait)
                    {
                        error_code = trion_api.API.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE, out avail_samples[nbrd]);
                        if (error_code != Trion.TrionError.NONE) continue;
                        if (error_code == Trion.TrionError.BUFFER_OVERWRITE)
                        {
                            Console.WriteLine("Buffer Overflow happened");
                            // TODO: deinit the driver and exit
                            error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }
                            error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }
                            error_code = trion_api.API.DeWeDriverDeInit();
                            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }
                            return 1;
                        }

                    }
                    else
                    {
                        // sleep to avoid busy waiting
                        // see: /TRION-SDK/03_DataAcquisition/DataAcquisition.html#block-and-block-size
                        System.Threading.Thread.Sleep(polling_interval_ms);
                        // Get the number of samples already stored in the circular buffer
                        error_code = trion_api.API.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE, out avail_samples[nbrd]);
                        if (error_code != Trion.TrionError.NONE) continue;
                        if (error_code == Trion.TrionError.BUFFER_OVERWRITE)
                        {
                            Console.WriteLine("Buffer Overflow happened");
                            // TODO: deinit the driver and exit
                            error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }
                            error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }
                            error_code = trion_api.API.DeWeDriverDeInit();
                            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }
                            return 1;
                        }

                    }

                    // Get available samples
                    error_code = trion_api.API.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE, out avail_samples[nbrd]);
                    if (error_code != Trion.TrionError.NONE) continue;
                    if (error_code == Trion.TrionError.BUFFER_OVERWRITE)
                    {
                        Console.WriteLine("Buffer Overflow happened");
                        // TODO: deinit the driver and exit
                        error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                        if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }
                        error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                        if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }
                        error_code = trion_api.API.DeWeDriverDeInit();
                        if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }
                        return 1;
                    }

                    // Adjust for ADC delay
                    avail_samples[nbrd] -= adc_delay[nbrd];
                    // skip if no samples are available
                    if (avail_samples[nbrd] <= 0) continue;

                    // Get current read pointer
                    error_code = trion_api.API.DeWeGetParam_i64(tmp_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS, out Int64 read_pos);
                    if (error_code != Trion.TrionError.NONE) continue;

                    // Adjust read pointer for ADC delay
                    long read_pos_AI = read_pos + adc_delay[nbrd] * scan_size[nbrd] * sizeof(Int32);

                    for (int i = 0; i < avail_samples[nbrd]; ++i)
                    {
                        // save the samples to the channel buffer
                        // each channel has a size of CHANNEL_SIZE samples
                        channel_buffer[i + (BUFFER_OFFSET_BOARD_1 * nbrd) + (CHANNEL_SIZE * 0)] = GetDataAtPos(read_pos_AI + (0 * sizeof(Int32)));
                        channel_buffer[i + (BUFFER_OFFSET_BOARD_1 * nbrd) + (CHANNEL_SIZE * 1)] = GetDataAtPos(read_pos_AI + (1 * sizeof(Int32)));
                        channel_buffer[i + (BUFFER_OFFSET_BOARD_1 * nbrd) + (CHANNEL_SIZE * 2)] = GetDataAtPos(read_pos_AI + (2 * sizeof(Int32)));
                        read_pos_AI += scan_size[nbrd] * sizeof(Int32);

                        // handle the circular buffer
                        if (read_pos_AI >= buf_end_pos)
                        {
                            read_pos_AI -= buf_size; // wrap around
                        }
                    }

                    // Free the buffer after reading
                    error_code = trion_api.API.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, avail_samples[nbrd]);
                }
                int min_samples = Math.Min(avail_samples[0], avail_samples[1]);
                for (int i = 0; i < min_samples; i += 100)
                {
                    Console.WriteLine(
                        $"B0_AI1: {channel_buffer[CHANNEL_SIZE * 0 + i],12}   " +
                        $"B0_AI2: {channel_buffer[CHANNEL_SIZE * 1 + i],12}   " +
                        $"B0_AI3: {channel_buffer[CHANNEL_SIZE * 2 + i],12}   " +
                        $"B1_AI1: {channel_buffer[CHANNEL_SIZE * 3 + i],12}   " +
                        $"B1_AI2: {channel_buffer[CHANNEL_SIZE * 4 + i],12}   " +
                        $"B1_AI3: {channel_buffer[CHANNEL_SIZE * 5 + i],12} "
                    );
                }
            }

            Console.WriteLine("End Example");
            return (int)error_code;
        }
    }
}