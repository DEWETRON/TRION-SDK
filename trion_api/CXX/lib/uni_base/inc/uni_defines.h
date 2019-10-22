// Copyright Dewetron 2012
#ifndef __UNI_DEFINES_H__
#define __UNI_DEFINES_H__

#if _MSC_VER > 1000
#   pragma once
#endif

/** @addtogroup UNI_UTIL */
//@{

/**
 * Basic defines for Software Development,
 * Defines for target operating system, target cpu, ...
 */

#ifdef _MSC_VER
#if _MSC_VER > 1000

    // Compiler Warning (level 1) C4251
    // 'identifier' : class 'type' needs to have dll-interface to be used by
    // clients of class 'type2'
    //
    // The specified base class was not declared with the __declspec(dllexport)
    // keyword.
    //
    // A base class or structure must be declared with the __declspec(dllexport)
    // keyword if a function in a derived class is to be exported.
#   pragma warning (disable: 4251)

    // Compiler Warning (level 2) C4275
    // non - DLL-interface classkey 'identifier' used as base for DLL-interface
    // classkey 'identifier'
    //
    // An exported class was derived from a class that was not exported.
#   pragma warning (disable: 4275)

    // Compiler Warning (level 1) C4503
    // warning C4503: 'a method' : decorated name length exceeded, name was truncated
#   pragma warning (disable: 4503)

    // Compiler Warning (level 3) C4786
    // 'identifier' : identifier was truncated to 'number' characters in the debug
    // information
    //
    // The identifier string exceeded the maximum allowable length and was
    // truncated.
    //
    // The debugger cannot debug code with symbols longer than 255 characters. In
    // the debugger, you cannot view, evaluate, update, or watch the truncated
    // symbols.
#   pragma warning (disable: 4786)

#endif  // _MSC_VER > 1000

#if _MSC_VER >= 1400

    // === WARNINGS AS ERROR ===

    //warning C4715: 'function' not all control paths return a value
#   pragma warning (error: 4715)


    // === DISABLED WARNINGS ===

    //warning C4996: 'a method' was declared deprecated
#   pragma warning (disable: 4996)

    // warning C4503: 'a method' : decorated name length exceeded, name was truncated
#   pragma warning (disable: 4503)

   // warning C4100: 'a parameter' : parameter list contains unreferenced formal parameter
#   pragma warning (disable: 4100)

    // warning C4290: C++ exception specification ignored except to indicate a
    // function is not __declspec(nothrow)
#   pragma warning (disable: 4290)

    //Compiler Warning (level 3) C4018:
    //'expression' : signed/unsigned mismatch
#   pragma warning (disable: 4018)

    //Compiler Warning (level 1) C4244
    //'variable' : conversion from 'type' to 'type', possible loss of data
    //
    //Compiler Warning (levels 3 and 4) C4244
    //'conversion' conversion from 'type1' to 'type2', possible loss of data
#   pragma warning (disable: 4244)

    //Compiler Warning (level 4) C4245
    //'conversion' : conversion from 'type1' to 'type2', signed/unsigned mismatch
#   pragma warning (disable: 4245)

    //Compiler Warning (level4 ) C4389
    //'operator' : signed/unsigned mismatch
#   pragma warning (disable: 4389)

    //Compiler Warning (level4 ) C4127
    //conditional expression is constant
#   pragma warning (disable: 4127)

    // Prevent inclusion of winsock.h in windows.h
#   define _WINSOCKAPI_

#endif  // _MSC_VER > 1400

#endif  // _MSC_VER

/*
 * Compiler identification
 */

/*
 * C++ compiler
 */
#ifdef __cplusplus

#   ifndef NULL
#       define NULL 0
#   endif

#else   // __cplusplus

#   ifndef NULL
#       define NULL ((void *) 0)
#   endif

#endif  // __cplusplus


/*
 * x86 M$ C++ compiler
 */
#ifdef _MSC_VER

