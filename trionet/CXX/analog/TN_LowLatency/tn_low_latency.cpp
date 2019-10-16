// Copyright (c) Dewetron 2019

#include <string>
#include <sstream>
#include <iostream>
#include "dewepxi_apicxx.h"
#include "trion_sdk_util.h"


void configureNetwork();


int main(int argc, char* argv[])
{
    int nErrorCode  = 0;
    int nNoOfBoards = 0;
    int nBoardId = 0;

    // Load pxi_api.dll (the TRIONET Wrapper API)
    if ( 0 != LoadTrionApi() )
    {
        return 1;
    }

    // configure trionet client
    configureNetwork();

    // Initialize driver and retrieve the number of TRION boards
    // nNoOfBoards is a negative number if system is in DEMO mode!
    nErrorCode = DeWeDriverInit(&nNoOfBoards);
    CheckError(nErrorCode);
    nNoOfBoards=abs(nNoOfBoards);

    if (nNoOfBoards <= 0)
    {
        std::cerr << "No TRION boards detected" << std::endl;
    }

    // Open & Reset all boards
    nErrorCode = DeWeSetParam_i32( 0, CMD_OPEN_BOARD_ALL, 0 );
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParam_i32( 0, CMD_RESET_BOARD_ALL, 0 );
    CheckError(nErrorCode);

    // Configure Acquisition properties
    for (int nBoardId = 0; nBoardId < nNoOfBoards; ++nBoardId)
    {
        std::stringstream sTarget;
        sTarget << "BoardID" << nBoardId << "/AcqProp";
        std::string target = sTarget.str();
        nErrorCode = DeWeSetParamStruct_str_s( target, "OperationMode", "Slave");
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParamStruct_str_s( target, "ExtTrigger", "False");
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParamStruct_str_s( target, "ExtClk", "False");
        CheckError(nErrorCode);
        nErrorCode = DeWeSetParamStruct_str_s( target, "SampleRate", "10000");
    }

    // Enable analog channels on all boards
    for (int nBoardId = 0; nBoardId < nNoOfBoards; ++nBoardId)
    {
        std::stringstream sTarget;
        sTarget << "BoardID" << nBoardId << "/AIAll";
        std::string target = sTarget.str();
        nErrorCode = DeWeSetParamStruct_str_s( target, "Used", "True");
        CheckError(nErrorCode);
    }

    // Data Buffer
    for (int nBoardId = 0; nBoardId < nNoOfBoards; ++nBoardId)
    {
        // Setup the acquisition buffer: Size = BLOCK_SIZE * BLOCK_COUNT
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_SIZE, 200);
        CheckError(nErrorCode);
        // Set the ring buffer size to 50 blocks. So ring buffer can store samples
        // for 5 seconds
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_COUNT, 50);
        CheckError(nErrorCode);

        // Update the hardware with settings
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_UPDATE_PARAM_ALL, 0);
        CheckError(nErrorCode);
    }



    // Data Acquisition
    for (int nBoardId = 0; nBoardId < nNoOfBoards; ++nBoardId)
    {
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_START_ACQUISITION, 0);
        CheckError(nErrorCode);
    }

    if (nErrorCode <= 0)
    {
        while(true)
        {
            for (int nBoardId = 0; nBoardId < nNoOfBoards; ++nBoardId)
            {
                sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
                int nAvailSamples=0;

                nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
                CheckError(nErrorCode);
                if (ERR_BUFFER_OVERWRITE == nErrorCode)
                {
                    printf("Measurement Buffer Overflow happened - stopping measurement\n");
                    break;
                }

                if (nAvailSamples > 0)
                {
                    std::cout << nBoardId << ": samples = " << nAvailSamples << std::endl;

                    // Get the current read pointer
                    nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
                    CheckError(nErrorCode);

                    // Free the ring buffer after read of all values
                    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
                    CheckError(nErrorCode);
                }
            }
        }
    }

    // Stop data acquisition
    for (int nBoardId = 0; nBoardId < nNoOfBoards; ++nBoardId)
    {
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_STOP_ACQUISITION, 0);
        CheckError(nErrorCode);
    }


    // Close the board connection
    nErrorCode = DeWeSetParam_i32( 0, CMD_CLOSE_BOARD_ALL, 0);
    CheckError(nErrorCode);

    // Unload pxi_api.dll
    UnloadTrionApi("\nEnd Of Example\n");


    return nErrorCode;
}



void configureNetwork()
{
    int nErrorCode;

    const char* address = "169.254.109.242";
    const char* netmask = "255.255.0.0";
    printf("Example is listening for TRIONET devices on: %s (%s)\n", address, netmask);

    // TODO: Configure the network interface to access TRIONET devices
    nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/LocalIP", address);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str("trionetapi/config", "Network/IPV4/NetMask", netmask);
    CheckError(nErrorCode);
}
