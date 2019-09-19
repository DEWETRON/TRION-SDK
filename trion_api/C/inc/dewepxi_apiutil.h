// Copyright (c) Dewetron 2013

#ifndef _DEWEPXI_APIUTIL_H_
#define _DEWEPXI_APIUTIL_H_
#pragma once

#include <stdio.h>
#include <stdarg.h>

#ifdef WIN32
#include <windows.h>
#include <conio.h>
#endif
#if defined(UNIX) || defined(__APPLE__)
#include <unistd.h>
#include <termios.h>
#include <string.h>
#endif

#include "dewepxi_types.h"


#ifdef WIN32
#ifdef _MSC_VER

#ifndef MAX
 #define MAX(a,b) (((a) > (b)) ? (a) : (b))
#endif

#ifndef MIN
 #define MIN(a,b) (((a) < (b)) ? (a) : (b))
#endif

#ifndef snprintf
#define snprintf _c99_snprintf
#endif

#ifndef vsnprintf
#define vsnprintf _c99_vsnprintf
#endif

#ifndef stricmp
#define stricmp _stricmp
#endif

#ifndef strnicmp
#define strnicmp _strnicmp
#endif

#ifndef getch
#define getch _getch
#endif

#ifndef strdup
#define strdup _strdup
#endif


__inline int _c99_vsnprintf(char* str, size_t size, const char* format, va_list ap)
{
    int count = -1;

    if (size != 0)
        count = _vsnprintf_s(str, size, _TRUNCATE, format, ap);
    if (count == -1)
        count = _vscprintf(format, ap);

    return count;
}

__inline int _c99_snprintf(char* str, size_t size, const char* format, ...)
{
    int count;
    va_list ap;

    va_start(ap, format);
    count = _c99_vsnprintf(str, size, format, ap);
    va_end(ap);

    return count;
}


#define snscanf _snscanf
#define kbhit _kbhit

#endif // _MSC_VER
#endif //WIN32



#if defined(UNIX)

#define stricmp strcasecmp
#define strnicmp strncasecmp

#ifndef snscanf
#define snscanf(data, size, format, ...) sscanf(data, format, __VA_ARGS__)
#endif

#ifndef MAX
 #define MAX(a,b) \
   ({  __typeof__ (a) _a = (a); \
       __typeof__ (b) _b = (b); \
     _a > _b ? _a : _b; })
#endif

#ifndef MIN
 #define MIN(a,b) \
   ({  __typeof__ (a) _a = (a); \
       __typeof__ (b) _b = (b); \
     _a < _b ? _a : _b; })
#endif


#if defined(__clang__)
#define EXTERN_DECL extern
#elif defined(__GNUC__)
#define GCC_VERSION (__GNUC__ * 10000                 \
                    + __GNUC_MINOR__ * 100            \
                    + __GNUC_PATCHLEVEL__)
#if GCC_VERSION > 50100
#define EXTERN_DECL extern
#else
#define EXTERN_DECL
#endif
#else
#error unsupported compiler
#endif

EXTERN_DECL inline int getch()
{
    struct termios term, oterm;
    int fd = 0;
    int c = 0;
    tcgetattr(fd, &oterm);
    memcpy(&term, &oterm, sizeof(term));
    term.c_lflag = term.c_lflag & (!ICANON);
    term.c_cc[VMIN] = 0;
    term.c_cc[VTIME] = 1;
    tcsetattr(fd, TCSANOW, &term);
    c = getchar();
    tcsetattr(fd, TCSANOW, &oterm);
    return c;
}

EXTERN_DECL inline void Sleep(uint32 timeout)
{
    usleep((timeout * 1000) % 1000000);
    sleep(timeout/1000);
}

EXTERN_DECL inline int kbhit(void)
{
    struct termios term, oterm;
    int fd = 0;
    int c = 0;
    tcgetattr(fd, &oterm);
    memcpy(&term, &oterm, sizeof(term));
    term.c_lflag = term.c_lflag & (!ICANON);
    term.c_cc[VMIN] = 0;
    term.c_cc[VTIME] = 1;
    tcsetattr(fd, TCSANOW, &term);
    c = getchar();
    tcsetattr(fd, TCSANOW, &oterm);
    if (c != -1)
        ungetc(c, stdin);
    return ((c != -1) ? 1 : 0);
}

#endif // UNIX



#endif // _DEWEPXI_APIUTIL_H_
