using System;
using System.Threading;
using trion_api = Trion;
using TrionApiUtils;

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
            Trion.TrionError error = TrionApi.DeWeGetParamStruct_str(target, "SampleRate", srate, 255);
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

            int board_id1 = 0;
            int board_id2 = 1;
            if (args.Length >= 2)
            {
                board_id1 = Convert.ToInt32(args[0]);
                board_id2 = Convert.ToInt32(args[1]);
                if ((board_id2 >= board_count) || (board_id1 < 0) || (board_id2 < 0) || (board_id1 >= board_count))
                {
                    Console.WriteLine($"Invalid board ID: {board_id1} or {board_id2}");
                    Console.WriteLine($"Board count: {board_count}");
                    Console.WriteLine("Please provide a valid board ID as an argument.");
                    return 1;
                }
            }

            error_code = TrionApi.DeWeSetParam_i32(board_id1, Trion.TrionCommand.OPEN_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to open board {board_id1}");
            error_code = TrionApi.DeWeSetParam_i32(board_id1, Trion.TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to reset board {board_id1}");

            error_code = TrionApi.DeWeSetParam_i32(board_id2, Trion.TrionCommand.OPEN_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to open board {board_id2}");
            error_code = TrionApi.DeWeSetParam_i32(board_id2, Trion.TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to reset board {board_id2}");

            string target_01 = $"BoardID{board_id1}/AcqProp";
            string target_02 = $"BoardID{board_id2}/AcqProp";

            error_code = TrionApi.DeWeSetParamStruct_str(target_01, "OperationMode", "Master");
            Utils.CheckErrorCode(error_code, $"Failed to set OperationMode for board {board_id1}");
            error_code = TrionApi.DeWeSetParamStruct_str(target_01, "ExtTrigger", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtTrigger for board {board_id1}");
            error_code = TrionApi.DeWeSetParamStruct_str(target_01, "ExtClk", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtClk for board {board_id1}");
            error_code = TrionApi.DeWeSetParamStruct_str(target_01, "SampleRate", SAMPLE_RATE.ToString());
            Utils.CheckErrorCode(error_code, $"Failed to set SampleRate for board {board_id1}");

            error_code = TrionApi.DeWeSetParamStruct_str(target_02, "OperationMode", "Slave");
            Utils.CheckErrorCode(error_code, $"Failed to set OperationMode for board {board_id2}");
            error_code = TrionApi.DeWeSetParamStruct_str(target_02, "ExtTrigger", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtTrigger for board {board_id2}");
            error_code = TrionApi.DeWeSetParamStruct_str(target_02, "ExtClk", "False");
            Utils.CheckErrorCode(error_code, $"Failed to set ExtClk for board {board_id2}");
            error_code = TrionApi.DeWeSetParamStruct_str(target_02, "SampleRate", SAMPLE_RATE.ToString());
            Utils.CheckErrorCode(error_code, $"Failed to set SampleRate for board {board_id2}");

            string target_ai_all = $"BoardID{board_id1}/AIAll";
            error_code = TrionApi.DeWeSetParamStruct_str(target_ai_all, "Used", "False");
            Utils.CheckErrorCode(error_code, $"Failed to disable all AI channels on board {board_id1}");

            target_ai_all = $"BoardID{board_id2}/AIAll";
            error_code = TrionApi.DeWeSetParamStruct_str(target_ai_all, "Used", "False");
            Utils.CheckErrorCode(error_code, $"Failed to disable all AI channels on board {board_id2}");

            for (int i = 0; i < NUM_OF_CHANNELS; ++i)
            {
                string channel_target = $"BoardID{board_id1}/AI{i}";
                string channel_target_2 = $"BoardID{board_id2}/AI{i}";

                error_code = TrionApi.DeWeSetParamStruct_str(channel_target, "Used", "True");
                Utils.CheckErrorCode(error_code, $"Failed to enable AI{i} on board {board_id1}");
                error_code = TrionApi.DeWeSetParamStruct_str(channel_target_2, "Used", "True");
                Utils.CheckErrorCode(error_code, $"Failed to enable AI{i} on board {board_id2}");
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

                error_code = TrionApi.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
                Utils.CheckErrorCode(error_code, $"Failed to set buffer block size for board {tmp_id}");

                error_code = TrionApi.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_BLOCK_COUNT, BLOCK_COUNT);
                Utils.CheckErrorCode(error_code, $"Failed to set buffer block count for board {tmp_id}");

                error_code = TrionApi.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
                Utils.CheckErrorCode(error_code, $"Failed to update parameters for board {tmp_id}");

                error_code = TrionApi.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BOARD_ADC_DELAY, out adc_delay[i]);
                Utils.CheckErrorCode(error_code, $"Failed to get ADC delay for board {tmp_id}");

                error_code = TrionApi.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_ONE_SCAN_SIZE, out scan_size[i]);
                Utils.CheckErrorCode(error_code, $"Failed to get scan size for board {tmp_id}");
            }

            error_code = TrionApi.DeWeSetParam_i32(board_id2, Trion.TrionCommand.START_ACQUISITION, 0);
            Utils.CheckErrorCode(error_code, $"Failed to start acquisition on board {board_id2}");

            // start master
            error_code = TrionApi.DeWeSetParam_i32(board_id1, Trion.TrionCommand.START_ACQUISITION, 0);
            Utils.CheckErrorCode(error_code, "Failed to start acquisition on master board");

            int[] avail_samples = new int[board_ids.Length];
            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);
            Console.WriteLine($"Polling interval: {polling_interval_ms} ms");

            while (!Console.KeyAvailable)
            {
                for (int nbrd = 0; nbrd < NUM_OF_BOARDS; ++nbrd)
                {
                    int tmp_id = board_ids[nbrd];

                    // Get buffer details
                    error_code = TrionApi.DeWeGetParam_i64(tmp_id, Trion.TrionCommand.BUFFER_0_END_POINTER, out Int64 buf_end_pos);
                    Utils.CheckErrorCode(error_code, $"Failed to get buffer end pointer for board {tmp_id}");

                    error_code = TrionApi.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE, out Int32 buf_size);
                    Utils.CheckErrorCode(error_code, $"Failed to get buffer total memory size for board {tmp_id}");

                    bool use_wait = true;
                    // check if the user wants to use the wait command
                    if (!use_wait)
                    {
                        error_code = TrionApi.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE, out avail_samples[nbrd]);
                        Utils.CheckErrorCode(error_code, $"Failed to get available samples with wait for board {tmp_id}");
                    }
                    else
                    {
                        // sleep to avoid busy waiting
                        // see: /TRION-SDK/03_DataAcquisition/DataAcquisition.html#block-and-block-size
                        System.Threading.Thread.Sleep(polling_interval_ms);
                        // Get the number of samples already stored in the circular buffer
                        error_code = TrionApi.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE, out avail_samples[nbrd]);
                        Utils.CheckErrorCode(error_code, $"Failed to get available samples without wait for board {tmp_id}");
                    }

                    // Get available samples
                    error_code = TrionApi.DeWeGetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE, out avail_samples[nbrd]);
                    if (error_code != Trion.TrionError.NONE) continue;
                    if (error_code == Trion.TrionError.BUFFER_OVERWRITE)
                    {
                        Console.WriteLine("Buffer Overflow happened");
                        // TODO: deinit the driver and exit
                        error_code = TrionApi.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                        if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }
                        error_code = TrionApi.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                        if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }
                        error_code = TrionApi.DeWeDriverDeInit();
                        if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }
                        return 1;
                    }

                    // Adjust for ADC delay
                    avail_samples[nbrd] -= adc_delay[nbrd];
                    // skip if no samples are available
                    if (avail_samples[nbrd] <= 0) continue;

                    // Get current read pointer
                    error_code = TrionApi.DeWeGetParam_i64(tmp_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS, out Int64 read_pos);
                    Utils.CheckErrorCode(error_code, $"Failed to get read position for board {tmp_id}");

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

                    error_code = TrionApi.DeWeSetParam_i32(tmp_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, avail_samples[nbrd]);
                    Utils.CheckErrorCode(error_code, $"Failed to free buffer for board {tmp_id}");
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