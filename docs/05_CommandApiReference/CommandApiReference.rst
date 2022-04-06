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
    DeWeGetParam_i32(0, CMD_BUFFER_0_AVAIL_NO_SAMPLE, &samples_available);


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
analog cards during a running acquisition can rely on this basic interface.
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

Type: compositem, issues:

* CMD_UPDATE_PARAM_ACQ_SR
* CMD_UPDATE_PARAM_AO_PATTERN
* CMD_UPDATE_PARAM_CHN_ALL
* CMD_UPDATE_PARAM_ACQ_ALL

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



CMD_BUFFER_AVAIL_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_AVAL_NO_SAMPLE instead


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
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



CMD_BUFFER_0_AVAIL_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
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
   +=====+============+==========================+===============+
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
of data as reported back by CMD_BUFFER_0_AVAIL_NO_SAMPLE. It
may process less data (provided enough data is processed per loop, to
ensure, that no buffer overrun occurs).

If the application issues CMD_BUFFER_0_FREE_NO_SAMPLE command with smaller
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
   +=====+============+==========================+===============+
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
   +=====+============+==========================+===============+
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
   +=====+============+==========================+===============+
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


CMD_ACT_SAMPLE_COUNT
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index only | nr of acquired samples   |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This function retrieves the number of already sampled values on the given
board. This is a low latency function and allows for example for software
time stamping of asynchronous Non-TRION data sources. Asynchronous TRION
data channels are usually hardware time stamped as this is the more accurate
way for time stamping. This function is meant to be used in conjunction
with third party hardware that does not provide reliable timestamps on its
own.

Note: Not to be confused with CMD_BUFFER_0_AVAIL_NO_SAMPLE or
CMD_BUFFER_0_ACT_SAMPLE_POS. This function does not provide any information
regarding the already transferred samples. It cannot be used for any
conclusion regarding any sample buffer information.



CMD_UPDATE_PARAM_ACQ_ALL
~~~~~~~~~~~~~~~~~~~~~~~~

Type: composite, issues:

* CMD_UPDATE_PARAM_ACQ
* CMD_UPDATE_PARAM_MUX
* CMD_UPDATE_PARAM_INTSIG0
* CMD_UPDATE_PARAM_INTSIG1

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This function updates the hardware regarding the pure data-acquisition
parameters. This includes, but is not limited to setting up the DMA-
characteristics.

This is equivalent to call CMD_UPDATE_PARAM_ACQ, CMD_UPDATE_PARAM_MUX,
CMD_UPDATE_PARAM_INTSIG0 and CMD_UPDATE_PARAM_INTSIG1 in this order.
As this command potentially changes the DMA layout, it is not advised
to issue this command during a running acquisition.

During a running acquisition this command will return ERR_COMMAND_NOT_ALLOWED.



CMD_UPDATE_PARAM_CHN_ALL
~~~~~~~~~~~~~~~~~~~~~~~~

Type: composite, issues:

* CMD_UPDATE_PARAM_AI with parameter UPDATE_ALL_CHANNELS
* CMD_UPDATE_PARAM_AREF with parameter UPDATE_ALL_CHANNELS
* CMD_UPDATE_PARAM_CNT with parameter UPDATE_ALL_CHANNELS
* CMD_UPDATE_PARAM_DI with parameter UPDATE_ALL_CHANNELS
* CMD_UPDATE_PARAM_BOARD_CNT with parameter UPDATE_ALL_CHANNELS
* CMD_UPDATE_PARAM_CAN with parameter UPDATE_ALL_CHANNELS
* CMD_UPDATE_PARAM_UART with parameter UPDATE_ALL_CHANNELS

Usable during acquisition:  yes (but not recommended)


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command updates the hardware-settings of all channels featured on the
indexed board to reflect the latest logical settings.



CMD_UPDATE_PARAM_ACQ
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command updates the board hardware to reflect all acquisition
related logical settings.
This includes the signal-routing on the PXI-plane (eg for synchronization
purposes), configuring the hardware to the selected samplingrate and
preparing the DMA-ring-buffer.



