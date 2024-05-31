RTX Realtime System Support
===========================

The TRION API is capable of running in the real-time environment `RTX64 <https://www.intervalzero.com/en-products/en-rtx64/>`_ of the IntervalZero RTOS platform (currently version 4.x).
For this purpose, there is special version the the TRION API as a real-time DLL called ``dwpxi_api_x64.rtdll`` that can be loaded from real-time processes.
The basic usage of the API is identical to the Windows/Linux variant.
The RTX enabled version has additional functionalities such as "data polling" and thread affinity control.
But due to limitations of the RTX subsystem, some functions which such as firmware upgrades cannot be used inside real-time processes.
In addition, simulation and logging support is limited.

Setting up TRION Hardware for use inside RTX64
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
The IntervalZero knowledge base describes a solution to this problem:

    To prevent jitter in RTSS thread execution latency, RTX64 disables Windows Inter-Processor Interrupts (IPIs).
    This leaves only thirteen (13) interrupt vectors available to which RTSS devices can attach.
    If more IPIs are needed, and you can tolerate the resulting jitter in RTSS thread execution latency, you can enable multiple interrupts in the Windows registry.

In order to enable IPIs, open the Windows registry editor.
Under the key ``HKEY_LOCAL_MACHINE/SOFTWARE/IntervalZero/RTX64`` create or set the DWORD-value ``EnableMultipleInterruptLevel=2`` (default is 0).
After a restart of the system, it should be possible to use a fully equipped RM16 chassis (which contains 16 TRION boards and 4 chassis controller boards).

Resource usage of the TRION API
-------------------------------

This section describes the use of processing and memory resources required by TRION API.

CPU / Multi-Threading
~~~~~~~~~~~~~~~~~~~~~
By default, the TRION API does not create own internal threads (except for threads that are automatically created by RTSS - such as timers).
There is however the possibility to explicitly enable multi-threaded initialization, which speeds up startup times and distributes interrupt service threads (when called before ``DeWeDriverInit``):

.. code:: c

    DeWeSetParamStruct_str("driver/api/config/thread", "enabled", "true");

By default, the number of worker threads is equal to the number of exclusive RTX CPU cores.
This can be changed via a change of the ``PoolSize`` property (e.g. to 8 threads):

.. code:: c

    DeWeSetParamStruct_str("driver/api/config/thread", "PoolSize", "8");

If required, the CPU affinity of created worker threads can be controlled via the ``Affinity`` property, which requires a bit-mask with a 1 for each enabled CPU as a numeric string.
Worker threads only are required during initialization of the boards.
The following example enables CPU 4 and 6 (01010000b = 0x50):

.. code:: cpp

    DeWeSetParamStruct_str("driver/api/config/thread",
                           "Affinity", std::to_string(0x50));

If the ``Affinity`` is set to 0 (its default value), all available CPUs are utilized (via a query to ``RtGetProcessAffinityMask``).

In addition to worker threads, each TRION board will be assigned an interrupt service thread (IST), where each thread has the affinity set to one CPU.
The ISTs use the same affinity mask and CPU allocation method as worker threads.

RAM Memory
~~~~~~~~~~

Most memory allocations in the TRION API are performed using default C++ heap allocations.
Driver related functions make use of ``RtAllocateLocalMemoryEx`` allocations explicitly, except for
DMA related memory, which is allocated using uncached physical memory using ``RtAllocateContiguousMemorySpecifyCache``.

Care has been taken to avoid physical memory fragmentation during startup and shutdown.
After frequent restarts of the API, memory fragmentation has been observered and failure of DMA memory allocation during startup may indicate that a reboot of the machine is required.
In a running acquisition loop no memory is allocated or freed by the API or driver.
Thus, no fragmentation in a longer running measurement is expected.

Harddisk
~~~~~~~~

The RTX version of the API/Driver creates a directory structure under ``C:\RTX\Dewetron\Trion`` where current system setups are stored.
If the directories do not exist, they are created automatically.