#   ifdef UNDER_CE
#       define TARGET_OS 'W_CE'
#   else   // UNDER_CE
#       define TARGET_OS 'W_NT'
#   endif  // UNDER_CE

#   define TARGET_CPU 'I86'

    /**
     * @brief Macro for initializing 64 bit values
     *
     * The literal of a 64bit integer is not the same on all compilers
     * e.g. the gcc does not compile if you do not add the integer literal
     *
     * Instead:
     * @code
     * uint64 val = 0xffffffffffffffff;
     * @endcode
     *
     * Use:
     * @code
     * uint64 val = UINT64_VAL(0xffffffffffffffff);
     * @endcode
     *
     * This will automatically add the correct integer literal for the 64bit value
     */

#   define UINT64_VAL(val) (val##ui64)
#   define SINT64_VAL(val) (val##i64)
#   define FORMAT_UINT64 "I64u"
#   define FORMAT_SINT64 "I64d"
#   define FORMAT_XINT64 "I64x"
#   define LFORMAT_UINT64 L"I64u"
#   define LFORMAT_SINT64 L"I64d"
#   define LFORMAT_XINT64 L"I64x"

#   ifdef BUILD_X86
#       define FORMAT_SIZE_T "u"
#       define LFORMAT_SIZE_T L"u"
#   endif
#   ifdef BUILD_X64
#       define FORMAT_SIZE_T FORMAT_UINT64
#       define LFORMAT_SIZE_T LFORMAT_UINT64
#   endif

#endif  // _MSC_VER

/*
 * Any GNU C++ compiler
 */
#if defined(__MINGW32__) || defined(__CYGWIN32__) || defined(__GNUC__) || defined(__clang__)

#   if defined __i386__
#       define TARGET_CPU 'I86'
#   elif defined __x86_64
#       define TARGET_CPU 'ET64'
#   elif defined __PPC__
#       if defined __PPC64__
#           define TARGET_CPU 'PP64'
#       else    // __PPC64__
#           define TARGET_CPU 'PPC'
#       endif   // __PPC64__
#       define UNI_BIG_ENDIAN
#   elif defined __arm__
#       if __ARM_ARCH == 7
#           define TARGET_CPU 'armhf'
#       elif defined __aarch64__
#           define TARGET_CPU 'arm64'
#       endif   // __PPC64__
#   endif

#   if defined(__MINGW32__)
#       define TARGET_OS 'W_NT'
#   elif defined(__CYGWIN32__) || defined(__GNUC__)
#       define TARGET_OS 'POSX'
#   endif

#   define UINT64_VAL(val) (val##ull)
#   define SINT64_VAL(val) (val##ll)
#   define FORMAT_UINT64 "llu"
#   define FORMAT_SINT64 "lld"
#   define FORMAT_XINT64 "llx"
#   define FORMAT_SIZE_T "zu"
#   define LFORMAT_UINT64 L"llu"
#   define LFORMAT_SINT64 L"lld"
#   define LFORMAT_XINT64 L"llx"
#   define LFORMAT_SIZE_T L"zu"

#   ifndef __MINGW32__
#       define snwprintf swprintf
#       define vsnwprintf vswprintf
#   endif

#endif  // __MINGW32__ || __CYGWIN32__ || __GNUC__

/*
 * GNU C++ handles "override" keyword only beginning with version 4.7
 */
#if __GNUC__ == 4 && __GNUC_MINOR__<7 && !(defined(__clang__) || defined(__INTEL_COMPILER))
#   define UNI_OVERRIDE
#   define UNI_FINAL
#   define UNI_EMPTY_THROW_CXX98 throw()
#else
#   define UNI_OVERRIDE override
#   define UNI_FINAL    final
#   define UNI_EMPTY_THROW_CXX98
#endif

// check if target os and target cpu are setup
#ifndef TARGET_OS
#   error "unable to setup TARGET_OS"
#endif
#ifndef TARGET_CPU
#   error "unable to setup TARGET_CPU"
#endif

