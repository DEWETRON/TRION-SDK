using System;
using System.Threading;
using trion_api = Trion;

namespace Examples
{
    class ADCDelayExample
    {
        private const int DATAWIDTH = 24;
        private const int BLOCK_SIZE = 200;
        private const int BLOCK_COUNT = 50;
        private const int CHANNEL_BUFFER_SIZE = 1024;
        private const int SAMPLE_RATE = 2000; // 2000 samples per second


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
            Trion.TrionError error_code = trion_api.API.DeWeGetParamStruct_str(target, "SampleRate", srate, 255);
            sample_rate = ByteArrayToString(srate);
            return error_code;
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

        static unsafe int Main(string[] args)
        {
            // select the TRION backend
            trion_api.API.DeWeConfigure(trion_api.API.Backend.TRION);

            // initialize the driver
            // this will also detect the number of TRION boards connected
            Trion.TrionError error_code = trion_api.API.DeWeDriverInit(out Int32 board_count);

            // check for errors during initialization
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Driver initialization error: {error_code}"); return 1; }

            // if no boards are found, exit the program
            if (0 == board_count)
            {
                Console.WriteLine("No Trion cards found. Aborting...\n");
                Console.WriteLine("Please configure a system using the DEWE2 Explorer.\n");

                return 1;
            }


            // if a board ID is provided, validate it
            // otherwise, default to the first board (ID 0)
            int board_id = 0;
            if (args.Length > 0)
            {
                board_id = Convert.ToInt32(args[0]);
                if ((board_id >= Math.Abs(board_count)) || (board_id < 0))
                {
                    Console.WriteLine($"Invalid board ID: {board_id}");
                    Console.WriteLine($"Board count: {board_count}");
                    Console.WriteLine("Please provide a valid board ID as an argument.");
                    return 1;
                }
            }

            // open and reset the board
            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.OPEN_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to open board: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.RESET_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to reset board: {error_code}"); return 1; }

            // after reset, all channels are disabled
            // enable the first analog channel (AI1)
            string target = $"BoardID{board_id}/AI0";
            error_code = trion_api.API.DeWeSetParamStruct_str(target, "Used", "True");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to enable channel {target}: {error_code}"); return 1; }

            // additionally add a counter: CNT0
            // and set its input to ACQ-Clock
            target = $"BoardID{board_id}/CNT0";
            error_code = trion_api.API.DeWeSetParamStruct_str(target, "Used", "True");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to enable channel {target}: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target, "Source_A", "Acq_Clk");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set source for channel {target}: {error_code}"); return 1; }

            // Set configuration to use one board in standalone mode
            // it isn't really important if it is in Slave or Master mode
            // ExtTrigger means an external trigger is used, to note is that if you use a slave card the signal from the Master is still handles as
            // an external trigger, even tho they are in the same housing
            // ExtClk means an external clock is used
            target = $"BoardID{board_id}/AcqProp";
            error_code = trion_api.API.DeWeSetParamStruct_str(target, "OperationMode", "Master");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set operation mode for board {board_id}: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target, "ExtTrigger", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set external trigger for board {board_id}: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeSetParamStruct_str(target, "ExtClk", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set external clock for board {board_id}: {error_code}"); return 1; }

            // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
            // For the default samplerate 2000 samples per second, 200 is a buffer for
            // 0.1 seconds
            // one scan consists of one sample for each sampled channel
            // one block is a collection of BLOCK_SIZE scans
            // BLOCK_COUNT defines how many blocks the buffer can hold
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_BLOCK_SIZE, BLOCK_SIZE);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set block size for board {board_id}: {error_code}"); return 1; }
            // set the circular buffer size, if the size is 50 it can store 5 seconds of data
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_BLOCK_COUNT, BLOCK_COUNT);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set block count for board {board_id}: {error_code}"); return 1; }
            // after that the paramaters need to be updated
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to update parameters for board {board_id}: {error_code}"); return 1; }

            // get the ADC delay. The typical conversion time of the ADC
            // The ADC delay is the offset of analog samples to digital or counter samples.
            // it is measured in number of scans
            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BOARD_ADC_DELAY, out long adc_delay);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get ADC delay for board {board_id}: {error_code}"); return 1; }
            Console.WriteLine($"ADC delay for board {board_id}: {adc_delay} scans");

            // Determine the size of a sample scan
            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ONE_SCAN_SIZE, out long one_scan_size);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get one scan size for board {board_id}: {error_code}"); return 1; }
            Console.WriteLine($"One scan size for board {board_id}: {one_scan_size} bytes");

            // start the data acquisition, it is stopped with any key
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.START_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition for board {board_id}: {error_code}");
                // stop acquisition
                error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition for board {board_id}: {error_code}"); return 1; }
                // close the board
                error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board {board_id}: {error_code}"); return 1; }
                // unload the api
                error_code = trion_api.API.DeWeDriverDeInit();
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to unload driver: {error_code}"); return 1; }
                Console.WriteLine("Acquisition could not be started. Aborting...");
                return 1;
            }

            // get detailed information about the circular buffer
            // to be able to handle the wrap around
            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER, out long buffer_end_pointer);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer end pointer for board {board_id}: {error_code}"); return 1; }
            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE, out long buffer_total_size);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer total size for board {board_id}: {error_code}"); return 1; }

            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);
            long read_pos_AI = 0;
            long[] ai_channel_buffer = new long[CHANNEL_BUFFER_SIZE];
            long[] cnt_channel_buffer = new long[CHANNEL_BUFFER_SIZE];
            Console.WriteLine("Acquisition started ..\n\n");
            while (!Console.KeyAvailable)
            {
                // wait for the samples
                Thread.Sleep(polling_interval_ms);
                // get the number of available samples in the buffer
                error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE, out long avail_samples);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get available samples for board {board_id}: {error_code}"); return 1; }

                // available samples has to be recalculated according to the ADC delay
                avail_samples -= adc_delay; // adjust for ADC delay

                // skip if number of samples is smaller than the current ADC delay
                if (avail_samples <= 0)
                {
                    continue;
                }

                // get the current read pointer
                error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS, out long read_pos);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get active sample position for board {board_id}: {error_code}"); return 1; }

                // recalculate the position according to the ADC delay, remember the delay is measured in scans
                read_pos_AI = read_pos + adc_delay * one_scan_size;

                // read the current AI samples from the circular buffer
                for (long i = 0; i < avail_samples; ++i)
                {
                }
            }


            return 0;
        }
    }
}