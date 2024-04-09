/*
 * Copyright (c) 2013 DEWETRON
 * License: MIT
 * 
 * File containing all externally used error-codes
 */

#ifndef __DEWEPXI_ERR_H
#define __DEWEPXI_ERR_H


//---------------------------------------------------------------------------
#define DEWEPXI_USER_DEFINED_ERROR_BASE   1
#define DEWEPXI_USER_DEFINED_ERROR_MAX    255 //0xFF
#define DEWEPXI_USER_DEFINED_WARNING_BASE -1
#define DEWEPXI_USER_DEFINED_WARNING_MIN  -99999
//---------------------------------------------------------------------------

enum dewepxi_error_codes
{

#define TRION_CONSTS_BEGIN
#define TRION_CONSTS_END
#define TRION_ERROR(name, val) ERR_##name = val,
#define TRION_ERROR2(name, val) ERROR_##name = val,
#define TRION_WARNING(name, val) WARNING_##name = val,
#include "dewepxi_err.inc"
#undef TRION_CONSTS_BEGIN
#undef TRION_CONSTS_END
#undef TRION_ERROR
#undef TRION_ERROR2
#undef TRION_WARNING

};

#endif //__DEWEPXI_ERR_H