Interrupt-triggered Sample Polling
----------------------------------
By default, all data transfer between TRION boards and the application is done via direct memory access (DMA),
usually by transferring a block of several sample values at each transaction.
In a real-time context, it is possible to disable DMA and query each sample value directly via PXI register access.
In addition, it is possible to register a callback function that gets called directly from the IST whenever a new sample is available.
This makes it possible to read single samples from up to 16 TRION boards (8 or more channels each) at a sampling rate of up to 1000 Hz.

Disabling DMA
~~~~~~~~~~~~~

DMA can be deactivated by setting the ``DMABuffer0Enabled`` acquisition property to false.
This can be done duing board initialization or when loading of an XML setup.
In code, you can use the folling call to disable DMA on board 0:

.. code:: c

    DeWeSetParamStruct_str("BoardID0/AcqProp", "DMABuffer0Enabled", "False");

This call will disable the use of any DMA resources for board 0, including interrupts and data block configurations.

Sample Polling
~~~~~~~~~~~~~~

After disabling DMA, the only way to read sample data is via polling (polling can be used parallel to DMA though).
Polling is implemented as reading the last known sample value of each enabled (Used) channel of a single board into
an API internal buffer and then reading from the buffer in the user application.
The following code will update that buffer for board ``board_no``, get its address and size:

.. code:: c

    int num_values = 0;
    int32* data = NULL;
    DeWeGetParam_i32(board_no, CMD_BOARD_ACT_SAMPLE_VALUE_COUNT, &num_values);
    DeWeGetParam_i64(board_no, CMD_BOARD_ACT_SAMPLE_VALUE_POINTER, (sint64*)&data);

The layout of the values in ``data`` are dependend on the TRION board and correspond to the ``SYNC_DATA_SAMPLES`` registers.
``ai`` values are the 32bit full-scale signed ADC samples that need to be scaled according to the set range.
``cnt`` and ``boardcnt`` contain the raw counter values.

For a TRION3-1820-MULTI-8 board with 8 AI channels, 2 Counter channels and one Board-counter, the 14 values are stored in ``data`` can be described with the folling structure:

.. code:: c

    struct DataValues
    {
        int ai[8];
        struct { int count; int subcount; } cnt[2];
        struct { int count; int subcount; } boardcnt;
    };

A TRION3-CONTROLLER board will use the following layout (11 values in total):

.. code:: c

    struct DataValues
    {
        struct { int count; int subcount; } cnt[4];
        struct { int count; int subcount; } boardcnt;
        int dio;
    };


New Sample Notifications
~~~~~~~~~~~~~~~~~~~~~~~~

Sample polling will always return the latest sample value of each channel.
These values will be updated immediately when a new sample is measured.
It is therefore important to read the sample values quickly after they have been measured.
For this reason, a callback can be registered that allows the user program to attach to the new-sample interrupt (``NEW_SAMPLES_RUN_IRQ``) with minimum delay.
The callback will be called direclty from the interrupt service thread and should therefore be considered time critical.
Register a callback using the ``CMD_BOARD_NEW_SAMPLE_CALLBACK`` command (and if needed, supply a context via ``CMD_BOARD_NEW_SAMPLE_CALLBACK_CONTEXT``).
By setting the callback to ``NULL``, the new-sample interrupt is disabled.
These commands are described in the next section.


Advanced RTX Command ID Enumeration
-----------------------------------

There is a set of i32/i64 commands that is exclusively available when running the RTX version of the TRION API.
They are required for polling sample values from inside a callback method that gets called when new samples are available.

