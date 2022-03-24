Command Reference
=================


This section gives a description of available command IDs for the integer
based API functions.


There are two distinct integer based commands:

- One to set data, or trigger an action
- One to retrieve data



Expert clarification
--------------------

Another distinction is whether it is an atomic command or a composite
command.
Composite commands are a collection of atomic commands executed within
the context of the API. Hardwareupdates in general are more time-consuming,
as they involve hardware-access, and board-specific guard-times between
those accesses, determined by the components used on the board itself.
Composite commands allow the API to perform order-optimization, and thereby
cut the time spent in the actual function, compared to using the
atomic-commands on application level.

When invoking a composite command, API will try to perform the underlying
atomic commands with a best-effortpolicy. So every atomic command will be
tried to execute, even if any of the prior executed atomic operation
returned an error. (Except if there are internal hard-dependencies, which
would render an operation as potentially harmful to the hardware).

As the i32-invokers use a single integer-return-value for the error-code,
the return-value of the composite commands will always reflect the *worst*
error-code encountered during the execution. Therefore it is possible to
lose detailed information, as it is possible that more than one of the
underlying atomic commands would indicate an error-state.


DeWeGetParam_i32
----------------

.. code:: c

    int DeWeGetParam_i32( [in] int board, [in] int command, [out] int* val);

DeWeGetParam_i32 is used to retrieve a value (or result) for a specific defined
command. A board is accessed by an integer index >= 0.
The second parameter command must be a valid command from the file
*dewexpi_commands.inc*
The result is stored to the variable referenced by the pointer val.

Example:

.. code:: c

    int samples_available;
    DeWeGetParam_i32(0, CMD_BUFFER_AVAIL_NO_SAMPLE, &samples_available);


Parameters:

* board: Index of the board the command has to be executed on. Starts at 0.
* command: The id of the get command to be executed.
* val: A pointer to an 32 bit integer variable where the result is stored.

Return codes:

* 0: A return code of 0 (ERR_NONE), indicates execution without failure. The content of val is reliable.
* <0: A negative return code indicates a warning. The value returned in pVal can be taken as reliable.
* >0: A positive return code indicates an error. The value returned in val is not reliable, and should not be considered for further usage.


DeWeSetParam_i32
----------------

.. code:: c

    int DeWeSetParam_i32( [in] int board, [in] int command, [in] int val);

DeWeSetParam_i32 is used to set a value or trigger a command.
A board is accessed by an integer index >= 0.
The second parameter command must be a valid command from the file
*dewexpi_commands.inc*
For some commands val is optional and can be 0.

Example:

.. code:: c

    DeWeSetParam_i32(0, CMD_BUFFER_0_FREE_NO_SAMPLE, 100);


Parameters:

* board: Index of the board the command has to be executed on. Starts at 0.
* command: The id of the get command to be executed.
* val: A pointer to an 64 bit integer variable where the result is stored.

Return codes:

* 0: A return code of 0 (ERR_NONE), indicates execution without failure. The content of val is reliable.
* <0: A negative return code indicates a warning. The value returned in pVal can be taken as reliable.
* >0: A positive return code indicates an error. The value returned in val is not reliable, and should not be considered for further usage.


DeWeGetParam_i64
----------------

.. code:: c

    int DeWeGetParam_i64( [in] int board, [in] int command, [out] sint64* val);

DeWeGetParam_i64 is used to retrieve a value (or result) for a specific defined
command. A board is accessed by an integer index >= 0.
The second parameter command must be a valid command from the file
*dewexpi_commands.inc*
The result is stored to the 64bit variable referenced by the pointer val.

DeWeGetParam_i64 was added to the API to get be able to get valid buffer
pointer on 64bit systems.

Example:

.. code:: c

    sint64 read_pos;
    DeWeGetParam_i64(0, CMD_BUFFER_0_ACT_SAMPLE_POS, &read_pos);


Parameters:

