using System;
using System.Threading;
using trion_api = Trion;
using Trion;
using TrionApiUtils;

namespace Examples
{
    class ADCDelayExample
    {
        private const int SAMPLE_RATE = 2000; // Sample rate in Hz
        private const int BLOCK_SIZE = 200; // Number of samples per block
        private const int BLOCK_COUNT = 50; // Number of blocks in the circular buffer

        private static Int32 GetDataAtPos(Int64 read_pos)
        {
            // Get the sample value at the read pointer of the circular buffer
            // The sample value is 24Bit (little endian, encoded in 32bit).
            unsafe
            {
                return *(Int32*)read_pos;
            }
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
            Console.WriteLine($"Using board ID: {board_id}");

            // Open & Reset the board
            var error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.OPEN_BOARD, 0);

            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error, "Failed to reset board");

            // Set configuration to use one board in standalone operation

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "OperationMode", "Slave");
            Utils.CheckErrorCode(error, "Failed to set operation mode");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtTrigger", "False");
            Utils.CheckErrorCode(error, "Failed to set external trigger");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtClk", "False");
            Utils.CheckErrorCode(error, "Failed to set external clock");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "SampleRate", "2000");
            Utils.CheckErrorCode(error, "Failed to set sample rate");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Used", "False");
            Utils.CheckErrorCode(error, "Failed to disable all AI channels");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AI0", "Used", "True");
            Utils.CheckErrorCode(error, "Failed to enable AI0 channel");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AIAll", "Range", "10 V");
            Utils.CheckErrorCode(error, "Failed to set range for all AI channels");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AI0", "Range", "10 V");
            Utils.CheckErrorCode(error, "Failed to set range for AI0 channel");

            // Setup the acquisition buffer
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, BLOCK_SIZE);
            Utils.CheckErrorCode(error, "Failed to set buffer block size");
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, BLOCK_COUNT);
            Utils.CheckErrorCode(error, "Failed to set buffer block count");

            // Update the hardware with settings
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error, "Failed to update parameters");

            // Get the ADC delay
            var (adcDelayError, adc_delay) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BOARD_ADC_DELAY);
            Utils.CheckErrorCode(adcDelayError, "Failed to get ADC delay");

            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);
            if (error != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition: {error}");
                TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                TrionApi.Uninitialize();
                return (int)error;
            }

            float value;
            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);
            int available_samples = 0;

            // Get buffer info
            var (bufferStartError, buffer_start_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_START_POINTER);
            if (bufferStartError != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer start pointer: {bufferStartError}"); TrionApi.Uninitialize(); return 1; }
            var (bufferEndError, buffer_end_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER);
            if (bufferEndError != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer end pointer: {bufferEndError}"); TrionApi.Uninitialize(); return 1; }
            var (bufferSizeError, buffer_size) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE);
            if (bufferSizeError != Trion.TrionError.NONE) { Console.WriteLine($"Failed to get buffer total memory size: {bufferSizeError}"); TrionApi.Uninitialize(); return 1; }

            while (!Console.KeyAvailable)
            {
                bool use_wait = true;
                if (!use_wait)
                {
                    var (availError, availSamples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
                    Utils.CheckErrorCode(availError, "Failed to get available samples with wait");
                    available_samples = availSamples;
                }
                else
                {
                    System.Threading.Thread.Sleep(polling_interval_ms);
                    var (availError, availSamples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
                    Utils.CheckErrorCode(availError, "Failed to get available samples without wait");
                    available_samples = availSamples;
                }

                available_samples -= adc_delay; // Adjust for ADC delay

                if (available_samples <= 0)
                {
                    continue;
                }

                var (readPosError, read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
                Utils.CheckErrorCode(readPosError, "Failed to get read position");

                read_pos += adc_delay * sizeof(UInt32);

                for (int i = 0; i < available_samples; ++i)
                {
                    if (read_pos >= buffer_end_pos)
                    {
                        read_pos -= buffer_size;
                    }

                    Int32 raw_data = GetDataAtPos(read_pos);
                    value = (float)((float)raw_data / 0x7FFFFF00 * 10.0);

                    Console.WriteLine($"Raw {raw_data,12} {value,17:#.000000000000}");

                    read_pos += sizeof(UInt32);
                }

                error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);
                Utils.CheckErrorCode(error, "Failed to free samples in buffer");
            }

            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
            if (error != Trion.TrionError.NONE) { Console.WriteLine($"Failed to stop acquisition: {error}"); TrionApi.Uninitialize(); return 1; }

            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
            if (error != Trion.TrionError.NONE) { Console.WriteLine($"Failed to close board: {error}"); TrionApi.Uninitialize(); return 1; }

            TrionApi.Uninitialize();

            Console.WriteLine("End Example");
            return (int)error;
        }
    }
}