// Copyright DEWETRON GmbH 2013
#include "uni_assert.h"
#include "uni_string_buffer.h"
#include <stdio.h>
#include <stdarg.h>
#include <iostream>

#ifdef WIN32
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#endif

namespace uni
{

#ifdef WIN32
AssertBehavior DefaultAssertHandlerWin32(const char* condition, const char* msg, const char* file, int line)
{
    uni::StringBuffer<1000, 0> buf;
    buf.printf("Debug Assertion Failed!\n\n");
    buf.printf("Reason:\t%s\n", msg?msg:condition);
    buf.printf("Location:\t%s : %d\n\n", file, line);
    buf.printf("(Press Retry to debug the application)");
    switch(MessageBoxA(NULL, buf.c_str(), "DEWETRON Debug Library", MB_CANCELTRYCONTINUE| MB_ICONSTOP | MB_DEFBUTTON2 | MB_SYSTEMMODAL))
    {
    case IDCANCEL:
        exit(-1);
    case IDTRYAGAIN:
    case IDRETRY:
        return Assert_Halt;
    default:
        return Assert_Continue;
    }
}
#endif

AssertBehavior HaltAlwaysAssertHandler(const char* condition, const char* msg, const char* file, int line)
{
    uni::StringBuffer<1000, 0> buf;
    buf.printf("Debug Assertion Failed!\n\n");
    buf.printf("Reason:\t%s\n", msg?msg:condition);
    buf.printf("Location:\t%s : %d\n\n", file, line);

    std::cerr << buf.c_str() << std::endl;

    return Assert_Halt;
}

AssertBehavior ContinueAlwaysAssertHandler(const char* condition, const char* msg, const char* file, int line)
{
    return Assert_Continue;
}

#ifdef NDEBUG
AssertHandler g_assert_handler(ContinueAlwaysAssertHandler);
#else
# ifdef WIN32
AssertHandler g_assert_handler(DefaultAssertHandlerWin32);
# else
AssertHandler g_assert_handler(HaltAlwaysAssertHandler);
# endif
#endif

AssertHandler GetAssertHandler()
{
    return g_assert_handler;
}

void SetAssertHandler(AssertHandler new_handler)
{
    g_assert_handler = new_handler;
}

uni::AssertBehavior ReportFailedAssert(const char* condition, const char* file, int line, const char* msg, ...)
{
    char buffer[256];
    if (msg)
    {
        va_list args;
        va_start(args, msg);
        vsnprintf(buffer, sizeof(buffer), msg, args);
        va_end(args);
    }

    uni::AssertBehavior ret = Assert_Continue;
    AssertHandler h = g_assert_handler;
    if (h)
    {
        ret = h(condition, msg?buffer:NULL, file, line);
    }
    return ret;
}

}
