Data Acquisition
================




Buffer
------

The sample of all enabled channel on a TRION module are accessible via a ringbuffer.
These buffers are managed by the TRION API or the driver, and exposed to the application
via pointer information.
Synchronous data and asynchronous data are kept in seperate ringbuffers.

.. warning:: The actual layout of data within the buffer is specific to
    to each model-type of TRION, and can change in future with changes to
    the firmare.
    The rules regarding how different data-widths are handled, and how data
    is padded for dma-transfer are complex, module specific and subject to
    change.
    Therefore the only safe way to navigate the binary stream is by utilizing
    the runtinme information provied by the API .
    API provides a dynamically generated xml document for each
    board to describe the data-layout. Please refer to the chapter `Scan Descriptor`_
    for detailed information.


Buffer for synchronous data
---------------------------

Synchronous data uses the ringbuffer "BUFFER0".
This buffer uses the i32/i64 commands, prefixed with CMD_BUFFER_0.
(eg CMD_BUFFER_0_START_POINTER, BUFFER_0_END_POINTER, ...)

.. figure:: _img/acquisition_circular_buffer.svg
    :align: center

    Acquisition circular buffer

Terminology
~~~~~~~~~~~

Scan and Scan Size
^^^^^^^^^^^^^^^^^^

One scan is the portion of data that consists of exactly one sample for
each sampled channel on a board.

.. note::
    For modules, that allow for using channel-based sample-rate dividers
    the term scan is ambiguous, as one "super scan" holds 1 sample of
    the "slowest" channel(s), but multiple of the faster ones.

So if there are 2 analog channels and 1 counter channel active, one
scan would logically hold three values. (AI0, AI1, CNT0).

The scan-size therefore directly derives from this information. It
describes the memory-consumption of one scan in Bytes. In above example,
when using the AI-channels in 24Bit mode (consuming 32Bit per Sample)
the resulting scan size would be:

ScanSize := sizeof(AI0) + sizeof(AI1) + sizeof(CNT0)

ScanSize := 32Bit + 32Bit + 32Bit

ScanSize := 4 Byte + 4 Byte + 4 Byte

ScanSize := 12 Byte

So one scan would have the size of 12 Byte.

The scan size cannot be directly controlled by the application as it
directly depends on the number and type of activated channels.

Usually the application does not have to know very detailed about one
scan and its layout inherently, as there are ways to get this
information from the API in an abstracted way at runtime.



Block and Block Size
^^^^^^^^^^^^^^^^^^^^

One block is a collection of *n* scans.

The blocksize determines the granularity of DMA-transactions for the
datatransfer from the on-board-memory of the TRION-modules into the RAM of
the PC.

The block-size can be set to any arbitrary value > 0. A standard use
case would set it to SampleRate \* pollingInterval. For Example:

BlockSize := SampleRate \* pollingInterval

BlockSize := 2000 SPS \* 0.1 sec

BlockSize := 200

This has to be set by the application.


Block Count
^^^^^^^^^^^

This defines how many blocks the buffer shall be able to hold. This
allows the application to control how big the backlog of data shall be
and thus how much time the application may spend with tasks not related
to the acquisition - so that peaks in computation times won't lead to
lost acquisition data.

It can be set to any value > 0, and is only limited by the total
available memory.

For example:

BlockCount := 50

This has to be setup by the application.


Total Buffer Size
~~~~~~~~~~~~~~~~~

The total buffer size is calculated based on the above described
information.

BufferSize := ScanSize \* BlockSize \* BlockCount

In our example:

BufferSize := 12 Bytes \* 200 \* 50

BufferSize := 12 Bytes \* 10000

BufferSize := 12000 Bytes


Synchronous Data Channels
-------------------------

Each sampling period produces one sample for each channel and consumes
“Scan Size” amount of data in the buffer. There are currently three
kinds of synchronous data in the buffer: analog channel samples, counter
channel samples and digital channel samples.