CMD_UPDATE_PARAM_ACQ_ROUTE
~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes (but only recommended in limited cases)


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command updates the signal-route-multiplexer for the signals directly
relevant to the acquisition to their latest logical values.

In detail these are the settings:

• AcqClk
• AcqSync
• AcqStart

Usually those parameters are set prior to acquisition. A possible use case,
when to issue this command during a running acquisition would be, to route
AcqStart to High after an acquisition using external trigger has been started
successfully, to prevent the acquisition from being stopped, if the
external-trigger returns to low.



CMD_UPDATE_PARAM_MUX
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command applies the logical settings of the signal-line multiplexers
to the hardware. This includes the PXI-line settings like TRIG0-TRIG7,
the LBL and LBR lines, Start Trigger lines, etc. Usually this command is
invoked implicitly by calling one of the PARAM_UPDATE_ACQ commands. This
command is only useful, if an application needs to change MUX-settings during
a running acquisition and can be ignored otherwise.



CMD_UPDATE_PARAM_INTSIG0, CMD_UPDATE_PRAM_INTSIG1
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes (but with considerations)

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command updates the hardware settings for the IntSig0 and IntSig1
line to reflect the latest logical settings. It can safely be used during
a running acquisition.



CMD_UPDATE_PARAM_AI
~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | - Channel index          |               |
   |     |            | - UPDATE_ALL_CHANNELS    |               |
   |     |            | - UPDATE_GROUP_CHANNELS  |               |
   +-----+------------+--------------------------+---------------+


This command updates the analog amplifier chain to reflect the latest
logic values. It is safe to issue this command during a running acquisition,
unless the *Used* state of the channel in question has been changed.



CMD_UPDATE_PARAM_CNT
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | - Channel index          |               |
   |     |            | - UPDATE_ALL_CHANNELS    |               |
   |     |            | - UPDATE_GROUP_CHANNELS  |               |
   +-----+------------+--------------------------+---------------+


This command updates the counter properties on the FPGA to reflect the
latest logical configuration values. This includes the input-multiplexer
as well, as the filter settings and advanced counter parameters. As the
advanced counter parameters change the logical math used on the channel it
makes no sense, to make changes to those settings during a running
acquisition.



CMD_UPDATE_PARAM_DI
~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | - Channel index          |               |
   |     |            | - UPDATE_ALL_CHANNELS    |               |
   |     |            | - UPDATE_GROUP_CHANNELS  |               |
   +-----+------------+--------------------------+---------------+


This command updates the digital input settings on the board to reflect
the latest logical configuration values. As this is most likely to change the
scan-layout it is not possible to issue this command on a running acquisition.



CMD_UPDATE_PARAM_BOARD_CNT
~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: no

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | - Channel index          |               |
   |     |            | - UPDATE_ALL_CHANNELS    |               |
   |     |            | - UPDATE_GROUP_CHANNELS  |               |
   +-----+------------+--------------------------+---------------+


This command updates the board-counter properties on the FPGA to reflect
the latest logical configuration values. This includes the input-multiplexer
as well, as the filter settings and advanced counter parameters.
As the advanced counter parameters change the logical math used on the channel
it makes no sense, to make changes to those settings during a running
acquisition.



CMD_UPDATE_PARAM_CAN
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes (synchronous acquisition), no (CAN acquisition)

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | - Channel index          |               |
   |     |            | - UPDATE_ALL_CHANNELS    |               |
   |     |            | - UPDATE_GROUP_CHANNELS  |               |
   +-----+------------+--------------------------+---------------+


This command updates the CAN-controller settings to reflect the latest
logical configuration values. The CAN-acquisition has to be in a stopped
stated, to accept this command.



CMD_UPDATE_PARAM_UART
~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes (synchronous acquisition), no (UART acquisition)

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | - Channel index          |               |
   |     |            | - UPDATE_ALL_CHANNELS    |               |
   |     |            | - UPDATE_GROUP_CHANNELS  |               |
   +-----+------------+--------------------------+---------------+