* board: Index of the board the command has to be executed on. Starts at 0.
* command: The id of the get command to be executed.
* val: A pointer to an 64 bit integer variable where the result is stored.

Return codes:

* 0: A return code of 0 (ERR_NONE), indicates execution without failure. The content of val is reliable.
* <0: A negative return code indicates a warning. The value returned in pVal can be taken as reliable.
* >0: A positive return code indicates an error. The value returned in val is not reliable, and should not be considered for further usage.


DeWeSetParam_i64
----------------

.. code:: c

    int DeWeSetParam_i64( [in] int board, [in] int command, [in] sint64 val);

DeWeSetParam_i32 is used to set a value or trigger a command.
A board is accessed by an integer index >= 0.
The second parameter command must be a valid command from the file
*dewexpi_commands.inc*
For some commands val is optional and can be 0.

DeWeGetParam_i64 was added to the API to get be able to get valid buffer
pointer on 64bit systems. DeWeSetParam_i64 is the datatype compatible set
function.

Example:

.. code:: c

    sint64 read_pos;
    DeWeSetParam_i64(0, CMD_BUFFER_1_CLEAR_ERROR, 0);


Parameters:

* board: Index of the board the command has to be executed on. Starts at 0.
* command: The id of the get command to be executed.
* val: A pointer to an 64 bit integer variable where the result is stored.

Return codes:

* 0: A return code of 0 (ERR_NONE), indicates execution without failure. The content of val is reliable.
* <0: A negative return code indicates a warning. The value returned in pVal can be taken as reliable.
* >0: A positive return code indicates an error. The value returned in val is not reliable, and should not be considered for further usage.



.. note:: DeWeSetParam_i32 and DeWeSetParam_i64 are compatible and the
    same commands can be used. To get a pointer always use DeWeSetParam_i64 to be save


.. note:: DeWeGetParam_i32 and DeWeGetParam_i64 are compatible and the
    same commands can be used.



Command ID Enumeration
----------------------

This section covers the basic set of i32-commands to be able to
perform data-acquisition with via the API with a TRION™-Board.
Most SDK Examples use this basic set.
Any application that does not intend to change amplifier-settings on
analogue cards during a running acquisition can rely on this basic interface.
The main difference between this basic interface and the advanced
interface-functions is the level of granularity of the commands.


CMD_OPEN_BOARD
~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------+---------------+
   |     | Board      | Value  | Remark        |
   +=====+============+========+===============+
   | Get | N/A        | N/A    | Not supported |
   +-----+------------+--------+---------------+
   | Set | Index only | unused |               |
   +-----+------------+--------+---------------+


Opens a board. This automatically configures the board to its
default state.



CMD_CLOSE_BOARD
~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------+---------------+
   |     | Board      | Value  | Remark        |
   +=====+============+========+===============+
   | Get | N/A        | N/A    | Not supported |
   +-----+------------+--------+---------------+
   | Set | Index only | unused |               |
   +-----+------------+--------+---------------+


Closes a board. If forgotten, DeWeDriverDeInit closes
all open boards.



CMD_RESET_BOARD
~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------+---------------+
   |     | Board      | Value  | Remark        |
   +=====+============+========+===============+
   | Get | N/A        | N/A    | Not supported |
   +-----+------------+--------+---------------+
   | Set | Index only | unused |               |
   +-----+------------+--------+---------------+


The default setting is applied to the board on the
initial CMD_OPEN_BOARD. CMD_RESET_BOARD reflects
these settings to the hardware of the given board, and therefore
acts like CMD_UPDATE_PARAM_ALL. To clear out the ADC-pipes, a short
(around 100 ms) measurement is started during this operation.



CMD_START_ACQUISITION
~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-----------+---------------+
   |     | Board      | Value     | Remark        |
   +=====+============+===========+===============+
   | Get | Index only | ACQ State |               |
   +-----+------------+-----------+---------------+
   | Set | Index only | unused    |               |
   +-----+------------+-----------+---------------+


