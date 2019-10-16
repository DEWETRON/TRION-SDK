// Copyright DEWETRON 2019

#include "dewepxi_apicxx.h"
#include "dewepxi_apicore.h"

int DeWeSetParamStruct_str_s(const std::string& target, const std::string& item, const std::string& value )
{
    return DeWeSetParamStruct_str(target.c_str(), item.c_str(), value.c_str());
}