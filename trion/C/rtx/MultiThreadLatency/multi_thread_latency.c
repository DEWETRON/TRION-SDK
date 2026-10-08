/*
 * RTX64 demo: 16 threads, each waiting on its own single-shot (auto-reset) event.
 *
 * Main thread:
 *   - creates 16 auto-reset events and 16 worker threads
 *   - set all 16 events in a for loop
 *   - measure time for one round-trip
 *
 * Build as an RTSS application (RTAPP) in Visual Studio with the RTX64 SDK.
 */

#include <windows.h>
#include <rtapi.h>
#include <stdio.h>

#define NUM_THREADS   16
#define STATS_INTERVAL 100000u     /* print round-trip statistics every N rounds */
//#define NUM_ROUNDS    (STATS_INTERVAL * 50u)
#define NUM_ROUNDS    0x7FFFFFFF
#define WAIT_TIMEOUT_MS 500
#define STACK_SIZE    0x40000
#if 0 // 0 = single CPU, 1 = multi CPU pinning
#define MIN_CPU       3     /* first worker CPU (mask 1 << MIN_CPU = 0x10) */
#define MAX_CPU       7     /* last worker CPU  (mask 1 << MAX_CPU = 0x80) */
#else
#define MIN_CPU       5
#define MAX_CPU       5
#endif
#define NUM_CPUS      (MAX_CPU - MIN_CPU + 1)
#define SPAWN_MULTIPLE_THREADS
//#define ENABLE_MONITORING

typedef struct _WORKER_CTX
{
    int             index;
    HANDLE          hEvent;     /* this thread's private event */
    volatile LONG   wakeCount;  /* how many times the thread was released */
} WORKER_CTX;

static WORKER_CTX     g_ctx[NUM_THREADS];
static HANDLE         g_hThread[NUM_THREADS];
static volatile LONG  g_quit = 0;
static volatile LONG  g_num_handled = 0;
static HANDLE         g_all_done;
static HANDLE         g_stopAcqEvent;

static BOOL DoWork(WORKER_CTX* ctx)
{
    // Cegelec-style stop event query
    {
        HANDLE Events[2] = { g_stopAcqEvent };
        DWORD stat = RtWaitForMultipleObjects(2, Events, FALSE, 0);	// Timeout is 0 : do not block
        if (stat == WAIT_OBJECT_0)			// Stop request (first event fired)
        {
            return FALSE;
        }
    }

    DWORD rc = RtWaitForSingleObject(ctx->hEvent, WAIT_TIMEOUT_MS);

    if (rc != WAIT_OBJECT_0)
    {
        RtPrintf("Thread %d: wait failed or timed out (rc=0x%lX)\n", ctx->index, rc);
        return FALSE;
    }

    if (g_quit)
        return FALSE;

    InterlockedIncrement(&ctx->wakeCount);
    if (InterlockedIncrement(&g_num_handled) == NUM_THREADS)
    {
        RtSetEvent(g_all_done);
    }
    return TRUE;
}

#ifdef SPAWN_MULTIPLE_THREADS

/* Worker thread: wait on own event, count the wake-up, repeat until told to quit. */
static ULONG RTFCNDCL WorkerThread(void* param)
{
    WORKER_CTX* ctx = (WORKER_CTX*)param;

    for (;;)
    {
        if (!DoWork(ctx))
        {
            break;
        }
    }

    return 0;
}

#else

/* Single worker thread: serve all contexts in turn, repeat until told to quit. */
static ULONG RTFCNDCL SingleWorkerThread(void* param)
{
    int i;

    UNREFERENCED_PARAMETER(param);

    for (;;)
    {
        for (i = 0; i < NUM_THREADS; i++)
        {
            if (!DoWork(&g_ctx[i]))
            {
                return 0;
            }
        }
    }
}
#endif

