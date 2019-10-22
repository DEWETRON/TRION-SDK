// Copyright DEWETRON GmbH 2013
#ifndef _UNI_ASSERT_H_
#define _UNI_ASSERT_H_

#include <sstream>

namespace uni
{

    enum AssertBehavior
    {
        Assert_Halt,
        Assert_Continue,
    };

    typedef AssertBehavior (*AssertHandler)(const char* condition, const char* msg, const char* file, int line);

    AssertHandler GetAssertHandler();
    void SetAssertHandler(AssertHandler new_handler);

    AssertBehavior ReportFailedAssert(const char* condition, const char* file, int line, const char* msg, ...);


#ifdef WIN32
    AssertBehavior DefaultAssertHandlerWin32(const char* condition, const char* msg, const char* file, int line);
#endif
    AssertBehavior HaltAlwaysAssertHandler(const char* condition, const char* msg, const char* file, int line);
    AssertBehavior ContinueAlwaysAssertHandler(const char* condition, const char* msg, const char* file, int line);

}

#ifdef WIN32
  #define UNI_BREAK() __debugbreak()
#elif __GNUC__
  #define UNI_BREAK() __builtin_trap()
#else
  #define UNI_BREAK() assert(false)
#endif


#define UNI_UNUSED(x) do { (void)sizeof(x); } while(0)

#ifndef UNI_ASSERTS_DISABLED
  #ifndef UNI_ASSERTS_ENABLED
    #ifndef NDEBUG
      #define UNI_ASSERTS_ENABLED
    #endif
  #endif
#endif

#ifdef UNI_ASSERTS_ENABLED

#define UNI_ASSERT(cond) \
    do \
    { \
        if (!(cond)) \
        { \
            if (uni::ReportFailedAssert(#cond, __FILE__, __LINE__, 0) == uni::Assert_Halt) \
                UNI_BREAK(); \
        } \
    } while(0)

#define UNI_ASSERT_EQUAL(val1, val2) \
    do \
    { \
        if (val1 != val2) \
        { \
            std::stringstream msg; \
            msg << #val1 << " != " << #val2 << " (" << val1 << " != " << val2 <<")"; \
            if (uni::ReportFailedAssert(msg.str().c_str(), __FILE__, __LINE__, 0) == uni::Assert_Halt) \
                UNI_BREAK(); \
        } \
    } while(0)

#define UNI_ASSERT_MSG(cond, ...) \
    do \
    { \
        if (!(cond)) \
        { \
            if (uni::ReportFailedAssert(#cond, __FILE__, __LINE__, __VA_ARGS__) == uni::Assert_Halt) \
                UNI_BREAK(); \
        } \
    } while(0)

#define UNI_ASSERT_FAIL(...) \
    do \
    { \
        if (uni::ReportFailedAssert(0, __FILE__, __LINE__, __VA_ARGS__) == uni::Assert_Halt) \
          UNI_BREAK(); \
    } while(0)

#define UNI_VERIFY(cond) UNI_ASSERT(cond)

#define UNI_VERIFY_MSG(cond, msg, ...) UNI_ASSERT_MSG(cond, msg, ##__VA_ARGS__)


#else //UNI_ASSERTS_ENABLED


#define UNI_ASSERT(condition) \
    do { UNI_UNUSED(condition); } while(0)

#define UNI_ASSERT_EQUAL(val1, val2) \
    do { UNI_UNUSED(val1); UNI_UNUSED(val2); } while(0)

#define UNI_ASSERT_MSG(condition, msg, ...) \
    do { UNI_UNUSED(condition); UNI_UNUSED(msg); } while(0)

#define UNI_ASSERT_FAIL(msg, ...) \
    do { UNI_UNUSED(msg); } while(0)

#define UNI_VERIFY(condition) \
    do { (void)(condition); } while (0)

#define UNI_VERIFY_MSG(condition, msg, ...) \
    do { (void)(condition); UNI_UNUSED(msg); } while(0)


#endif //UNI_ASSERTS_ENABLED

#endif // _UNI_ASSERT_H_
