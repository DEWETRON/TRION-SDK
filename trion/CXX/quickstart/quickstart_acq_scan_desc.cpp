/**
 * TRION-SDK Quickstart example with acquisition loop and ScanDescriptor support.
 *
 * This code is licensed under MIT license (see LICENSE.txt for details)
 * Copyright (c) 2022 by DEWETRON GmbH
 */


#include "dewepxi_load.h"
#include "dewepxi_apicore.h"
#include "dewepxi_apiutil.h"
#include "pugixml.hpp"
#include <functional>
#include <iomanip>
#include <iostream>
#include <string>
#include <vector>


/**
 * Functor to be implemented by applications interested in sample data.
 */
using AddSampleFunctor = std::function<void(const char*, uint32_t, 
    const void*, uint32_t, uint32_t, uint32_t)>;


/**
 * Example Decoder using the ScanDescripter V2 xml for generic
 * sample decoding and processing.
 */
class ScanDescriptorDecoder
{
public:
    ScanDescriptorDecoder(const std::string& sd_xml, AddSampleFunctor f)
        : m_datasink(f)
    {    
        parseScanDescriptor(sd_xml);
    }

    /**
     * parseScanDescriptor - parses V2 xml and build internal processing vector
     */
    void parseScanDescriptor(const std::string& sd_xml)
    {
        pugi::xml_document sd_doc;
        if (pugi::status_ok == sd_doc.load_string(sd_xml.c_str()).status)
        {
            auto scan_description_node = 
                sd_doc.select_node("ScanDescriptor/*/ScanDescription").node();
            if (scan_description_node)
            {
                if (2 != scan_description_node.attribute("version").as_int())
                {
                    throw std::runtime_error("Unsupported version");
                }

                m_scan_size_bytes 
                    = scan_description_node.attribute("scan_size").as_int() / 8;
                // Can be safely ignored:
                // bit
                // byte_order
                // buffer_direction
                // buffer

                auto channel_nodes = scan_description_node.select_nodes("Channel");
                for (auto channelx : channel_nodes)
                {
                    auto channel = channelx.node();

                    auto sample = channel.child("Sample");

                    m_scan_desc_vec.push_back(
                        SDData{ std::string(channel.attribute("name").as_string()) 
                                , std::string(channel.attribute("type").as_string())
                                , channel.attribute("index").as_uint()
                                , sample.attribute("size").as_uint()
                                , sample.attribute("offset").as_uint()
                        });
                }

            }
            else
            {
                throw std::runtime_error("ScanDescriptor unexpected element");
            }
        }
        else
        {
            throw std::runtime_error("ScanDescriptor parse error");
        }
    }

    /**
     * Process sample blocks 
     * @param avail_samples a continuous block of samples
     * @return incremented read_pos
     */
    sint64 processSamples(sint64 read_pos, int avail_samples)
    {
        auto read_pos_ptr = reinterpret_cast<const char*>(read_pos);

        for (const auto& sd : m_scan_desc_vec)
        {
            uint32_t offset_bytes = sd.sample_offset / 8;
            auto read_pos_ptr_chn = read_pos_ptr + offset_bytes;
            uint32_t channel_bit_mask = (1 << sd.sample_size) - 1;

            m_datasink(sd.name.c_str(), sd.index, read_pos_ptr_chn, avail_samples, 
                channel_bit_mask, m_scan_size_bytes);
        }

        return read_pos + (avail_samples * m_scan_size_bytes);
    }

private:
    AddSampleFunctor m_datasink;
    uint32_t m_scan_size_bytes;

    struct SDData
    {
        std::string name;
        std::string channel_type;
        uint32_t index;
        uint32_t sample_size;
        uint32_t sample_offset;
    };
    std::vector<SDData> m_scan_desc_vec;
};


/**
 * Data Sink - App interface
 * @param channel is the name of the channel
 * @param first_sample pointer to the first sample
 * @param nr_samples number of samples available
 * @param bitmask bit mask to apply to get the valid sample value
 * @param stride is the offset to the next sample
 */
void addSample(const char* channel_name,
            uint32_t channel_index,
            const void* first_sample,
            uint32_t nr_samples,
            uint32_t bitmask,
            uint32_t stride)
{
    const char* data_ptr = static_cast<const char*>(first_sample);

    for (uint32_t i = 0; i < nr_samples; ++i)
    {
        uint32_t value = (*reinterpret_cast<const uint32_t*>(data_ptr)) & bitmask;
        std::cout << channel_name << ": " << std::hex << value  << std::endl;
        data_ptr += stride;
    }
}

/**
 * Print samples in channel per column
 * Uses Functor interface like "addSample"
 */
class FormattedOutput
{
public:
    FormattedOutput(int num_channels, int block_size)
        : m_num_channels(num_channels)
        , m_block_size(block_size)
    {
        m_output_buffer.resize(m_num_channels);
    }

