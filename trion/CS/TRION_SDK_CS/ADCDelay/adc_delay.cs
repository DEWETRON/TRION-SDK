using System;
using System.Threading;
using trion_api = Trion;
using TrionApiUtils;

namespace Examples
{
    class ADCDelayExample
    {
        private const int DATAWIDTH = 24;
        private const int BLOCK_SIZE = 200;
        private const int BLOCK_COUNT = 50;
        private const int CHANNEL_BUFFER_SIZE = 1024;
        private const int SAMPLE_RATE = 2000;

        private static int GetPollingIntervalMs(int block_size, int sample_rate)
        {
            return (int)(block_size / (double)sample_rate * 1000);
        }

        private static Int32 GetDataAtPos(Int64 read_pos)
        {
            unsafe { return *(Int32*)read_pos; }
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
                    TrionApi.Uninitialize();
                    return 1;
                }
            }
            Console.WriteLine($"Using board ID: {board_id}");

            var error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.OPEN_BOARD, 0);
            Utils.CheckErrorCode(error, $"Failed to open board {board_id}");
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.RESET_BOARD, 0);
            Utils.CheckErrorCode(error, $"Failed to reset board {board_id}");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AI0", "Used", "True");
            Utils.CheckErrorCode(error, "Failed to enable AI0 channel");
            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/CNT0", "Used", "True");
            Utils.CheckErrorCode(error, "Failed to enable CNT0 channel");
            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/CNT0", "Source_A", "Acq_Clk");
            Utils.CheckErrorCode(error, "Failed to set CNT0 Source_A");

            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "OperationMode", "Master");
            Utils.CheckErrorCode(error, "Failed to set OperationMode");
            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtTrigger", "False");
            Utils.CheckErrorCode(error, "Failed to set ExtTrigger");
            error = TrionApi.DeWeSetParamStruct($"BoardID{board_id}/AcqProp", "ExtClk", "False");
            Utils.CheckErrorCode(error, "Failed to set ExtClk");

            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_SIZE, BLOCK_SIZE);
            Utils.CheckErrorCode(error, "Failed to set buffer block size");
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_BLOCK_COUNT, BLOCK_COUNT);
            Utils.CheckErrorCode(error, "Failed to set buffer block count");
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            Utils.CheckErrorCode(error, "Failed to update parameters");

            var (adcDelayError, adc_delay) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BOARD_ADC_DELAY);
            Utils.CheckErrorCode(adcDelayError, "Failed to get ADC delay");
            Console.WriteLine($"ADC delay for board {board_id}: {adc_delay} scans");

            var (scanSizeError, one_scan_size) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ONE_SCAN_SIZE);
            Utils.CheckErrorCode(scanSizeError, "Failed to get one scan size");
            Console.WriteLine($"One scan size for board {board_id}: {one_scan_size} bytes");

            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.START_ACQUISITION, 0);
            if (error != Trion.TrionError.NONE)
            {
                Console.WriteLine($"Failed to start acquisition for board {board_id}: {error}");
                TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
                TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
                TrionApi.Uninitialize();
                return (int)error;
            }

            var (bufferEndError, buffer_end_pointer) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_END_POINTER);
            Utils.CheckErrorCode(bufferEndError, "Failed to get buffer end pointer");
            var (bufferSizeError, buffer_total_size) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_TOTAL_MEM_SIZE);
            Utils.CheckErrorCode(bufferSizeError, "Failed to get buffer total size");

            int polling_interval_ms = GetPollingIntervalMs(BLOCK_SIZE, SAMPLE_RATE);
            long read_pos_AI = 0;
            long[] ai_channel_buffer = new long[CHANNEL_BUFFER_SIZE];
            long[] cnt_channel_buffer = new long[CHANNEL_BUFFER_SIZE];
            Console.WriteLine("Acquisition started ..\n\n");

            while (!Console.KeyAvailable)
            {
                Thread.Sleep(polling_interval_ms);
                var (availError, avail_samples) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_AVAIL_NO_SAMPLE);
                Utils.CheckErrorCode(availError, "Failed to get available samples");

                avail_samples -= adc_delay;
                if (avail_samples <= 0) continue;

                var (readPosError, read_pos) = TrionApi.DeWeGetParam_i64(board_id, Trion.TrionCommand.BUFFER_0_ACT_SAMPLE_POS);
                Utils.CheckErrorCode(readPosError, "Failed to get active sample position");

                read_pos_AI = read_pos + adc_delay * one_scan_size;
                for (long i = 0; i < Math.Min(avail_samples, CHANNEL_BUFFER_SIZE); ++i)
                {
                    if (read_pos_AI >= buffer_end_pointer)
                    {
                        read_pos_AI -= buffer_total_size;
                    }
                    ai_channel_buffer[i] = GetDataAtPos(read_pos_AI);
                    cnt_channel_buffer[i] = GetDataAtPos(read_pos_AI + sizeof(Int32));
                    Console.WriteLine($"AI0: {ai_channel_buffer[i],12} CNT0: {cnt_channel_buffer[i],12}");
                    read_pos_AI += one_scan_size;
                }
                error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.BUFFER_0_FREE_NO_SAMPLE, (int)avail_samples);
                Utils.CheckErrorCode(error, "Failed to free samples in buffer");
            }
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.STOP_ACQUISITION, 0);
            Utils.CheckErrorCode(error, "Failed to stop acquisition");
            error = TrionApi.DeWeSetParam_i32(board_id, Trion.TrionCommand.CLOSE_BOARD, 0);
            Utils.CheckErrorCode(error, "Failed to close board");
            TrionApi.Uninitialize();
            Console.WriteLine("End Example");
            return (int)error;
        }
    }
}