This command updates the UART-controller settings to reflect the latest
logical configuration values. The UART-acquisition has to be in a stopped
stated, to accept this command.



CMD_ASYNC_POLLING_TIME
~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Status: deprecated and unsupported

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------------+---------------+
   |     | Board      | Value                          | Remark        |
   +=====+============+================================+===============+
   | Get | Index only | Currently set polling time [ms]|               |
   +-----+------------+--------------------------------+---------------+
   | Set | Index only | Polling time to set [ms]       |               |
   +-----+------------+--------------------------------+---------------+


*No longer implemented.*



CMD_ASYNC_FRAME_SIZE
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Status: deprecated and unsupported

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------------+---------------+
   |     | Board      | Value                          | Remark        |
   +=====+============+================================+===============+
   | Get | Index only | Frame size                     |               |
   +-----+------------+--------------------------------+---------------+
   | Set | Index only | Frame size                     |               |
   +-----+------------+--------------------------------+---------------+


*No longer implemented.*



CMD_UPDATE_PARAM_ACQ_TIMING
~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


Updates the hardware to reflect the currently set logical configuration
related to the various timing-modes (eg.: PTP, IRIG, GPS-SYNC, PPS-Sync).



CMD_TIMING_STATE
~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index only | Current timing state     |               |
   +-----+------------+--------------------------+---------------+
   | Set | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command queries the board for the current state of the FPGA-internal
state machine.

The possible reported values are:

• TIMINGSTATE_LOCKED
• TIMINGSTATE_NOTRESYNCED
• TIMINGSTATE_UNLOCKED
• TIMINGSTATE_LOCKEDOOR
• TIMINGSTATE_TIMEERROR
• TIMINGSTATE_RELOCKOOR
• TIMINGSTATE_NOTIMINGMODE

For a detailed description of those values please refer to the chapter
*Synchronization*.



CMD_BUFFER_CLEAR_ERROR
~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_CLEAR_ERROR instead

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command is used in more advanced error control of buffer overruns
during acquisition. When a buffer-overrun occurs, the driver will stay
in the overrun-error-occurred state, until either the acquisition is
stopped or the command CMD_BUFFER_CLEAR_ERROR is issued. The basic and
recommended error-handling for buffer-overruns is to stop the running
acquisition, and restart it.

For advanced applications this might be a
undesired error-handling-policy, especially in multi-board-systems. With
the stop-start approach even boards that are currently error-free would
be stopped (in a master-slave environment). With the command
CMD_BUFFER_CLEAR_ERROR it is possible to inform the driver that the
application did acknowledge the overrun-condition, and handled it, and
normal acquisition can occur from now on. For details about the usage
of this command, please refer to the related SDK example.

CMD_BUFFER_CLEAR_ERROR is deprecated. CMD_BUFFER_0_CLEAR_ERROR should be
used.



CMD_BUFFER_0_CLEAR_ERROR
~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command is used in more advanced error control of buffer overruns
during acquisition. When a buffer-overrun occurs, the driver will stay
in the overrun-error-occurred state, until either the acquisition is
stopped or the command CMD_BUFFER_0_CLEAR_ERROR is issued. The basic and
recommended error-handling for buffer-overruns is to stop the running
acquisition, and restart it.

For advanced applications this might be a
undesired error-handling-policy, especially in multi-board-systems. With
the stop-start approach even boards that are currently error-free would
be stopped (in a master-slave environment). With the command
CMD_BUFFER_0_CLEAR_ERROR it is possible to inform the driver that the
application did acknowledge the overrun-condition, and handled it, and
normal acquisition can occur from now on. For details about the usage
of this command, please refer to the related SDK example.



CMD_GET_UART_STATUS
~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index only | Current UART-FIFO state  |               |
   +-----+------------+--------------------------+---------------+
   | Set | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command retrieves the current FIFO-status of the on-board-UART
(UART capable boards only).

The possible reported values are:

