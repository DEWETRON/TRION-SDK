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
If the returned value is 0, polling is not supported.

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
Ideally, the ``CMD_BOARD_ACT_SAMPLE_VALUE_POINTER`` call is executed direclty in the new-sample callback (called in the interrupt thread of the board notification).
It is possible to query all boards from the callback of a single board as all boards in a system are synchronized when they use the same sample rate.
Expect a time of 2-3 µs per register update of an used channel as each update requires its own 32bit PXI bus transfer.
For example, reading 8 AI + BoardCNT channel values from 16 boards requires about 300 µs (reading multiple boards in parallel does ususally not show a performance increase).
Thus, operating 16 boards at a sample rate of 1000 Hz (1000 µs cycle time) is easily possible.


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