This command starts the acquisition on the indexed board. To allow
this command to actually execute and start acquisition on the hardware,
it is mandatory, that the hardware setup (both, on the logical layer
and on the hardware) has been executed and finished.

Get is aliased by CMD_ACQ_STATE. Should not be used directly, as
support for the not-aliased command may be dropped in
upcoming versions.



CMD_STOP_ACQUISITION
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-----------+---------------+
   |     | Board      | Value     | Remark        |
   +=====+============+===========+===============+
   | Get |  N/A       | N/A       | Not supported |
   +-----+------------+-----------+---------------+
   | Set | Index only | unused    |               |
   +-----+------------+-----------+---------------+


This command stops the acquisition for the given board.



CMD_BUFFER_BLOCK_SIZE
~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no

Status: deprecated, use CMD_BUFFER_0_BLOCK_SIZE instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+------------+---------------+
   |     | Board      | Value      | Remark        |
   +=====+============+============+===============+
   | Get | Index only | block size |               |
   +-----+------------+------------+---------------+
   | Set | Index only | block size |               |
   +-----+------------+------------+---------------+


This command queries or sets the block size to be used during acquisition.
Please refer to the chapter “Data Acquisition” for details.

CMD_BUFFER_BLOCK_SIZE is deprecated. CMD_BUFFER_0_BLOCK_SIZE should be
used.


CMD_BUFFER_0_BLOCK_SIZE
~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+------------+---------------+
   |     | Board      | Value      | Remark        |
   +=====+============+============+===============+
   | Get | Index only | block size |               |
   +-----+------------+------------+---------------+
   | Set | Index only | block size |               |
   +-----+------------+------------+---------------+


This command queries or sets the block size to be used during acquisition.
Please refer to the chapter “Data Acquisition” for details. This configures
the block size of buffer 0.


CMD_BUFFER_BLOCK_COUNT
~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no

Status: deprecated, use CMD_BUFFER_0_BLOCK_SIZE instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | block count |               |
   +-----+------------+-------------+---------------+
   | Set | Index only | block count |               |
   +-----+------------+-------------+---------------+


This command queries or sets the block count to be used during acquisition.
Please refer to the chapter “Data Acquisition” for details.

CMD_BUFFER_BLOCK_COUNT is deprecated. CMD_BUFFER_0_BLOCK_COUNT should be
used.


CMD_BUFFER_0_BLOCK_COUNT
~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | block count |               |
   +-----+------------+-------------+---------------+
   | Set | Index only | block count |               |
   +-----+------------+-------------+---------------+


This command queries or sets the block count to be used during acquisition.
Please refer to the chapter “Data Acquisition” for details. This configures
the block count of buffer 0.



CMD_UPDATE_PARAM_ALL
~~~~~~~~~~~~~~~~~~~~

Type: composite

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get |  N/A       | N/A         | Not supported |
   +-----+------------+-------------+---------------+
   | Set | Index only | N/A         |               |
   +-----+------------+-------------+---------------+


This function updates the whole hardware to match the logical settings.
It allocates the sample ring buffer according to the specified parameters
and inherent parameters like samplerate, number of used channels and channel
data width.

This is equivalent to calling:

* CMD_UPDATE_PARAM_ACQ_SR
* CMD_UPDATE_PARAM_AO_PATTERN
* CMD_UPDATE_PARAM_CHN_ALL
* CMD_UPDATE_PARAM_ACQ_ALL

During setup this is typically the last set-command issued to the driver
before starting the acquisition. Typically after this command only
get-commands are applied to the driver. An exception to this rule would be
the command CMD_BUFFER_FREE_NO_SAMPLE as this is part of a typical
data-readout-loop and is used as a set-command. As this command potentially
changes the DMA layout, it is not advised to issue this command during a
running acquisition.

During a running acquisition this command will return ERR_COMMAND_NOT_ALLOWED.
But as this command is a composite command, updating all channels, the
trigger-line-MUX and the acquisition parameters parts of changed configuration
will take effect, using an best-effort-policy.

