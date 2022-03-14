Synchronization
===============

Multiple TRION (DEWE2, DEWE3) enclosures can be synchronized using various synchronization methods.
Supported methods are:

* TRION-SYNC-BUS
* PTP IEEE1588
* GPS
* IRIG A/B


TRION-SYNC-BUS is part of all enclosures. All other options need dedicated enclosures or TRION-TIMING boards in 
the enclosures first slot.


TRION-SYNC-BUS
--------------

Using TRION-SYNC-BUS needs special setup for two different enclosure roles. There has to be one MASTER 
instrument and one or more SLAVE instruments.


Cabling
~~~~~~~

Connect the SYNC cable to the sync-out plug at the master instrument to the sync-in plug of the first slave 
instrument. For further slave instruments follow the pattern and connect the slave’s sync-out plug to the next 
slaves’ sync-in plug.


Master instrument
~~~~~~~~~~~~~~~~~

The board setup is the same when multiple TRION boards are used. The first board has to be set to Master mode, 
all others to Slave:

Master board setup “BoardID0”:

.. code:: c
    
    DeWeSetParamStruct_str(“BoardID0/AcqProp”, “OperationMode”, “Master”);
    DeWeSetParamStruct_str(“BoardID0/AcqProp”, “ExtTrigger”, “False”);
    DeWeSetParamStruct_str(“BoardID0/AcqProp”, “ExtClk”, “False”);

Slave board setup “BoardIDX” [for X from 1 to NrOfAvailableBoards]:

.. code:: c
    
    DeWeSetParamStruct_str(“BoardIDX/AcqProp”, “OperationMode”, “Slave”);
    DeWeSetParamStruct_str(“BoardIDX/AcqProp”, “ExtTrigger”, “PosEdge”);
    DeWeSetParamStruct_str(“BoardIDX/AcqProp”, “ExtClk”, “False”);


SYNC-OUT has to be configured:

* Set the trigger line TRIG7, Source to low
* Set the trigger line TRIG7, Inverted to false

These are the appropriate TRION-API commands:

.. code:: c
    
    DeWeSetParamStruct_str(“BoardID0/Trig7”, “Source”, “Low”);
    DeWeSetParamStruct_str(“BoardID0/Trig7”, “Inverted”, “False”);
    // Then apply the settings using:
    DeWeSetParam_i32(0, CMD_UPDATE_PARAM_ALL, 0);


Slave instrument
~~~~~~~~~~~~~~~~

On slave devices using TRION-SYNC-BUS has to be configured too.
All boards have to be configured to slave mode!


Slave board setup “BoardIDX” [for X from 0 to NrOfAvailableBoards]

.. code:: c

    DeWeSetParamStruct_str(“BoardIDX/AcqProp”, “OperationMode”, “Slave”);
    // Usually “PosEdge”
    DeWeSetParamStruct_str(“BoardIDX/AcqProp”, “ExtTrigger”, “PosEdge”);
    DeWeSetParamStruct_str(“BoardIDX/AcqProp”, “ExtClk”, “False”);


SYNC-OUT has to be configured:

* Set the trigger line TRIG7, Source to high
* Set the trigger line TRIG7, Inverted to false

These are the appropriate TRION-API commands:

.. code:: c

    DeWeSetParamStruct_str(“BoardID0/Trig7”, “Source”, “High”);
    DeWeSetParamStruct_str(“BoardID0/Trig7”, “Inverted”, “False”);
    // Then apply the settings using:
    DeWeSetParam_i32(0, CMD_UPDATE_PARAM_ALL, 0);


Acquisition on the Master instrument
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Acquisition on the master instrument has to be started using:

For each (slave) board of the instrument start:

.. code:: c

    for (int BoardID = 1; BoardID < NrOfAvailableBoards; ++BoardID)
    {
        DeWeSetParam_i32(BoardID, CMD_START_ACQUISITION, 0);
    }
    // Then start acquisition on the master board
    DeWeSetParam_i32(0, CMD_START_ACQUISITION, 0);


Please keep in mind:

* Acquisition on slave instruments has to be started before starting acquisition on the master instrument.
* Acquisition on the slave boards has to be started before starting acquisition on the master board.


Acquisition on the Slave instrument
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Acquisition on the slave instruments has to be started using:

For each board of the instrument start:

.. code:: c
    
    for (int BoardID = 0; BoardID < NrOfAvailableBoards; ++BoardID)
    {
        DeWeSetParam_i32(BoardID, CMD_START_ACQUISITION, 0);
    }


Sync cabling check
~~~~~~~~~~~~~~~~~~

It is possible to check if the sync cables are plugged in correctly.

On each slave instrument use the following commands:

.. code:: c

    int state = 0;
    DeWeGetParam_i32(0, CMD_PXI_LINE_STATE, &state);
    if ((state & PXI_LINE_STATE_TRIG6) == 0)
    {
        // no TRION-SYNC-BUS plugged in on slave instrument
    }



PTP IEEE1588
------------



GPS
---



IRIG
----
