using System;
using System.Threading;
using trion_api = Trion;

namespace Examples
{
    class ADCDelayExample
    {
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

        static int Main(string[] args)
        {
            // select the TRION backend
            // this is required to use the TRION API
            trion_api.API.DeWeConfigure(trion_api.API.Backend.TRION);

            // initialize the driver
            // this will also detect the number of TRION boards connected
            Trion.TrionError error_code = trion_api.API.DeWeDriverInit(out Int32 board_count);

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
                if ((board_id2 >= Math.Abs(board_count)) || (board_id2 = Math.Abs(board_count)) || (board_id < 0))
                {
                    Console.WriteLine($"Invalid board ID: {board_id} or {board_id2}");
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
            error_code = trion_api.API.DeWeSetParamStruct_str(target_01, "SampleRate", "2000");
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
            error_code = trion_api.API.DeWeSetParamStruct_str(target_02, "SampleRate", "2000");
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

            // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
            // For the default samplerate 2000 samples per second, 200 is a buffer for
            // 0.1 seconds
            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, 200);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set buffer block size: {error_code}"); return 1; }
            // Set the circular buffer size to 50 blocks. So circular buffer can store samples
            // for 5 seconds
            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, 50);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to set buffer block count: {error_code}"); return 1; }

            // Update the hardware with settings
            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to update parameters: {error_code}"); return 1; }

            // Get the ADC delay. The typical conversion time of the ADC.
            // The ADCDelay is the offset of analog samples to digital or counter samples.
            // It is measured in number of samples,
            error_code = trion_api.API.DeWeGetParam_i32(board_id, Trion.TrionCommand.BOARD_ADC_DELAY, out Int32 adc_delay);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get ADC delay: {error_code}"); return 1; }

            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition: {error_code}");

                // Stop data acquisition
                error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }

                // Close the board connection
                error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }

                // Uninitialize
                error_code = trion_api.API.DeWeDriverDeInit();
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }

                return (int)error_code;
            }

            float value;

            // Get detailed information about the circular buffer
            // to be able to handle the wrap around
            // First position in the circular buffer
            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_START_POINTER, out Int64 buffer_start_pos);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer start pointer: {error_code}"); return 1; }
            // Last position in the circular buffer
            error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_END_POINTER, out Int64 buffer_end_pos);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer end pointer: {error_code}"); return 1; }
            // total buffer size
            error_code = trion_api.API.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_TOTAL_MEM_SIZE, out Int32 buffer_size);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer total memory size: {error_code}"); return 1; }

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    break; // Exit loop if a key is pressed
                }

                System.Threading.Thread.Sleep(100); // Sleep for 100 ms to avoid busy waiting

                Int32 i = 0;

                // Get the number of samples already stored in the circular buffer
                error_code = trion_api.API.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_AVAIL_NO_SAMPLE, out Int32 available_samples);
                if (Trion.TrionError.BUFFER_OVERWRITE == error_code) { Console.WriteLine("Measurement Buffer Overflow happened - stopping measurement"); break; }
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get available samples: {error_code}"); break; }

                available_samples -= adc_delay; // Adjust for ADC delay

                if (available_samples <= 0)
                {
                    continue; // No samples available, continue to next iteration
                }


                error_code = trion_api.API.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_ACT_SAMPLE_POS, out Int64 read_pos);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get active sample position: {error_code}"); break; }

                // recalculate read_pos to handle ADC delay
                read_pos += adc_delay * sizeof(UInt32);

                // Read the current samples from the circular buffer
                for (i = 0; i < available_samples; ++i)
                {
                    // Handle the circular buffer wrap around
                    if (read_pos >= buffer_end_pos)
                    {
                        read_pos -= buffer_size;
                    }

                    Int32 raw_data = GetDataAtPos(read_pos);
                    value = (float)((float)raw_data / 0x7FFFFF00 * 10.0);

                    // Print the sample value:
                    string out_str = String.Format("Raw {0,12} {1,17:#.000000000000}", raw_data, value);
                    Console.WriteLine(out_str);

                    // Increment the read pointer
                    read_pos += sizeof(UInt32);

                }


                // Free the circular buffer after read of all values
                error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_FREE_NO_SAMPLE, available_samples);
                if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to free buffer: {error_code}"); break; }
                Console.WriteLine("CMD_BUFFER_FREE_NO_SAMPLE {0}  (err={1})", available_samples, error_code);
            }

             // Stop data acquisition
            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error_code}"); return 1; }

            // Close the board connection
            error_code = trion_api.API.DeWeSetParam_i32(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error_code}"); return 1; }

            // Uninitialize
            error_code = trion_api.API.DeWeDriverDeInit();
            if (error_code != Trion.TrionError.NONE) { Console.WriteLine($"Failed to deinitialize driver: {error_code}"); return 1; }

            Console.WriteLine("We good");
            return (int)error_code;
        }
    }
}