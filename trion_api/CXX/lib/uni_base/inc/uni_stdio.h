// Copyright DEWETRON GmbH 2012


#ifndef _UNI_STDIO_H_
#define _UNI_STDIO_H_


/**
 * This header provides compatibility implementations for snprintf and vsnprintf
 * These are C99 compliant.
 */

#include <stdio.h>
#include <stdarg.h>

#if defined(_MSC_VER) && _MSC_VER < 1900

#ifndef snprintf
#define snprintf c99_snprintf
#else
#error
#endif

#ifndef vsnprintf
#define vsnprintf c99_vsnprintf
#else
#error
#endif

#define HAVE_SNPRINTF

inline int c99_vsnprintf(char* str, size_t size, const char* format, va_list ap)
{
    int count = -1;

    if (size != 0)
        count = _vsnprintf_s(str, size, _TRUNCATE, format, ap);
    if (count == -1)
        count = _vscprintf(format, ap);

    return count;
}

inline int c99_snprintf(char* str, size_t size, const char* format, ...)
{
    int count;
    va_list ap;

    va_start(ap, format);
    count = c99_vsnprintf(str, size, format, ap);
    va_end(ap);

    return count;
}
#endif // _MSC_VER

#if defined(_MSC_VER)

#define snscanf _snscanf

#endif // _MSC_VER && _MSC_VER < 1900

#ifdef UNIX

#ifndef snscanf
#define snscanf(data, size, format, ...) sscanf(data, format, __VA_ARGS__)
#endif //snscanf

#endif //UNIX


#endif // _UNI_STDIO_H_
