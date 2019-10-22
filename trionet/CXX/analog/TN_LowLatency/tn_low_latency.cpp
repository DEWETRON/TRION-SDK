// Copyright (c) Dewetron 2019

#include "dewepxi_apicxx.h"
#include "trion_sdk_util.h"
#include "xpugixml.h"
#include <string>
#include <sstream>
#include <iostream>


void configureNetwork();


int main(int argc, char* argv[])
{
    int nErrorCode  = 0;
    int nNoOfBoards = 0;
    int nBoardId = 0;
    int nBlockSize = 20;
    int nBlockCount = 10000;

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

    // Configure Acquisition properties - standalone for every board
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
        nErrorCode = DeWeSetParamStruct_str_s( target, "SampleRate", "200000");
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
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_SIZE, nBlockSize);
        CheckError(nErrorCode);
        // Set the ring buffer size to 50 blocks. So ring buffer can store samples
        // for 5 seconds
        nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_BLOCK_COUNT, nBlockCount);
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
        bool stop_on_error = false;
        int nAvailSamplesMax = 0;
        int max_after = 40;

        while(!stop_on_error)
        {
            for (int nBoardId = 0; nBoardId < nNoOfBoards; ++nBoardId)
            {
                sint64 nReadPos=0;       // Pointer to the ring buffer read pointer
                int nAvailSamples=0;

                nErrorCode = DeWeGetParam_i32( nBoardId, CMD_BUFFER_AVAIL_NO_SAMPLE, &nAvailSamples );
                if (CheckError(nErrorCode))
                {
                    stop_on_error = true;
                    break;
                }

                if (nAvailSamples > nBlockSize)
                {
                    if (max_after <= 0)
                    {
                        nAvailSamplesMax = std::max(nAvailSamplesMax, nAvailSamples);
                    }
                    else 
                    {
                        --max_after;
                    }



                    //if (--stop_output_after > 0)
                    {
                        std::cout << nBoardId << ": samples = " << nAvailSamples << ", max = " << nAvailSamplesMax << std::endl;
                    }

                    // Get the current read pointer
                    nErrorCode = DeWeGetParam_i64( nBoardId, CMD_BUFFER_ACT_SAMPLE_POS, &nReadPos );
                    if (CheckError(nErrorCode)) 
                    {
                        stop_on_error = true;
                        break;
                    }

                    // Free the ring buffer after read of all values
                    nErrorCode = DeWeSetParam_i32( nBoardId, CMD_BUFFER_FREE_NO_SAMPLE, nAvailSamples );
                    if (CheckError(nErrorCode)) 
                    {
                        stop_on_error = true;
                        break;
                    }
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
    std::string address;
    std::string ll_address;
    std::string mask;
    std::string network_interfaces_xml;

    nErrorCode = DeWeGetParamStruct_str_s("trionetapi", "Network/Enumerate", network_interfaces_xml);
    CheckError(nErrorCode);

    //std::cerr << xpugi::xmlPrettyPrint(network_interfaces_xml) << std::endl;

    pugi::xml_document if_doc;
    if (pugi::status_ok == if_doc.load_string(network_interfaces_xml.c_str()).status)
    {
        auto interfaces = if_doc.document_element().select_nodes("Enumerate/Interface");
        for (auto itfx : interfaces)
        {
            auto itf = itfx.node();

            // ignore loopback, look for a LL address
            if (std::string("lo") != itf.attribute("name").value())
            {
                auto addresses = itf.select_nodes("Address");
                for (auto addrx : addresses)
                {
                    auto addr = addrx.node();
                    // IPv4 only, prefer link local
                    if (std::string("1") == addr.attribute("family").value())
                    {
                        address = addr.attribute("address").value();
                        mask = addr.attribute("mask").value();
                        if (address.find("169.254.") != std::string::npos)
                        {
                            // link local found
                            ll_address = address;
                        }
                    }
                }
            }
        }

        if (!ll_address.empty())
        {
            address = ll_address;
            mask = "255.255.0.0";
        }

    }
    else
    {
        // localhost fallback
        address = "127.0.0.1";
        mask = "255.255.255.0";
    }
    

    std::cout << "Example is listening for TRIONET devices on:\n" << address << "," << mask << std::endl;

    // Configure the network interface to access TRIONET devices
    nErrorCode = DeWeSetParamStruct_str_s("trionetapi/config", "Network/IPV4/LocalIP", address);
    CheckError(nErrorCode);
    nErrorCode = DeWeSetParamStruct_str_s("trionetapi/config", "Network/IPV4/NetMask", mask);
    CheckError(nErrorCode);
}