    void operator()(const char* channel_name,
        uint32_t channel_index,
        const void* first_sample,
        uint32_t nr_samples,
        uint32_t bitmask,
        uint32_t stride)
    {
        const char* data_ptr = static_cast<const char*>(first_sample);

        m_output_buffer[channel_index].chn_name = channel_name;

        for (uint32_t i = 0; i < nr_samples; ++i)
        {
            uint32_t value = (*reinterpret_cast<const uint32_t*>(data_ptr)) & bitmask;
            m_output_buffer[channel_index].chn_sample_buffer.push_back(value);
            data_ptr += stride;
        }

        if (channel_index + 1 >= m_num_channels)
        {
            // Channel names
            for (uint32_t chn = 0; chn < m_num_channels; ++chn)
            {
                std::cout << std::setw(10) << m_output_buffer[chn].chn_name << ", ";
            }
            std::cout << std::endl;


            for (uint32_t i = 0; i < nr_samples; ++i)
            {
                for (uint32_t chn = 0; chn < m_num_channels; ++chn)
                {
                    auto value = m_output_buffer[chn].chn_sample_buffer[i];
                    std::cout << std::setw(10) << std::hex << value << ", ";
                }

                std::cout << std::endl;
            }

            m_output_buffer.clear();
            m_output_buffer.resize(m_num_channels);
        }
    }

    struct ChannelSamples
    {
        std::string chn_name;
        std::vector<int32_t> chn_sample_buffer;
    };

    std::vector<ChannelSamples> m_output_buffer;
    uint32_t m_num_channels;
    int m_block_size;
};



int main(int argc, char* argv[])
{
    int boards = 0;
    int avail_samples = 0;
    const int32_t buffer_block_size = 10;
    int64_t buf_end_pos = 0;        // Last position in the ring buffer
    int buff_size = 0;              // Total size of the ring buffer
    char scan_descriptor[8192] = { 0 };

    FormattedOutput output(4, buffer_block_size);

    // Basic SDK Initialization
    DeWePxiLoad();

    // boards is negative for simulation
    DeWeDriverInit(&boards);

    // Open boards
    // 0: chassis controller
    // 1: TRION3-1850-MULTI
    DeWeSetParam_i32(0, CMD_OPEN_BOARD, 0);
    DeWeSetParam_i32(0, CMD_RESET_BOARD, 0);
    DeWeSetParam_i32(1, CMD_OPEN_BOARD, 0);
    DeWeSetParam_i32(1, CMD_RESET_BOARD, 0);

    // Enable AI0 channel on board 1, disable all other
    DeWeSetParamStruct_str("BoardID1/AI0", "Used", "True");
    DeWeSetParamStruct_str("BoardID1/AI1", "Used", "True");
    DeWeSetParamStruct_str("BoardID1/AI2", "Used", "True");
    DeWeSetParamStruct_str("BoardID1/AI3", "Used", "True");

    // Configure acquisition properties
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_SIZE, buffer_block_size);
    DeWeSetParam_i32(1, CMD_BUFFER_BLOCK_COUNT, 200);
    DeWeSetParamStruct_str("BoardID1/AcqProp", "SampleRate", "100");

    // Apply settings
    DeWeSetParam_i32(1, CMD_UPDATE_PARAM_ALL, 0);

    // Get buffer configuration
    DeWeGetParam_i64(1, CMD_BUFFER_END_POINTER, &buf_end_pos);
    DeWeGetParam_i32(1, CMD_BUFFER_TOTAL_MEM_SIZE, &buff_size);

    // Get scan descriptor
    DeWeGetParamStruct_str("BoardId1", "ScanDescriptor_V2", scan_descriptor, sizeof(scan_descriptor));
    
#if 1
    // Connect to formatted output
    ScanDescriptorDecoder sd_decoder(scan_descriptor, output);
    
#else
    // or addSample function
    //ScanDescriptorDecoder sd_decoder(scan_descriptor, &addSample);
#endif

    // Start acquisition
    DeWeSetParam_i32(1, CMD_START_ACQUISITION, 0);

    // Measurement loop and sample processing
    int64_t read_pos = 0;
    int64_t* read_pos_ptr = 0;
    int sample_value = 0;

    // Break with CTRL+C only
    while (1)
    {
        // Get the number of samples available
        DeWeGetParam_i32(1, CMD_BUFFER_AVAIL_NO_SAMPLE, &avail_samples);
        if (avail_samples <= 0)
        {
            Sleep(100);
            continue;
        }

        // Get the current read pointer
        DeWeGetParam_i64(1, CMD_BUFFER_ACT_SAMPLE_POS, &read_pos);

        // Process samples in CMD_BUFFER_BLOCK_SIZE
        // to handle ring buffer wrap arounds only here:
        auto avail_samples_to_process = avail_samples;
        while (avail_samples_to_process > 0)
        {
            read_pos = sd_decoder.processSamples(read_pos, buffer_block_size);
            avail_samples_to_process -= buffer_block_size;

            // Handle the ring buffer wrap around
            if (read_pos >= buf_end_pos)
            {
                read_pos -= buff_size;
            }
        }

        DeWeSetParam_i32(1, CMD_BUFFER_FREE_NO_SAMPLE, avail_samples);
    }


    // Stop acquisition
    DeWeSetParam_i32(1, CMD_STOP_ACQUISITION, 0);

    // Free boards and unload SDK
    DeWeSetParam_i32(0, CMD_CLOSE_BOARD, 0);
    DeWeSetParam_i32(1, CMD_CLOSE_BOARD, 0);
    DeWeDriverDeInit();
    DeWePxiUnload();

    return 0;
}