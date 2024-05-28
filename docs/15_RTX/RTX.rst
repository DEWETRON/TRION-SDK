RTX Realtime System Support
===========================

The TRION API is capable of running in the real-time environment `RTX64 <https://www.intervalzero.com/en-products/en-rtx64/>`_ of the IntervalZero RTOS platform (currently version 4.x).
For this purpose, there is special version the the TRION API as a real-time DLL called ``dwpxi_api_x64.rtdll`` that can be loaded from real-time processes.
The basic usage of the API is identical to the Windows/Linux variant.
The RTX enabled version has additional functionalities such as "data polling" and thread affinity control.
But due to limitations of the RTX subsystem, some functions which such as firmware upgrades cannot be used inside real-time processes.
In addition, simulation support is limited.

Setting up TRION hardware for use inside RTX64
----------------------------------------------

In order to run real-time processes in the RTX64 subsystem, the system must have a properly installed, configured and licensed RTX environment with at least one CPU core dedicated to the real-time subsystem (RTSS).
Please refer to the IntervalZero `Online Help <https://help.intervalzero.com/product_help/RTX64_4/RTX64_4x_Help.htm>`_ for more information on this topic.

After the RTSS is configured, TRION PXIe boards need to be assigned exclusively to RTX64.
The required steps are documented in `Converting a Windows Device to an RTX64 Device <https://help.intervalzero.com/product_help/RTX64_4/RTX64_4x_Help.htm#Topics/NAL/Converting_a_Windows_Device_to_an_RTX_Device.htm>`_ in the RTX64 online help.
Dewetron provides signed ``rtx64pnp_trion.inf`` and ``rtx64pnp_trion.cat`` files for TRION boards and TRION chassis controllers.
After successful installation, TRION devices will be shown under the ``RTX64 Drivers`` section of the Windows Device Manager.
Note that after TRION devices have been assigned to RTX64, they can no longer be accessed from a Windows process.

Enabling Multiple Inter-Processor Interrupts
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
By default, there is a limitation in the RTX64 configuration that prevents the use of more than 13 PCI devices such as TRION boards at the same time from a real-time process.
This limitation will make it impossible to use a fully equipped Dewetron RM16 chassis, for example.
The IntervalZero knowledge base descibes a solution to this problem:

    To prevent jitter in RTSS thread execution latency, RTX64 disables Windows Inter-Processor Interrupts (IPIs).
    This leaves only thirteen (13) interrupt vectors available to which RTSS devices can attach.
    If more IPIs are needed, and you can tolerate the resulting jitter in RTSS thread execution latency, you can enable multiple interrupts in the Windows registry.

In order to enable IPIs, open the Windows registry editor.
Under the key ``HKEY_LOCAL_MACHINE/SOFTWARE/IntervalZero/RTX64`` create or set the DWORD-value ``EnableMultipleInterruptLevel=2`` (default is 0).
After a restart of the system, it should be possible to use a fully equipped RM16 chassis (which contains 16 TRION boards and 4 chassis controller boards).

Resource usage of the TRION API
-------------------------------

This section describes the use of processing and memory resources required by TRION API.

Multi-Threading
~~~~~~~~~~~~~~~
By default, the TRION API does not create own internal threads (except for threads that are automatically created by RTSS such as timers).
There is however the possibility to explicitly enable multi-threaded initialization, which speeds up startup times (when called before ``DeWeDriverInit``):

.. code:: c

    DeWeSetParamStruct_str("driver/api/config/thread", "enabled", "true");

By default, the number of worker threads is equal to the number of exclusive RTX CPU cores.
This can be changed via a change of the ``PoolSize`` property (e.g. to 8 threads):

.. code:: c

    DeWeSetParamStruct_str("driver/api/config/thread", "PoolSize", "8");

If required, the CPU affinity of created worker threads can be controlled via the ``Affinity`` property, which requires a bit-mask with a 1 for each enabled CPU as a numeric string.
The following example enables CPU 4 and 6 (01010000b = 0x50):

.. code:: cpp

    DeWeSetParamStruct_str("driver/api/config/thread",
                           "Affinity", std::to_string(0x50));

If the ``Affinity`` is set to 0 (its default value), all available CPUs are utilized (via a query to ``RtGetProcessAffinityMask``).

In addition to worker threads, each TRION board will be assigned an interrupt service thread (IST), where each thread has the affinity set to one CPU.
Currently, the affinity cannot be controlled via the ``Affinity`` property.

Interrupt triggered sample polling
----------------------------------
By default, all data transfer between TRION boards and the application if done via direct memory access (DMA), usually by transferring a block of several sample values at each transaction.
In a real-time context, it is possible to disable DMA and query each sample value directly via PXI register access.
In addition, it is possible to register a callback function that gets called directly from the IST whenever a new sample is available.
This makes it possible to read single samples from up to 16 TRION boards (8 or more channels each) at a sampling rate of up to 1000 Hz.

Advanced RTX Command ID Enumeration
-----------------------------------

There is a set of i32/i64 commands that is exclusively available when running the RTX version of the TRION API.

