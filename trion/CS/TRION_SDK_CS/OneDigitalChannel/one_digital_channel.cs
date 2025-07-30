using System;
using System.Threading;
using trion_api = Trion;

namespace Examples
{
    class OneDigitalChannelExample
    {

        private const int SAMPLE_RATE = 2000; // Sample rate in Hz
        private const int BLOCK_SIZE = 1000; // Number of samples per block
        private const int BLOCK_COUNT = 100; // Number of blocks in the circular buffer
        private const int RESOLUTION_AI = 24; // Resolution of the analog input channels in bits
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

        unsafe private static Int32 GetDataAtPos(long read_pos)
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
            Trion.TrionError error_code = trion_api.API.DeWeDriverInit(out int board_count);
            board_count = Math.Abs(board_count); // Ensure board count is positive
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error initializing TRION driver: {error_code}"); return 1; }

            Console.WriteLine($"Number of TRION boards found: {board_count}");
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

            Console.WriteLine($"Using board ID: {board_id}");

            // Open & Reset the board
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.OPEN_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error opening board {board_id}: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.RESET_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error resetting board {board_id}: {error_code}"); return 1; }

            // Set the board to use the first digital channel (Discret0)
            string discret0_target = $"BoardID{board_id}/Discret0";
            error_code = trion_api.API.DeWeSetParamStruct_str(discret0_target, "Mode", "DIO");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting Discret0 Mode: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParamStruct_str(discret0_target, "Used", "True");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting Discret0 Used: {error_code}"); return 1; }

            // Set configuration to use one board in standalone operation
            string target = $"BoardID{board_id}/AcqProp";
            error_code = trion_api.API.DeWeSetParamStruct_str(target, "OperationMode", "Slave");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting OperationMode: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParamStruct_str(target, "ExtTrigger", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting ExtTrigger: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParamStruct_str(target, "ExtClk", "False");
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting ExtClk: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParamStruct_str(target, "SampleRate", SAMPLE_RATE.ToString());
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting SampleRate: {error_code}"); return 1; }

            // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
            // For the default samplerate 2000 samples per second, 200 is a buffer for
            // 0.1 seconds
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, BLOCK_SIZE);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting Buffer Block Size: {error_code}"); return 1; }

            // Set the circular buffer size to 50 blocks. So the circular buffer can store samples
            // for 5 seconds
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, BLOCK_COUNT);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error setting Buffer Block Count: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error updating parameters: {error_code}"); return 1; }

            // Allocate a buffer for the scan descriptor
            byte[] scan_descriptor = new byte[5000];

            // Retrieve the scan descriptor
            error_code = trion_api.API.DeWeGetParamStruct_str($"BoardID{board_id}", "ScanDescriptor_V3", scan_descriptor, (uint)scan_descriptor.Length);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error retrieving ScanDescriptor: {error_code}"); return 1; }

            // Convert the byte array to a string for further use
            string scan_descriptor_str = System.Text.Encoding.ASCII.GetString(scan_descriptor);
            Console.WriteLine($"Scan Descriptor: {scan_descriptor_str}");

            // Start the acquisition
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.START_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error starting acquisition: {error_code}"); return 1; }

            // get detailed description about the circular buffer
            // to be able to handle wrap around
            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_END_POINTER, out long buffer_end_pointer);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error getting Buffer End Pointer: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_TOTAL_MEM_SIZE, out long buffer_total_mem_size);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error getting Buffer Total Memory Size: {error_code}"); return 1; }

            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);
            while (!Console.KeyAvailable)
            {
                int raw_data = 0;
                uint bit = 0;
                bool use_wait = true;
                long available_samples = 0;

                if (use_wait)
                {
                    // get the number of samples already stored in the circular buffer
                    // using CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLES no sleep is necessary
                    error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE, out available_samples);
                    if (error_code != Trion.TrionError.NONE)
                    {
                        Console.WriteLine($"Error waiting for available samples: {error_code}");
                        // TODO: handle error appropriately
                        break;
                    }
                }
                else
                {
                    // if not using wait, we need to sleep for the polling interval
                    // to avoid busy waiting
                    System.Threading.Thread.Sleep(polling_interval_ms);
                    error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE, out available_samples);
                    if (error_code != Trion.TrionError.NONE)
                    {
                        Console.WriteLine($"Failed to get available samples: {error_code}");
                        // TODO: handle error appropriately
                        break;
                    }

                }

                // get the current read position of the circular buffer
                error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_ACT_SAMPLE_POS, out long read_pos);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error getting Buffer Active Sample Position: {error_code}"); return 1; }

                trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.DISCRET_STATE_SET, 0);
                //Console.WriteLine("Available samples: " + available_samples);

                for (long i = 0; i < available_samples; i++)
                {
                    raw_data = GetDataAtPos(read_pos);

                    // Extract Discret0 (bit 0)
                    bit = (uint)(raw_data & 0x1);

                    // Print every 100th sample on the same line
                    if (i % 100 == 0)
                    {
                        Console.Write($"\rDiscret0 = {bit}");
                    }

                    // Handle circular buffer wrap-around
                    if (read_pos >= buffer_end_pointer)
                    {
                        read_pos -= buffer_total_mem_size;
                    }

                    // Move to the next sample (assuming 4 bytes per sample)
                    read_pos += sizeof(uint);
                }
                // Free the samples after reading
                error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Error freeing samples: {error_code}"); break; }
            }
            // Stop data acquisition
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);

            // Close the board connection
            error_code = trion_api.API.DeWeSetParam_i64(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);

            // Uninitialize
            error_code = trion_api.API.DeWeDriverDeInit();

            return (int)error_code;
        }
    }
}