* WARNING_UART_FIFO_BUSY
* WARNING_UART_FIFO_FULL
* WARNING_UART_FIFO_ERROR
* ERR_NONE



CMD_BUFFER_WAIT_AVAIL_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: deprecated, use CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLE instead

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index only | Nr of available samples  |               |
   +-----+------------+--------------------------+---------------+
   | Set | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command is the blocking sibling of CMD_BUFFER_AVAIL_NO_SAMPLE.
When issued, the API will block until a number of samples
have been acquired. This command frees the application of the need to
poll for available samples. The subsequent processing of acquired data
is the same, as it would be with CMD_BUFFER_AVAIL_NO_SAMPLE.

CMD_BUFFER_WAIT_AVAIL_NO_SAMPLE is deprecated.
CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLE should be used.



CMD_BUFFER_0_WAIT_AVAIL_NO_SAMPLE
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index only | Nr of available samples  |               |
   +-----+------------+--------------------------+---------------+
   | Set | N/A        | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command is the blocking sibling of CMD_BUFFER_0_AVAIL_NO_SAMPLE.
When issued, the API will block until the a number of samples
have been acquired. This command frees the application of the need to
poll for available samples. The subsequent processing of acquired data
is the same, as it would be with CMD_BUFFER_0_AVAIL_NO_SAMPLE.



CMD_ACQ_STATE
~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+---------------------------+---------------+
   |     | Board      | Value                     | Remark        |
   +=====+============+===========================+===============+
   | Get | Index only | Current acquisition state |               |
   +-----+------------+---------------------------+---------------+
   | Set | N/A        | N/A                       | Not supported |
   +-----+------------+---------------------------+---------------+


This command retrieves the current acquisition state of the board.

The possible reported values:

* ACQ_STATE_IDLE
* ACQ_STATE_RUNNING
* ACQ_STATE_SYNCED
* ACQ_STATE_ERROR

This way an application can always see, in what detailed state the
data acquisition hardware currently is.

This command is for example useful on external-trigger setups, as the
command CMD_START_ACQUISTION would just arm the given board, but as
long, as the external trigger did not happen, the board itself would
stay in ACQ_STATE_IDLE.



CMD_UPDATE_PARAM_AREF
~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+---------------------------+---------------+
   |     | Board      | Value                     | Remark        |
   +=====+============+===========================+===============+
   | Get | N/A        | Current acquisition state |               |
   +-----+------------+---------------------------+---------------+
   | Set | Index only | - Channel index           |               |
   |     |            | - UPDATE_ALL_CHANNELS     |               |
   |     |            | - UPDATE_GROUP_CHANNELS   |               |
   +-----+------------+---------------------------+---------------+


This command updates the current hardware-settings of the internal
analogue reference source to reflect the latest logical values.



CMD_TIMING_TIME
~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{5.0cm}|p{2.5cm}|

.. table::
   :widths: 20 20 50 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index only | nr of acquired samples   |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This low-latency command updates the logical data-elements below
/AcqProp/Timing/SystemTime to hold the current hardware-values.
Typical usage examples would be a GPS or IRIG time-source connected
to a IRIG or GPS capable TRION™-board.

After issuing this command the string-getters for the single
time-elements can be issued to obtain the information.



CMD_GPS_RECEIVER_RESET
~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

 .. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


This command issues the command-sequence, that will perform a low-level-reset
on the receiver of the GPScapable TRION™-board.



CMD_PXI_LINE_STATE
~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get | Index only | State of PXI-lines       |               |
   +-----+------------+--------------------------+---------------+
   | Set |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+


This command retrieves the current state of the PXI-Lines.
For decoding use the filter-bits PXI_LINE_STATE_TRIG0 to PXI_LINE_STATE_LBL12.



CMD_DISCRET_STATE_SET
~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | DI Channel index         |               |
   +-----+------------+--------------------------+---------------+


This command is used to set the given logical discreet (DIO/DO) channel to
a logic 1 value (= High). As only natural channel number can be passed,
only one DIO/DO channel (bit) can be set at the same time.



