using System;
using System.Threading;
using trion_api = Trion;
using Trion;
using TrionApiUtils;

namespace Examples
{
    class OneDigitalChannelExample
    {
        private const int SAMPLE_RATE = 2000;
        private const int BLOCK_SIZE = 1000;
        private const int BLOCK_COUNT = 100;
        private const int RESOLUTION_AI = 24;
        unsafe private static Int32 GetDataAtPos(long read_pos)
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

            int board_id = 0;
            if (args.Length > 0)
            {
                board_id = Convert.ToInt32(args[0]);
                if ((board_id >= number_of_boards) || (board_id < 0))
                {
                    Console.WriteLine($"Invalid board ID: {board_id}");
                    Console.WriteLine($"Board count: {number_of_boards}");
                    Console.WriteLine("Please provide a valid board ID as an argument.");
                    return 1;
                }
            }
            Console.WriteLine($"Using board ID: {board_id}");

            var error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.OPEN_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to open board {board_id}");
            error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error_code, $"Failed to reset board {board_id}");

            string discret0_target = $"BoardID{board_id}/Discret0";
            error_code = TrionApi.DeWeSetParamStruct(discret0_target, "Mode", "DIO");
            Utils.CheckErrorCode(error_code, "Failed to set Discret0 Mode");
            error_code = TrionApi.DeWeSetParamStruct(discret0_target, "Used", "True");
            Utils.CheckErrorCode(error_code, "Failed to set Discret0 Used");

            string target = $"BoardID{board_id}/AcqProp";
            error_code = TrionApi.DeWeSetParamStruct(target, "OperationMode", "Slave");
            Utils.CheckErrorCode(error_code, "Failed to set OperationMode");
            error_code = TrionApi.DeWeSetParamStruct(target, "ExtTrigger", "False");
            Utils.CheckErrorCode(error_code, "Failed to set ExtTrigger");
            error_code = TrionApi.DeWeSetParamStruct(target, "ExtClk", "False");
            Utils.CheckErrorCode(error_code, "Failed to set ExtClk");
            error_code = TrionApi.DeWeSetParamStruct(target, "SampleRate", SAMPLE_RATE.ToString());
            Utils.CheckErrorCode(error_code, "Failed to set SampleRate");

            error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, BLOCK_SIZE);
            Utils.CheckErrorCode(error_code, "Failed to set Buffer Block Size");
            error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, BLOCK_COUNT);
            Utils.CheckErrorCode(error_code, "Failed to set Buffer Block Count");
            error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error_code, "Failed to update parameters");

            (error_code, string scan_descriptor) = TrionApi.DeWeGetParamStruct_String($"BoardID{board_id}", "ScanDescriptor_V3");
            Utils.CheckErrorCode(error_code, "Failed to retrieve ScanDescriptor");
            Console.WriteLine($"Scan Descriptor: {scan_descriptor}");

            error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);
            Utils.CheckErrorCode(error_code, "Failed to start acquisition");

            (error_code, var buffer_end_pointer) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER);
            Utils.CheckErrorCode(error_code, "Failed to get Buffer End Pointer");
            (error_code, var buffer_total_mem_size) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE);
            Utils.CheckErrorCode(error_code, "Failed to get Buffer Total Memory Size");

            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);
            while (!Console.KeyAvailable)
            {
                int raw_data = 0;
                uint bit = 0;
                bool use_wait = true;
                int available_samples = 0;

                if (use_wait)
                {
                    (error_code, available_samples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_WAIT_AVAIL_NO_SAMPLE);
                    Utils.CheckErrorCode(error_code, "Failed to get available samples (wait)");
                }
                else
                {
                    Thread.Sleep(polling_interval_ms);
                    (error_code, available_samples) = TrionApi.DeWeGetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
                    Utils.CheckErrorCode(error_code, "Failed to get available samples (poll)");
                }

                (error_code, var read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
                Utils.CheckErrorCode(error_code, "Failed to get Buffer Active Sample Position");

                TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.DISCRET_STATE_SET, 0);

                for (int i = 0; i < available_samples; i++)
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

                error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, available_samples);
                Utils.CheckErrorCode(error_code, "Failed to free buffer");
            }

            error_code = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
            Utils.CheckErrorCode(error_code, "Failed to stop acquisition");
            TrionApi.CloseBoards();
            Utils.CheckErrorCode(error_code, "Failed to close board");
            TrionApi.Uninitialize();

            Console.WriteLine("End Example");
            return (int)error_code;
        }
    }
}