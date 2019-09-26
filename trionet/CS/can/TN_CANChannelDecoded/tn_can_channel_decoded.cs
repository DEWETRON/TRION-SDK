using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using trion_api = Trion;


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
            String address = "127.0.0.1";
            String netmask = "255.255.255.0";

            // Configure the network interface to access TRIONET devices
            Trion.TrionError nErrorCode = trion_api.API.DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/LocalIP", address);
            trion_api.API.CheckError(nErrorCode);
            nErrorCode = trion_api.API.DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/NetMask", netmask);
            trion_api.API.CheckError(nErrorCode);
        }

        static int Main(string[] args)
        {
            Int32 nNoOfBoards = 0;

            // select TRIONET backend
            trion_api.API.DeWeConfigure(trion_api.API.Backend.TRIONET);

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
            trion_api.API.CheckError(nErrorCode);
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.RESET_BOARD, 0);
            trion_api.API.CheckError(nErrorCode);

            // Set configuration to use one board in standalone operation
            string sTarget = "BoardID" + nBoardId + "/AcqProp";

            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "OperationMode", "Slave");
            trion_api.API.CheckError(nErrorCode);
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "ExtTrigger", "False");
            trion_api.API.CheckError(nErrorCode);
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "ExtClk", "False");
            trion_api.API.CheckError(nErrorCode);

            // configure the BoardCounter-channel
            // for HW - timestamping to work it is necessary to have
            // at least one synchronous channel active. All TRION
            // boardtypes support a channel called Board-Counter (BoardCNT)
            // this is a basic counter channel, that usually has no
            // possibility to feed an external signal, and is usually
            // used to route internal signals to its input
            sTarget = "BoardID" + nBoardId + "/BoardCNT0";
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "Used", "True");
            trion_api.API.CheckError(nErrorCode);

            // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
            // For the default samplerate 2000 samples per second, 200 is a buffer for
            // 0.1 seconds
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_BLOCK_SIZE, 200);
            trion_api.API.CheckError(nErrorCode);
            // Set the ring buffer size to 50 blocks. So ring buffer can store samples
            // for 5 seconds
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_BLOCK_COUNT, 50);
            trion_api.API.CheckError(nErrorCode);

            // configure the CAN-channel 0
            // only two properties that need to be changed for this example are
            // SyncCounter: set it to 10Mhz, so the CAN Data will have timestamps with
            // Used: enable the channel for usage
            sTarget = "BoardID" + nBoardId + "/CANAll";
            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "SyncCounter", "10 MHzCount");
            trion_api.API.CheckError(nErrorCode);

            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "Termination", "True");
            trion_api.API.CheckError(nErrorCode);

            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "ListenOnly", "False");
            trion_api.API.CheckError(nErrorCode);

            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "BaudRate", "500000");
            trion_api.API.CheckError(nErrorCode);

            nErrorCode = trion_api.API.DeWeSetParamStruct_str(sTarget, "Used", "True");
            trion_api.API.CheckError(nErrorCode);


            // Open the CAN - Interface to this Board
            nErrorCode = trion_api.API.DeWeOpenCAN(nBoardId);
            trion_api.API.CheckError(nErrorCode);

            
            // Configure the ASYNC-Polling Time to 100ms
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.ASYNC_POLLING_TIME, 100);
            trion_api.API.CheckError(nErrorCode);

            // Update the hardware with settings
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.UPDATE_PARAM_ALL, 0);
            trion_api.API.CheckError(nErrorCode);


            // Start CAN capture, before start sync-acquisition
            // the sync - acquisition will synchronize the async data
            nErrorCode = trion_api.API.DeWeStartCAN(nBoardId, -1);
            trion_api.API.CheckError(nErrorCode);


            // Data Acquisition - stopped with any key
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.START_ACQUISITION, 0);
            trion_api.API.CheckError(nErrorCode);


            if (nErrorCode <= 0)
            {
                int CANBUFFER = 1000;
                trion_api.BOARD_CAN_FRAME[] aDecodedFrame = new trion_api.BOARD_CAN_FRAME[CANBUFFER];

                while (true)
                {
                    if (Console.KeyAvailable) // since .NET 2.0
                    {
                        break;
                    }

                    Int32 nAvailSamples = 0;
                    Int32 nRealFrameCount = 0;

                    // wait for 100ms
                    System.Threading.Thread.Sleep(100);

                    // Get the number of samples already stored in the ring buffer
                    nErrorCode = trion_api.API.DeWeGetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_AVAIL_NO_SAMPLE, out nAvailSamples);
                    trion_api.API.CheckError(nErrorCode);

                    // Free the ring buffer after read of all values
                    nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.BUFFER_FREE_NO_SAMPLE, nAvailSamples);
                    trion_api.API.CheckError(nErrorCode);

                    // now obtain all CAN - frames that have been collected in this timespan
                    do
                    {
                        nRealFrameCount = 0;
                        aDecodedFrame[0].CanNo = 42;
                        nErrorCode = trion_api.API.DeWeReadCAN(nBoardId, ref aDecodedFrame, CANBUFFER, ref nRealFrameCount);
                        trion_api.API.CheckError(nErrorCode);

                        for (int i = 0; i < nRealFrameCount; ++i)
                        {
                            //Timestamp in 100ns re-formated to seconds
                            float timestamp = (float)(aDecodedFrame[i].SyncCounter / 10000000.0);

                            // note here: with a 10Mhz counter @ 32Bit width, the timestamp will wrap
                            // around after roughly 7 minutes. This Warp around has to be handled by the
                            // application on raw data
                            System.Console.WriteLine("[{0:f7}] MSGID: {1:X8}   Port: {2:D}   Errorcount: {3:D}   DataLen: {4:D}   Data: {5:X2} {6:X2} {7:X2} {8:X2}   {9:X2} {10:X2} {11:X2} {12:X2}\n",
                                    timestamp,
                                    aDecodedFrame[i].MessageId,
                                    aDecodedFrame[i].CanNo,
                                    aDecodedFrame[i].ErrorCounter,
                                    aDecodedFrame[i].DataLength,
                                    aDecodedFrame[i].CanData[0], aDecodedFrame[i].CanData[1], aDecodedFrame[i].CanData[2], aDecodedFrame[i].CanData[3],
                                    aDecodedFrame[i].CanData[4], aDecodedFrame[i].CanData[5], aDecodedFrame[i].CanData[6], aDecodedFrame[i].CanData[7]
                            );
                        }
                    }
                    while (nRealFrameCount > (CANBUFFER / 2));

                }
            }

            // Stop CAN capture
            nErrorCode = trion_api.API.DeWeStopCAN(nBoardId, -1);
            trion_api.API.CheckError(nErrorCode);

            // Stop data acquisition
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.STOP_ACQUISITION, 0);

            // Close the board connection
            nErrorCode = trion_api.API.DeWeSetParam_i32(nBoardId, Trion.TrionCommand.CLOSE_BOARD, 0);

            // Uninitialize
            nErrorCode = trion_api.API.DeWeDriverDeInit();

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