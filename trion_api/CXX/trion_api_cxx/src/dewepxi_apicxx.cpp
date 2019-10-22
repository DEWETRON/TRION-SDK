// Copyright DEWETRON 2019

#include "dewepxi_apicxx.h"
#include "dewepxi_apicore.h"
#include <inttypes.h>

int DeWeSetParamStruct_str_s(const std::string& target, const std::string& item, const std::string& value )
{
    return DeWeSetParamStruct_str(target.c_str(), item.c_str(), value.c_str());
}


int DeWeGetParamStruct_str_s(const std::string& target, const std::string& item, std::string& value)
{
    char buff[1024] = {0};  // 1 page
    auto err = DeWeGetParamStruct_str(target.c_str(), item.c_str(), buff, sizeof(buff));
    if (err == ERROR_BUFFER_TOO_SMALL)
    {
        uint32_t buff_size;
        err = DeWeGetParamStruct_strLEN(target.c_str(), item.c_str(), &buff_size);
        if (err == ERR_NONE)
        {
            auto heap_buff = new char(buff_size);
            err = DeWeGetParamStruct_str(target.c_str(), item.c_str(), heap_buff, buff_size);
            if (err == ERR_NONE)
            {
                value = std::string(heap_buff, buff_size);
            }
            delete [] heap_buff;
        }
    }
    else
    {
        value = std::string(buff);
    }

    return err;
}