Expert tip: Do not call it during a running acquisition.



CMD_BUFFER_START_POINTER
~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_START_POINTER instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | buffer start|               |
   +-----+------------+-------------+---------------+
   | Set |  N/A       | N/A         | Not supported |
   +-----+------------+-------------+---------------+


This command retrieves the start pointer of the sample ring buffer.
This value can be queried after applying all necessary parameters
for acquisition setup (for example by calling CMD_UPDATE_PARAM_ALL).

CMD_BUFFER_START_POINTER is deprecated. CMD_BUFFER_0_START_POINTER should be
used.


CMD_BUFFER_0_START_POINTER
~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | buffer start|               |
   +-----+------------+-------------+---------------+
   | Set |  N/A       | N/A         | Not supported |
   +-----+------------+-------------+---------------+


This command retrieves the start pointer of the sample ring buffer 0.
This value can be queried after applying all necessary parameters
for acquisition setup (for example by calling CMD_UPDATE_PARAM_ALL).



CMD_BUFFER_END_POINTER
~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_END_POINTER instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | buffer start|               |
   +-----+------------+-------------+---------------+
   | Set |  N/A       | N/A         | Not supported |
   +-----+------------+-------------+---------------+


This command retrieves the start pointer of the sample ring buffer.
This value can be queried after applying all necessary parameters
for acquisition setup (for example by calling CMD_UPDATE_PARAM_ALL).

CMD_BUFFER_END_POINTER is deprecated. CMD_BUFFER_0_END_POINTER should be
used.



CMD_BUFFER_0_END_POINTER
~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | buffer start|               |
   +-----+------------+-------------+---------------+
   | Set |  N/A       | N/A         | Not supported |
   +-----+------------+-------------+---------------+


This command retrieves the start pointer of the sample ring buffer 0.
This value can be queried after applying all necessary parameters
for acquisition setup (for example by calling CMD_UPDATE_PARAM_ALL).



CMD_BUFFER_TOTAL_MEM_SIZE
~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_TOTAL_MEM_SIZE instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | mem size    |               |
   +-----+------------+-------------+---------------+
   | Set |  N/A       | N/A         | Not supported |
   +-----+------------+-------------+---------------+

This command retrieves the total size of the allocated ring buffer in bytes.
A typical usage for this information is the wrap around handling when
reading out the sample ring buffer.

This value can be queried after applying all necessary parameters
for acquisition setup (for example by calling CMD_UPDATE_PARAM_ALL).

CMD_BUFFER_TOTAL_MEM_SIZE is deprecated. CMD_BUFFER_0_TOTAL_MEM_SIZE should be
used.



CMD_BUFFER_0_TOTAL_MEM_SIZE
~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+-------------+---------------+
   |     | Board      | Value       | Remark        |
   +=====+============+=============+===============+
   | Get | Index only | mem size    |               |
   +-----+------------+-------------+---------------+
   | Set |  N/A       | N/A         | Not supported |
   +-----+------------+-------------+---------------+


This command retrieves the total size of the allocated ring buffer in bytes.
A typical usage for this information is the wrap around handling when
reading out the sample ring buffer.

This value can be queried after applying all necessary parameters
for acquisition setup (for example by calling CMD_UPDATE_PARAM_ALL).



CMD_BUFFER_AVAL_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_AVAL_NO_SAMPLE instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================================+
   | Get | Index only | nr of available samples  |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command retrieves the number of unprocessed scans within the ring buffer.
This command is non-blocking so \*val may have the value of 0 if there are no
samples available yet.

This function will indicate if a buffer-overflow in the ring buffer has
occurred by returning ERR_BUFFER_OVERWRITE. This can happen if the actual
data processing is too slow. To clear this error the acquisition should
be stopped on all boards and restarted. Another method is to call
CMD_BUFFER_0_CLEAR_ERROR.

This value can be queried at any time during a running acquisition. Calling
this command on a stopped boardwill result in an error code indicating that
no acquisition is running (ERR_DAQ_NOT_STARTED).