CMD_BOARD_ACT_SAMPLE_VALUE_COUNT
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{1.5cm}|p{2.5cm}|p{3.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index      | Number of registers      |               |
   +-----+------------+--------------------------+---------------+
   | Set | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+

Queries the number of sample value polling registers.
Each register is a 32 bit value that can be updated from the board memory using ``CMD_BOARD_ACT_SAMPLE_VALUE_POINTER``.
If the returned value is 0 or an error is returned, polling is not supported.

CMD_BOARD_ACT_SAMPLE_VALUE_POINTER
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{1.5cm}|p{2.5cm}|p{3.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index      | Pointer to registers     | i64 only      |
   +-----+------------+--------------------------+---------------+
   | Set | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+

Updates the registers of last sample values values from the specified TRION board in an internal API buffer and returns the pointer to the first int32 value.
The number of values in the buffer can be queried using ``CMD_BOARD_ACT_SAMPLE_VALUE_COUNT``.
Only values of used channels are updated from the board (registers corresponding to unused channels are set to 0).

Note that reading last sample values from the board is a time critical operation and requires a real-time operating system like RTX.
If the readout is too slow and the board already measures a new sample, old and new values may be mixed. 
Ideally, the ``CMD_BOARD_ACT_SAMPLE_VALUE_POINTER`` call is executed directly in the new-sample callback (called from the interrupt thread of the board notification).
It is possible to query all boards from the callback of a single board as all boards in a system are synchronized when they use the same sample rate.
Expect a duration of 2-3 µs per register update (each sample access requires its own 32bit PXI bus transfer).
It is therefore beneficial to disable channels that are not needed (set ``Used`` to ``false``).
For example, reading 8 AI + BoardCNT channel values from 16 boards requires about 300 µs (reading multiple boards in parallel does ususally not show a performance increase).
Thus, polling 16 boards at a sample rate of 1000 Hz (1000 µs cycle time) is easily possible.


CMD_BOARD_NEW_SAMPLE_CALLBACK
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{1.5cm}|p{2.5cm}|p{3.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+--------------------------------------+
   |     | Board      | Value                    | Remark                               |
   +=====+============+==========================+======================================+
   | Get | Index      | Pointer to callback-fn   | i64 only                             |
   +-----+------------+--------------------------+--------------------------------------+
   | Set | Index      | Pointer to callback-fn   | Enables interrupt if set to non-NULL |
   +-----+------------+--------------------------+--------------------------------------+

This command allows to set (or read back) a pointer to a function with the signature

.. code:: c

    void callback(int board_no, void* context);

The callback has two arguments: The ``board_no`` of the board that calls the callback and a user definable pointer to a context (see ``CMD_BOARD_NEW_SAMPLE_CALLBACK_CONTEXT``).

By default, the pointer to callback is NULL and the new-sample interrupt is not activated.
When the pointer to the callback is a non-NULL value, the new-sample notification interrupt is enabled on
the specified board and the callback is called every time a new sample is measured by the board.
Note that the callback is emitted from the interrupt service thread under RTX and any complex data processing should be defered to another thread.
The callback function is however well suited for polling data from the board (it is possible to poll data from all boards in a single callback call as long as they are synchronized).
Setting the callback function to NULL deactivates the new-sample interrupt.


CMD_BOARD_NEW_SAMPLE_CALLBACK_CONTEXT
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{1.5cm}|p{2.5cm}|p{3.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index      | void* pointer to context | Default: NULL |
   +-----+------------+--------------------------+---------------+
   | Set | Index      | void* pointer to context | i64 only      |
   +-----+------------+--------------------------+---------------+

Writes or reads a pointer to a memory location of the user (as ``void*``).
Use this memory as context when handling the new sample callback.
This pointer will be passed to the callback function set in ``CMD_BOARD_NEW_SAMPLE_CALLBACK``.
If not set, the default value of NULL will be used.
The API does not dereference this pointer in any way.


GPS Synchronization Extensions
------------------------------

When starting with GPS synchronization, a new method to determine the exact acquisition start UTC time has been added:
The unix timestamp (with subsecond precision) of the acquisition start is latched when the first sample is measured.
It can be read via the following command:

.. code:: c

    char acq_start[32];
    DeWeGetParamStruct_str("BoardId0/AcqProp/Timing/AcqStartTime", "UnixTimestamp", acq_start, sizeof(acq_start));

The value in ``acq_start`` will then contain the unix timestamp in UTC (e.g. ``"1716980535.21"``).
When starting with a PPS, the subsecond part will be 0 (e.g. ``1716980535.0``).