// macro for deprecated features
#ifndef UNI_DEPRECATED
#   if __cplusplus >= 201402L
#       define UNI_DEPRECATED [[deprecated]]
#   else
#       if defined(__GNUC__) || defined(__clang__) || defined(__INTEL_COMPILER)
#           if __cplusplus == 201103
#               define UNI_DEPRECATED [[gnu::deprecated]]
#           else
#               define UNI_DEPRECATED __attribute__((deprecated))
#           endif
#       elif defined(_MSC_VER) && _MSC_VER >= 1300
#           define UNI_DEPRECATED __declspec(deprecated)
#       else
#           define UNI_DEPRECATED
#       endif
#   endif
#endif

/*
 * noexcept is not supported for all compilers
 * C++11 issue
 */
#if defined(__clang__)
#  if __has_feature(cxx_noexcept)
#    define UNI_NOEXCEPT_SUPPORTED
#    define UNI_NOEXCEPT noexcept
#  else
#    ifdef UNI_NOEXCEPT_SUPPORTED
#      undef UNI_NOEXCEPT_SUPPORTED
#    endif
#    define UNI_NOEXCEPT
#  endif
#else
#  if defined(__GXX_EXPERIMENTAL_CXX0X__) && __GNUC__ * 10 + __GNUC_MINOR__ >= 46 || \
      defined(_MSC_VER) && _MSC_VER >= 1900
#    define UNI_NOEXCEPT_SUPPORTED
#    define UNI_NOEXCEPT noexcept
#  else
#    ifdef UNI_NOEXCEPT_SUPPORTED
#      undef UNI_NOEXCEPT_SUPPORTED
#    endif
#    define UNI_NOEXCEPT
#  endif
#endif

/**
 * __restrict is not supported for all compilers
 */
#if defined(__GNUC__)
#  define UNI_RESTRICT __restrict__
#elif defined(_MSC_VER)
#  define UNI_RESTRICT __restrict
#else
#  define UNI_RESTRICT
#endif

/**
 * constexpr is not supported by all compilers
 */
#if __cplusplus >= 201103L || (defined(_MSC_VER) && _MSC_VER >= 1900)
#  define UNI_CONSTEXPR constexpr
#else
#  define UNI_CONSTEXPR
#endif

/**
 * r-value references (e.g. move semantics) are not supported by all compilers
 */
#if __cplusplus >= 201103L
#  define UNI_HAS_RVALUE_REFERENCES
#elif defined(_MSC_VER) && _MSC_VER >= 1800
#  define UNI_HAS_RVALUE_REFERENCES
#endif

//to be used as access specifier for unit test interface
//set this to public in unit test implementations before including headers
#ifndef UNIT_TEST_ACCESSIBLE
#define UNIT_TEST_ACCESSIBLE private
#endif

//to be used as access specifier for unit test interface
//set this to protected in unit test implementations before including headers
#ifndef UNIT_TEST_PROTECTED_ACCESSIBLE
#define UNIT_TEST_PROTECTED_ACCESSIBLE protected
#endif
//@}

#if defined __APPLE__ && defined __MACH__
#define UNI_PLAT_MACOS
#endif

#if defined __linux__ || defined __gnu_linux__
#define UNI_PLAT_LINUX
#endif

#if defined _WIN32 \
    || defined __WIN32__ \
    || defined _WIN64 \
    || defined __WIN64__ \
    || defined __TOS_WIN__ \
    || defined __WINDOWS__
#define UNI_PLAT_WINDOWS
#endif

#if defined UNI_PLAT_WINDOWS && defined __GNUC__
#define UNI_PLAT_MINGW
#endif

// sanity check platform checks
#if ( defined UNI_PLAT_MACOS && defined UNI_PLAT_LINUX ) || ( defined UNI_PLAT_MACOS && defined UNI_PLAT_WINDOWS ) || ( defined UNI_PLAT_LINUX && defined UNI_PLAT_WINDOWS )
#error "unable to detect target platform"
#endif

#endif  // __UNI_DEFINES_H__