CMD_BUFFER_AVAL_NO_SAMPLE is deprecated. CMD_BUFFER_0_AVAL_NO_SAMPLE should be
used.



CMD_BUFFER_0_AVAL_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================================+
   | Get | Index only | nr of available samples  |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command retrieves the number of unprocessed scans within the ring buffer.
This command is non-blocking so \*val may have the value of 0 if there are no
samples available yet.

This function will indicate if a buffer-overflow in the ring buffer has
occurred by returning ERR_BUFFER_OVERWRITE. This can happen if the actual
data processing is too slow. To clear this error the acquisition should
be stopped on all boards and restarted. Another method is to call
CMD_BUFFER_0_CLEAR_ERROR.

This value can be queried at any time during a running acquisition. Calling
this command on a stopped boardwill result in an error code indicating that
no acquisition is running (ERR_DAQ_NOT_STARTED).




CMD_BUFFER_FREE_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_FREE_NO_SAMPLE instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================================+
   | Get | Index only | nr of samples to free    |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command indicates to the driver that a numbers of scans have been
processed by the application and the ring buffer can be freed.

In a typical data-readout-loop the sequence would be:

- CMD_BUFFER_0_AVAIL_NO_SAMPLE returning \*val = x
- CMD_BUFFER_0_ACT_SAMPLE_POS
- Actual data processing of y scans
- CMD_BUFFER_0_FREE_NO_SAMPLE setting val = y

The application does not have necessarily to process the same amount
of data as reported back by CMD_BUFFER_AVAIL_NO_SAMPLE. It
may process less data (provided enough data is processed per loop, to
ensure, that no buffer overrun occurs).

If the application issues CMD_BUFFER_FREE_NO_SAMPLE command with smaller
values than samples available, the un-freed samples will be reported again
at the next CMD_BUFFER_0_AVAIL_NO_SAMPLE call.

CMD_BUFFER_FREE_NO_SAMPLE is deprecated. CMD_BUFFER_0_FREE_NO_SAMPLE should be
used.



CMD_BUFFER_0_FREE_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================================+
   | Get | Index only | nr of samples to free    |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command indicates to the driver that a numbers of scans have been
processed by the application and the ring buffer can be freed.

In a typical data-readout-loop the sequence would be:

- CMD_BUFFER_0_AVAIL_NO_SAMPLE returning \*val = x
- CMD_BUFFER_0_ACT_SAMPLE_POS
- Actual data processing of y scans
- CMD_BUFFER_0_FREE_NO_SAMPLE setting val = y

The application does not have necessarily to process the same amount
of data as reported back by CMD_BUFFER_AVAIL_NO_SAMPLE. It
may process less data (provided enough data is processed per loop, to
ensure, that no buffer overrun occurs).

If the application issues CMD_BUFFER_FREE_NO_SAMPLE command with smaller
values than samples available, the un-freed samples will be reported again
at the next CMD_BUFFER_0_AVAIL_NO_SAMPLE call.



CMD_BUFFER_ACT_SAMPLE_POS
~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_ACT_SAMPLE_POS instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================================+
   | Get | Index only | pointer to samples       |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+

This command retrieves the address of the start of the first unprocessed scan.
This value can be queried at any time during a running acquisition.
Calling this command on a stopped board will result in an error code
indicating that no acquisition is running (ERR_DAQ_NOT_STARTED).

CMD_BUFFER_ACT_SAMPLE_POS is deprecated. CMD_BUFFER_0_ACT_SAMPLE_POS should be
used.



CMD_BUFFER_0_ACT_SAMPLE_POS
~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================================+
   | Get | Index only | pointer to samples       |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+

This command retrieves the address of the start of the first unprocessed scan.
This value can be queried at any time during a running acquisition.
Calling this command on a stopped board will result in an error code
indicating that no acquisition is running (ERR_DAQ_NOT_STARTED).



Advanced Command ID Enumeration
-------------------------------



