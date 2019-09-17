using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using trion_api = Trion;
using trion_api = Trion_x64;
//using trion_api = TrionNET;
//using trion_api = TrionNET_x64;

namespace Examples
{
    class OneAnalogChannel
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

        private static void configureNetwork()
        {
            //
            // Enter the local IP here (not the one of the TRIONET device!)
            String address = "169.254.220.141";
            String netmask = "255.255.0.0";

            // Configure the network interface to access TRIONET devices
            Trion.TrionError nErrorCode = trion_api.API.DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/LocalIP", address);
            //System.Console.WriteLine(nErrorCode.ToString());
            nErrorCode = trion_api.API.DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/NetMask", netmask);
            //System.Console.WriteLine(nErrorCode.ToString());
        }

        static int Main(string[] args)
        {
            Int32 nNoOfBoards;

            // get access to TRIONET devices
            configureNetwork();

            Trion.TrionError nErrorCode = trion_api.API.DeWeDriverInit(out nNoOfBoards);
            System.Console.WriteLine(nNoOfBoards.ToString() + " boards found. err = " + nErrorCode.ToString());

            // Check if TRION cards are in the system
            if (nNoOfBoards == 0)
            {
                Console.WriteLine("No Trion cards found. Aborting...\n");
                Console.WriteLine("Please configure a system using the DEWE2 Explorer.\n");

                return 1;
            }

            // Retrieve BoardId from the commandline
            int nBoardId = 0;
            if (args.Length > 0)
            {
                nBoardId = Convert.ToInt32(args[0]);
                if (nBoardId >= Math.Abs(nNoOfBoards))
                {
                    Console.WriteLine("Invalid BoardId: {0}\n", nBoardId);
                    return 1;
                }
            }

            // Open & Reset the board
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.OPEN_BOARD, 0);
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.RESET_BOARD, 0);

            // Set configuration to use one board in standalone operation
            string sTarget = "BoardID" + nBoardId + "/AcqProp";

            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "OperationMode", "Slave");
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "ExtTrigger", "False");
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "ExtClk", "False");
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "SampleRate", "2000");

            string sample_rate;
            nErrorCode = ReadSR(sTarget, out sample_rate);


            sTarget = "BoardID" + nBoardId + "/AI0";
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "Used", "True");
            if (nErrorCode > 0)
            {
                Console.WriteLine("Could not enable AI channel on board {0}: {1}\n", nBoardId, trion_api.API.DeWeErrorConstantToString(nErrorCode));
                return 1;
            }


            // Set 10V range
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "Range", "10 V");


            // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
            // For the default samplerate 2000 samples per second, 200 is a buffer for
            // 0.1 seconds
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_BLOCK_SIZE, 200);
            // Set the ring buffer size to 50 blocks. So ring buffer can store samples
            // for 5 seconds
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_BLOCK_COUNT, 50);

            // Update the hardware with settings
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);

            // Get the ADC delay. The typical conversion time of the ADC.
            // The ADCDelay is the offset of analog samples to digital or counter samples.
            // It is measured in number of samples,
            Int32 nADCDelay = 0;
            nErrorCode = trion_api.API.DeWeGetParam_i32(nBoardId, Trion.TrionCommand.BOARD_ADC_DELAY, out nADCDelay);

            // Data Acquisition - stopped with any key
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.START_ACQUISITION, 0);

            if (nErrorCode <= 0)
            {
                Int64 nBufStartPos;         // First position in the ring buffer
                Int64 nBufEndPos;          // Last position in the ring buffer
                Int32 nBufSize;              // Total buffer size
                float fVal;

                // Get detailed information about the ring buffer
                // to be able to handle the wrap around
                nErrorCode = trion_api.API.DeWeGetParam_i64(nBoardId, Trion.TrionCommand.BUFFER_START_POINTER, out nBufStartPos);
                nErrorCode = trion_api.API.DeWeGetParam_i64(nBoardId, Trion.TrionCommand.BUFFER_END_POINTER, out nBufEndPos);
                nErrorCode = trion_api.API.DeWeGetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_TOTAL_MEM_SIZE, out nBufSize);

                while (true)
                {
                    if (Console.KeyAvailable) // since .NET 2.0
                    {
                        break;
                    }
                    Int64 nReadPos;       // Pointer to the ring buffer read pointer
                    Int32 nAvailSamples;
                    Int32 i;
                    
                    // wait for 100ms
                    System.Threading.Thread.Sleep(10);

                    // Get the number of samples already stored in the ring buffer
                    nErrorCode = trion_api.API.DeWeGetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_AVAIL_NO_SAMPLE, out nAvailSamples);
                    if (Trion.TrionError.BUFFER_OVERWRITE == nErrorCode)
                    {
                        Console.WriteLine("Measurement Buffer Overflow happened - stopping measurement");
                        break;
                    }

                    // Available samples has to be recalculated according to the ADC delay
                    nAvailSamples = nAvailSamples - nADCDelay;

                    // skip if number of samples is smaller than the current ADC delay
                    if (nAvailSamples <= 0)
                    {
                        continue;
                    }

                    // Get the current read pointer
                    nErrorCode = trion_api.API.DeWeGetParam_i64(nBoardId, Trion.TrionCommand.BUFFER_ACT_SAMPLE_POS, out nReadPos);

                    // recalculate nReadPos to handle ADC delay
                    nReadPos = nReadPos + nADCDelay * sizeof(UInt32);

                    // Read the current samples from the ring buffer
                    for (i = 0; i < nAvailSamples; ++i)
                    {
                        // Handle the ring buffer wrap around
                        if (nReadPos >= nBufEndPos)
                        {
                            nReadPos -= nBufSize;
                        }

                        Int32 nRawData = GetDataAtPos(nReadPos);
                        

                        fVal = (float)((float)nRawData / 0x7FFFFF00 * 10.0);

                        // Print the sample value:
                        string out_str = String.Format("Raw {0,12} {1,17:#.000000000000}", nRawData, fVal);
                        Console.WriteLine(out_str);
                        
                        // Increment the read pointer
                        nReadPos += sizeof(UInt32);

                    }


                    // Free the ring buffer after read of all values
                    nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_FREE_NO_SAMPLE, nAvailSamples);
                    Console.WriteLine("CMD_BUFFER_FREE_NO_SAMPLE {0}  (err={1})", nAvailSamples, nErrorCode);
                }
            }

            // Stop data acquisition
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.STOP_ACQUISITION, 0);

            // Close the board connection
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.CLOSE_BOARD, 0);

            return (int)nErrorCode;
        }

        unsafe private static Trion.TrionError ReadSR(string sTarget, out string sample_rate)
        {
	        //fixed byte srate[27];
            byte[] srate = new byte[255];
            Trion.TrionError err = trion_api.API.DeWeGetParamStruct_str(sTarget, "SampleRate", srate, 255);
            sample_rate = ByteArrayToString(srate);
            return err;
        }

        unsafe private static Int32 GetDataAtPos( Int64 nReadPos)
        {
            // Get the sample value at the read pointer of the ring buffer
            // The sample value is 24Bit (little endian, encoded in 32bit). 
            return *((Int32*)nReadPos);
        }
    }
}