The driver itself maintains a separate read- and write-pointer into this
buffer. So the hardware can add new samples independent of the
applications data-processing.

The driver will notify the application with an error-code if a
buffer-overrun occurs. That is, if the application processes data too
slow, so that the new samples have already overwritten unprocessed old
ones.

The application then can freely decide how to handle this error case.


Buffer Setup and Buffer Ownership
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The buffer itself is completely maintained inside the API - so the
applications do not have to bother with allocation and de-allocation
issues, which usually come with having a buffer.

However - to allow the application a fine granulated control over the
buffer, it is able and obligated to indicate to the API the desired size
of the buffer in terms of logical units, by using the integer-based
functions. The application decides, how many scans one block shall hold,
and how many blocks shall be allocated. The actual size in bytes is then
calculated by the API and the buffer is allocated.



Buffer Readout from Application Point of View
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The ring-buffer is exposed to the application by providing the related
pointer information.

The API will provide:

-  Start-pointer of the ring buffer
-  End-Pointer of the ring buffer
-  Pointer to the first unprocessed scan

Together with the information how many unprocessed samples are available
the application iterates directly over the ring buffer.

This approach allows a minimal internal overhead on data-access.



.. _data_aquisition_scan_descriptor:

Scan Descriptor
---------------

As mentioned before a scan is the portion of data
containing exactly one sample per used channel. Without knowledge about
its internal layout, this would just be a binary stream with arbitrary
length.

But the application does not need to know implicitly about the layout of
the data. This would be undesirable, as the layout may change with
coming driver versions or coming hardware. For example, when a new type
of synchronous data will be added, inherent hardcoded knowledge within
the application would immediately break the data-readout mechanism of
the application.

So after setting up the acquisition environment, the API can be queried
about the layout.

The detailed layout-information will be returned as an XML-string.

.. code-block:: XML
    :caption: BoardProperties - ScanDescriptor Example

    <ScanDescriptor>
        <BoardId0>
            <ScanDescription version="3" buffer = "BUFFER0" buffer_direction = "from_trion_board" scan_size="96" byte_order="little_endian" unit="bit">
                <Channel type="Analog" index="3" name="AI3">
                    <Sample offset="32" size="24" />
                </Channel>
            </ScanDescriptor>
        </BoardId0>
    </ScanDescriptor>



Scan Descriptor Structure
~~~~~~~~~~~~~~~~~~~~~~~~~

The following API string command returns the scan information for a
specific Board:

.. code:: c

    DeWeGetParamStruct_str( "BoardId0", "ScanDescriptor_V3", Buf, sizeof(Buf));


The returned XML document correlates with the following hierarchy:

#. <ScanDescriptor> : XML Element. Max. Occurrences: 1.
#. <BoardID0> : XML Element. Max. Occurrences: 1.
#. <ScanDescription> : XML Element. Max. Occurrences: 1.
#. <Channel> : XML Element. Max. Occurrences: Unbounded.
#. <Sample> : XML Element. Max. Occurrences: Unbounded.

Please be aware that the scan descriptor annotates only the enabled
channels for a specific Board. In case no channel is enabled, the API
returns an empty scan descriptor with “scan_size” set to the value 0.

The API considers disabled channels and therefore the returned
“scan_size” and “offsets” are being returned accordingly.

The following list depicts all possible XML Elements and their XML
attributes and values of the returned scan descriptor XML document:



.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{9cm}|