CMD_DISCRET_STATE_CLEAR
~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | DI Channel index         |               |
   +-----+------------+--------------------------+---------------+


This command is used to set the given logical discreet (DIO/DO) channel to
a logic 0 value (= Low). As only natural channel number can be passed,
only one DIO/DO channel (bit) can be set at the same time.



CMD_DISCRET_GROUP32_SET
~~~~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | Bit-pattern to set       |               |
   +-----+------------+--------------------------+---------------+


Discreet asynchronous output channels (DIO/DO) are organized in 32Bit groups
on TRION™. This command is used to update a whole 32-bit group at once.
This command by nature operates on a less hardware-abstract level than the
CMD_DISCRET_STATE_SET/CMD_DISCRET_STATE_CLEAR commands. Therefore an
application has to have a higher awareness about the detailed hardware
capabilities of the given board.

Note: the second group of 32-Discreets can be addressed by incrementing the
command ID by 1 (CMD_DISCRET_GROUP32_Set +1).



CMD_IDLED_BOARD_ON
~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | LED color                |               |
   +-----+------------+--------------------------+---------------+


Specific TRION™-boards feature a single LED that can be SW-controlled.
Currently valid colors to set the LED to are:

* IDLED_COL_RED
* IDLED_COL_GREEN
* IDLED_COL_ORANGE
* IDLED_COL_OFF

Whether this command is supported, and details about the allowed values
are depending on the specific TRION™-board.



CMD_IDLED_BOARD_OFF
~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


Specific TRION™-boards feature a single LED that can be SW-controlled.
This command will turn the LED of the given board off.
The command is equivalent to issue CMD_IDLED_BOARD_ON with the parameter
IDLED_COL_OFF.



CMD_IDLED_CHANEL_ON
~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: not implemented

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | LED color                |               |
   +-----+------------+--------------------------+---------------+


Command that will set a LED that is located near a specific channel
connector.

This command is currently not supported, as no board features these
LEDs.



CMD_IDLED_CHANEL_OFF
~~~~~~~~~~~~~~~~~~~~

Type: atomic

Usable during acquisition: yes

Status: not implemented

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+


Command that will set a LED that is located near a specific channel
connector.

This command is currently not supported, as no board features these
LEDs.



CMD_BOARD_ADC_DELAY
~~~~~~~~~~~~~~~~~~~
Type: atomic

Usable during acquisition: yes

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+

This command retrieves the ADC delay, using the current settings.
AD converter have different runtime of the samples
from analog side to the final converted value. Analog
samples may appear in later scans, than time-wise corresponding
digital channels. The ADC-delay is used to allow the application
to re-associate samples of different channel-types.

A more detailed description can be found in th chapter
:ref:`ADC Delay <data_acq_adc_delay>`

Please refer to the SDK-example “ADCDelay” for correct usage.



CMD_BOARD_BASEEEPROM_WRITE
~~~~~~~~~~~~~~~~~~~~~~~~~~
Type: atomic

Usable during acquisition: yes (but not recommended)

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+

This command writes the content of the base-e2rpom-xml-file back to
the e2prom. Unless the application willingly writes directly to the
baseeeprom-xml-file, there is no need to issue this command.



CMD_BOARD_CONEEPROM_WRITE
~~~~~~~~~~~~~~~~~~~~~~~~~
Type: atomic

Usable during acquisition: yes (but not recommended)

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table::
   :widths: 20 20 20 20

   +-----+------------+--------------------------+---------------+
   |     | Board      | Value                    | Remark        |
   +=====+============+==========================+===============+
   | Get |  N/A       | N/A                      | Not supported |
   +-----+------------+--------------------------+---------------+
   | Set | Index only | N/A                      |               |
   +-----+------------+--------------------------+---------------+

This command writes the content of the connector-e2rpom-xml-file back
to the e2prom. Unless the application willingly writes directly to the
coneeprom-xml-file, there is no need to issue this command.