void _cdecl wmain(int argc, wchar_t** argv)
{
    int i, round;
    LARGE_INTEGER freq, t_run_start;
    /* round-trip statistics in QPC ticks, converted to ns only when printing */
    LONGLONG min_ticks = MAXLONGLONG, max_ticks = 0, sum_ticks = 0;
    int num_ticks = 0;
    /* overall statistics, folded in from each block when it is printed */
    LONGLONG total_max_ticks = 0, total_sum_ticks = 0;
    LONGLONG total_num_ticks = 0;

    UNREFERENCED_PARAMETER(argc);
    UNREFERENCED_PARAMETER(argv);

    RtPrintf("*** multi_thread_latency.c ***\n");
#ifdef SPAWN_MULTIPLE_THREADS
    RtPrintf("* Num Threads: %d\n", NUM_THREADS);
#else
    RtPrintf("* Single threaded - boards: %d\n", NUM_THREADS);
#endif
    RtPrintf("* CPUs: %d - %d\n", MIN_CPU, MAX_CPU);
    RtPrintf("\n");

    /* Create events (auto-reset => single-shot, initially non-signaled) and threads. */
    g_all_done = RtCreateEvent(NULL, FALSE /*auto-reset*/, FALSE /*non-signaled*/, NULL);
    g_stopAcqEvent = RtCreateEvent(NULL, TRUE /*manual-reset*/, FALSE /*non-signaled*/, NULL);
    for (i = 0; i < NUM_THREADS; i++)
    {
        g_ctx[i].index = i;
        g_ctx[i].wakeCount = 0;
        g_ctx[i].hEvent = RtCreateEvent(NULL, FALSE /*auto-reset*/, FALSE /*non-signaled*/, NULL);

        if (g_ctx[i].hEvent == NULL)
        {
            RtPrintf("RtCreateEvent %d failed, error %lu\n", i, GetLastError());
            goto cleanup;
        }
    }

#ifdef SPAWN_MULTIPLE_THREADS
    for (i = 0; i < NUM_THREADS; i++)
    {
        // create suspended worker thread
        g_hThread[i] = RtCreateThread(NULL, STACK_SIZE, WorkerThread, &g_ctx[i], 0x0004, NULL);

        if (g_hThread[i] == NULL)
        {
            RtPrintf("RtCreateThread %d failed, error %lu\n", i, GetLastError());
            goto cleanup;
        }

        RtSetThreadPriority(g_hThread[i], RT_PRIORITY_MIN);
        /* distribute threads round-robin over CPUs MIN_CPU..MAX_CPU, wrapping back to MIN_CPU */
        SetThreadAffinityMask(g_hThread[i], (DWORD_PTR)1 << (MIN_CPU + i % NUM_CPUS));
        RtResumeThread(g_hThread[i]);
    }
#else
    /* one worker thread serving all contexts; g_hThread[1..] stay NULL and are skipped on cleanup */
    g_hThread[0] = RtCreateThread(NULL, STACK_SIZE, SingleWorkerThread, NULL, 0x0004, NULL);

    if (g_hThread[0] == NULL)
    {
        RtPrintf("RtCreateThread failed, error %lu\n", GetLastError());
        goto cleanup;
    }

    RtSetThreadPriority(g_hThread[0], RT_PRIORITY_MIN);
    SetThreadAffinityMask(g_hThread[0], (DWORD_PTR)1 << MIN_CPU);
    RtResumeThread(g_hThread[0]);
#endif

    /* Give the threads a moment to reach their wait. */
    RtSleep(100);

    QueryPerformanceFrequency(&freq);

#ifdef ENABLE_MONITORING
    RtMonitorChangeState(MONITOR_CONTROL_START);
    int stop_count_down = 0;
#endif

    QueryPerformanceCounter(&t_run_start);

    /* Main loop: signal every thread's event, wait 1 ms, repeat. */
    for (round = 0; round < NUM_ROUNDS; round++)
    {
        LARGE_INTEGER t_start, t_end;
        LONGLONG ticks = 0;

        QueryPerformanceCounter(&t_start);

        g_num_handled = 0;

        for (i = 0; i < NUM_THREADS; i++)
        {
            RtSetEvent(g_ctx[i].hEvent);
        }

        RtWaitForSingleObject(g_all_done, WAIT_TIMEOUT_MS);

        QueryPerformanceCounter(&t_end);

        ticks = t_end.QuadPart - t_start.QuadPart;

        if (ticks < min_ticks) min_ticks = ticks;
        if (ticks > max_ticks) max_ticks = ticks;
        sum_ticks += ticks;
        num_ticks++;

#ifdef ENABLE_MONITORING
        if (round > 1000000) // ignore initial noise
        {
            if ((LONG)(ticks * 1000000LL / freq.QuadPart) > 100)
            {
                stop_count_down = 3;
            }
            if (stop_count_down > 0 && --stop_count_down == 0)
            {
                // stop early to allow investigation
                break;
            }
        }
#endif

        /* Report round-trip statistics every STATS_INTERVAL rounds (and for a trailing partial
           block), outside the timed section so RtPrintf doesn't affect the measurement. */
        if (num_ticks == STATS_INTERVAL || (round == NUM_ROUNDS - 1 && num_ticks > 0))
        {
            /* computed in ns, printed as us with 3 decimals (integer math, no float formatting) */
            LONG min_10ns = (LONG)(min_ticks * 100000000LL / freq.QuadPart);
            LONG max_10ns = (LONG)(max_ticks * 100000000LL / freq.QuadPart);
            LONG avg_100ns = (LONG)(sum_ticks * 10000000LL / (num_ticks * freq.QuadPart));
            LONG elapsed_s = (LONG)((t_end.QuadPart - t_run_start.QuadPart) / freq.QuadPart);

            RtPrintf("%02ld:%02ld:%02ld %d cycles: round-trip min %ld.%02ld us, max %ld.%02ld us, avg %ld.%01ld us\n",
                elapsed_s / 3600, (elapsed_s / 60) % 60, elapsed_s % 60,
                round + 1,
                min_10ns / 100, min_10ns % 100,
                max_10ns / 100, max_10ns % 100,
                avg_100ns / 10, avg_100ns % 10);

            if (max_ticks > total_max_ticks) total_max_ticks = max_ticks;
            total_sum_ticks += sum_ticks;
            total_num_ticks += num_ticks;

            min_ticks = MAXLONGLONG;
            max_ticks = 0;
            sum_ticks = 0;
            num_ticks = 0;
        }

        //RtSleep(1);
    }

    RtSleep(10); /* let the last round drain */

    /* Total result over all rounds. The sum is split into whole seconds and remainder before
       scaling to ns, so sum_ticks * 1e9 can't overflow for long runs / high QPC frequencies. */
    if (total_num_ticks > 0)
    {
        LONGLONG total_sum_ns = (total_sum_ticks / freq.QuadPart) * 1000000000LL
            + (total_sum_ticks % freq.QuadPart) * 1000000000LL / freq.QuadPart;
        LONG max_ns = (LONG)(total_max_ticks * 1000000000LL / freq.QuadPart);
        LONG avg_ns = (LONG)(total_sum_ns / total_num_ticks);

        RtPrintf("TOTAL over %I64d rounds: round-trip max %ld.%03ld us, avg %ld.%03ld us\n",
            total_num_ticks,
            max_ns / 1000, max_ns % 1000,
            avg_ns / 1000, avg_ns % 1000);
    }

#ifdef ENABLE_MONITORING
    RtMonitorChangeState(MONITOR_CONTROL_STOP);
#endif

cleanup:
    /* Tell workers to quit and release them one last time. */
    InterlockedExchange(&g_quit, 1);

    RtSetEvent(g_stopAcqEvent);

    for (i = 0; i < NUM_THREADS; i++)
    {
        if (g_ctx[i].hEvent != NULL)
            RtSetEvent(g_ctx[i].hEvent);
    }

    /* Join threads. */
    for (i = 0; i < NUM_THREADS; i++)
    {
        if (g_hThread[i] != NULL)
        {
            RtWaitForSingleObject(g_hThread[i], WAIT_TIMEOUT_MS);
            RtCloseHandle(g_hThread[i]);
        }
    }

    /* Report results and free events. */
    for (i = 0; i < NUM_THREADS; i++)
    {
        if (g_ctx[i].hEvent != NULL)
        {
            //RtPrintf("Thread %2d woke %ld times (expected %d)\n",
            //    i, g_ctx[i].wakeCount, NUM_ROUNDS);
            RtCloseHandle(g_ctx[i].hEvent);
        }
    }

    RtCloseHandle(g_all_done);
    RtCloseHandle(g_stopAcqEvent);

    RtPrintf("Done.\n");
}