.. table:: TEDS XML description
   :widths: 10 10 80

   +-------------------+--------------------------+------------------------------------------------------+
   | **Element**       | **Attribute**            | **Description**                                      |
   +===================+==========================+======================================================+
   | ScanDescriptor    |                          | ScanDescriptor root element                          |
   +-------------------+--------------------------+------------------------------------------------------+
   | BoardIdXX         |                          | Selected board element “BoardID0”                    |
   +-------------------+--------------------------+------------------------------------------------------+
   | ScanDescription   |                          | Describes the scan for the requested board           |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | version                  | Scan descriptor's document version (Should be 3)     |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | scan_size                | The size of the scan expressed in unit               |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | byte_order               | The byte order of the scan ("little_endian")         |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | unit                     | The unit of “scan_size” attribute ("bit")            |
   +-------------------+--------------------------+------------------------------------------------------+
   | Channel           |                          | Channel element                                      |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | type                     | Value: string "Analog", "Counter", "Discrete"        |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | index                    | The channel index on the specific board              |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | name                     | API name of the channel "AI0"                        |
   +-------------------+--------------------------+------------------------------------------------------+
   | Sample            |                          | Detailed sample description                          |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | offset                   | The offset within the whole scan                     |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | size                     | the size of the sample                               |
   +-------------------+--------------------------+------------------------------------------------------+
   |                   | subChannel               | Optional attribute for counter sub channels          |
   +-------------------+--------------------------+------------------------------------------------------+


.. warning::
    When requesting a scan descriptor with command “ScanDescriptor” (Version
    1), some boards may not be able to return a valid scan descriptor
    for analog 24bit channels.
    When requesting a scan descriptor with command “ScanDescriptor_V2” (Version
    2), some boards may not be able to return a valid scan descriptor
    when used with channel-samplerate-dividers.
    Therefore, always use “ScanDescriptor_V3”.


.. warning::
    "ScanDescriptor" version 1 and 2 are deprecated and will be removed. They will return
    the V3 document in the future.


Scan Descriptor Example Source Code
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The next example extends the Quickstart app with a valid Scan Descriptor
support class.


.. literalinclude:: ../../trion/CXX/quickstart/quickstart_acq_scan_desc.cpp
    :caption: Scan Descriptor example
    :language: c++
    :linenos:
    :lines: 9-


.. _data_acq_adc_delay:

ADC Delay
---------

AD converters have a conversion time. Analog samples may
appear in later scans than time-wise corresponding digital channels. The
ADCdelay is used to allow the application to align samples of analog and
digital channel types.


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|p{2.5cm}|

.. table:: ADC delay effect on samples (for ADCDelay = 4)
   :widths: 20 20 20 20 20

   +----------------+----------+----------+----------+----------+
   | **Sample Nr**  | **AI0**  | **AI1**  | **CNT0** | **DI0**  |
   +================+==========+==========+==========+==========+
   | 0              | invalid  | invalid  | 0        | 0        |
   +----------------+----------+----------+----------+----------+
   | 1              | invalid  | invalid  | 1        | 1        |
   +----------------+----------+----------+----------+----------+
   | 2              | invalid  | invalid  | 2        | 2        |
   +----------------+----------+----------+----------+----------+
   | 3              | invalid  | invalid  | 3        | 3        |
   +----------------+----------+----------+----------+----------+
   | 4              | 0        | 0        | 4        | 4        |
   +----------------+----------+----------+----------+----------+
   | 5              | 1        | 1        | 5        | 5        |
   +----------------+----------+----------+----------+----------+
   | 6              | 2        | 2        | 6        | 6        |
   +----------------+----------+----------+----------+----------+
   | 7              | 3        | 3        | 7        | 7        |
   +----------------+----------+----------+----------+----------+


After acquisition start the samples from index 0 to 3 (== ADCDelay) are
marked as invalid. There will be values, but because of AD conversion
time they will be more or less randomized.

The value of ADCDelay is board dependent and can be requested with
CMD_BOARD_ADC_DELAY.

.. note::
    Please have look at the example “ADCDelay” showing a way for applying the
    ADC delay to realign AI samples to the other channels.



Sample Rate
-----------


Synchronous Acquisition
-----------------------





Asynchronous Acquisition
------------------------

.. CAN
.. CANFD
.. UART




Data Output
-----------

.. Analog
.. Digital
.. CAN(FD)-OUT
.. UART-OUT
