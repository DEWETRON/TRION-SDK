XML Reference
=============

TRION-SDK exposes information about the measurement devices by
exporting multiple different xml documents. These hold information
about the selected measurement device, acquisition parameters,
calibration information and many more.

This section explains howto access and manipulate those documents.


Relevant API interface
----------------------

XML documents can be accessed by the DeweSetParamStruct_str class
functions. XPath operations on these documents are possible by
using the DeweSetParamXML_str class functions.


.. code:: c

    int DeweSetParamStruct_str)(const char *target, 
        const char *command,
        const char *val);
    int DeweGetParamStruct_str(const char *target, 
        const char *command, 
        char *val, 
        uint32 val_size);
    int DeweGetParamStructEx_str(const char *target, 
        const char *command, 
        char *val, 
        uint32 val_size);
    int DeweGetParamStruct_strLEN(const char *target, 
        const char *command, 
        uint32 *val_size);
    
    int DeweSetParamXML_str(const char *target, 
        const char *command, 
        const char *val);
    int DeweGetParamXMLStruct_str(const char *target, 
        const char *command, 
        char *val, 
        uint32 val_size);
    int DeweGetParamXMLStruct_strLEN(const char *target, 
        const char *command, 
        uint32 *val_size);


Accessible Documents
--------------------

1. BoardProperties.xml
  
..  * AcquisitionProperties
..  * ChannelProperties


BoardProperties XML-File
------------------------

The Boardproperties XML-file serves several purposes:

-  It holds information specific to a single board, like

   -  Board-type and Board-name
   -  The serial number
   -  Information in which enclosure and at which slot within the
      enclosure this board can be found in the physical system

-  It is the implicit documentation of the capabilities that one
   specific board offers
-  It is the implicit documentation of all settable properties of a
   single board together with the definition set for each property.

So instead of having the risk of a potentially outdated
paper-documentation about the logical capabilities of a single board,
this XML-File can and should be used as a look-up reference while
developing an application.

This file is generated at runtime and will therefore always reflect the
actual capabilities of the current API-version in conjunction with the
current capabilities of one specific board. So for example if either the
API and/or the firmware of a specific board are enhanced with new
functionality that is externally accessible, this fact will be
immediately visible in the generated file.

This file is internally used by the API for parameter-checking. Using
this file as base for the application guarantees using valid settings
that will be accepted by the API.

Not all information within this file is strictly necessary for operating
the board. Some of the information only serves documentation or
maintenance purposes.


Relevant Sections of the File
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The top-level nodes of the XMLfile are:

.. figure:: _img/image6.png
    :align: center

    BoardProperties Root Node


Version
^^^^^^^

This node holds detailed information about the API and the input-source
used for generating the properties file. Its main use is for
documentation purposes.

BoardInfo
^^^^^^^^^

This node holds detailed information about the board itself like

-  Serial number
-  Firmware version information
-  Administrative information about the calibration

SystemInfo
^^^^^^^^^^

This node holds detailed information about the enclosure the board is
residing in. For example the PXI-slot-number could be extracted from
here to be shown in the application User Interface.

Variable
^^^^^^^^

This node only serves internal purposes and is not meant to be used
by the application directly.

BoardFeatures
^^^^^^^^^^^^^

This section roughly describes the acquisition capabilities of the board
at hand. This example shows a TRION-2402-dACC-6-BNC (analogue sampling
board with six analogue channels)


.. code-block:: XML
    :caption: BoardProperties - BoardFeatures Node

    <BoardFeatures>
        <AI>
            <Channels>6</Channels>
            <Resolution Count = "2" Default = "0">
                <ID0>24</ID0>
                <ID1>16</ID1>
            </Resolution>
        </AI>
        <ARef>
            <Channels>1</Channels>
        </ARef>
        <CNT>
            <Channels>2</Channels>
            <Resolution>32</Resolution>
            <TimeBase Unit = "MHz">80</TimeBase>
        </CNT>
        <BoardCNT>
            <Channels>1</Channels>
            <Resolution>32</Resolution>
            <TimeBase Unit = "MHz">80</TimeBase>
        </BoardCNT>
        <ChnNoStart>
            <AI/>
            <CNT/>
            <BoardCNT/>
            <DI/>
            <CAN/>
        </ChnNoStart>
    </BoardFeatures>


From this information the application can deduce:

-  The board has six analogue input channels
-  The analogue input channels can be used with 16 and 24 Bit resolution
-  The default resolution is 24 Bit
-  2 counter channels are available with a 80MHz resolution
-  1 internal counter channel is available - a so called BoardCounter
-  No digital or CAN channels are available



AcquisitionProperties
---------------------

This node holds very detailed information about the various settings
necessary for the synchronization capabilities of the board and general
settings affecting the acquisition itself (e.g. the sample-rate).

This node is very elaborate and needs only to be considered in detail
for more advanced setup. Discussing these nodes in detail is beyond the
scope of this document. So its contents are not fully shown here.

One more general sub-node within this node is AcqProp


AcqProp
~~~~~~~
This node holds generic setup information about the acquisition
parameters for this board.


.. figure:: _img/image7.png
    :align: center

    BoardProperties - AcqProp Node


Illustration 7 - BoardProperties - AcqProp Node

The most interesting sub-elements within here probably are:

-  SampleRate
-  OperationMode
-  ResolutionAI

SampleRate
^^^^^^^^^^

.. figure:: _img/image8.png
    :align: center

    BoardProperties - SampleRate Node


This allows the application to know the upper and lower limits of the
available sample-rates. In this case the range goes from 100
Samples/second up to 200 kSamples/second, 204800 Hz to be exact.


OperationMode
^^^^^^^^^^^^^

.. code-block:: XML
    :caption: BoardProperties - OperationMode Node

    <OperationMode Count = "2" Default = "0">
        <ID0>Slave</ID0>
        <ID1>Master</ID1>
    </OperationMode>

This property allows selecting the predefined roles of the board within
a multi-board system. Together with the information about external
clocking and external triggering this will automatically set up the
routing for the internal trigger- and clock-lines to a predefined state
that is suited to make the board fulfill its desired role.


ResolutionAI
^^^^^^^^^^^^

.. code-block:: XML
    :caption: BoardProperties - ResolutionAI Node

    <ResolutionAI Count = "2" Default = "0">
        <ID0>24</ID0>
        <ID1>16</ID1>
    </ResolutionAI>


This property allows setting the analogue channels to a desired
ADC-resolution.




ChannelProperties
-----------------

This node gives exhaustive information about all available acquisition
channels and all their settable properties.


.. figure:: _img/image9.png
    :align: center

    BoardProperties - ChannelProperties Node


In this case, the XML-File shows that:

-  There are six analogue channels, labeled AI0 to AI5
-  Two counter channels, labeled CNT0 and CNT 1
-  One internal counter (the Board-Counter), labeled BoardCNT0

The basic layout for all the channel-types is always the same and allows
for easy initial navigation within the node.

The analogue channel 0 is used as example to explain this in more depth.


.. figure:: _img/image10.png
    :align: center

    BoardProperties - AI0 Channel Node


The first layer always holds:

-  a list of supported operation modes
-  the Used-flag itself as it is independent of the chosen mode

In this example the list of modes is:

-  <Mode Mode = "Calibration">
-  <Mode Mode = "Voltage">
-  <Mode Mode = "Resistance">
-  <Mode Mode = "IEPE">


Element AI0
~~~~~~~~~~~

Looking at AI0 Channel Node, the
default indicates that the mode “Voltage” is set as default.

Each of the modes lists its own associated properties.

The list of applicable properties may vary between the modes. Properties
which are only mentioned in some modes but not in others simply indicate
that they would have no actual meaning in the modes where they are not
listed.

Here within the analogue channels this is not the case. An example would
be counter-channels that have a mode called “Simple Event Counting” -
that only takes one input signal - and therefore have only one source
mentioned in this mode but also support a “gated event counting” - that
will take two distinct input-signals - and therefore has two separate
sources settable.

Taking the Voltage mode as an example:



Voltage/Range
^^^^^^^^^^^^^

.. figure:: _img/image11.png
    :align: center

    BoardProperties - Voltage Mode

This is the exhaustive list of supported properties for an analogue
channel in voltage mode.


The most obvious property here is Range:

.. code-block:: XML
    :caption: BoardProperties - Range Node

    <Range
        Unit = "V"
        Count = "7"
        Default = "0"
        Programmable = "True"
        MinInputOffset = "-200"
        MaxInputOffset = "200"
        MinOutputOffset = "-150"
        MaxOutputOffset = "150"
        MinTotalOffset = "-300"
        MaxTotalOffset = "300"
        AmplRangeMin = "0.01"
        AmplRangeMinUnit = "V"
        AmplRangeMax = "100"
        AmplRangeMaxUnit = "V"
        ProgMax = "100"
        ProgMin = "-100">
        <ID0 MinInputOffset = "-100" MaxInputOffset = "100">100</ID0>
        <ID1>30</ID1>
        <ID2>10</ID2>
        <ID3>3</ID3>
        <ID4>1</ID4>
        <ID5>0.3</ID5>
        <ID6>0.1</ID6>
        <ID7>0.03</ID7>
    </Range>


From this information the application can deduce:

-  The analogue input supports ranges from 0.03V to 200V
-  The default input range is 200V
-  It is freely programmable. So any value between min and max can be
   set and the hardware is not limited to the values presented in the
   list
-  Various information about offsets. Explaining these in detail is
   beyond the scope of this overview document.



Using the BoardProperties XML-File
----------------------------------

With the overview provided in mind one obvious use case
for this document is to allow the application to perform some
preliminary evaluation of settings before trying to apply them to the
API.

One other use case would be that it easily allows the application to
decide about setup-information shown in the user-interface if needed. It
would be easy to just offer such options in the UI that are actually
supported by the board for the given mode.

One less obvious information the document provides is information needed
for the string based functions – namely the target string and the
item-identifier.


Deriving Target Strings and Item IDs from the Document
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

As shown all logical properties are addressed by strings.

As a rule of thumb one can assume that the target string matches the
path within the XML-File starting from the root but omitting the first
node-level.

The last element identifies the Items. Anything that remains in before
is part of the target-string.

This is entirely true for the acquisition properties and slightly
different for the channel properties. The detailed rational for this
approach is provided within this chapter along with a couple of
examples.


Acquisition Properties
^^^^^^^^^^^^^^^^^^^^^^

While the examples provided here will look awfully complicated on first
sight, the procedure itself is generic enough to become natural to the
application developer very quickly.


Example: How to setup/retrieve the SampleRate with the string based functions
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Path within the XML-File: AcquisitionProperties/AcqProp/SampleRate

Derived target and item-string for the string based functions:

-  Applying the rule of omitting the first node-level: AcqProp/SampleRate
-  Last part is the item: “SampleRate”
-  Remainder for the target string: AcqProp
-  As the target-string always needs the BoardID as part of it the
   complete Target string is: “BoardID0/AcqProp”

So the final function call looks like:

.. code:: c

    DeweSetParamStruct_str( "BoardID0/AcqProp", "SampleRate", "20000" );


Example: How to setup the ResolutionAI
''''''''''''''''''''''''''''''''''''''

Path within XML-File: AcquisitionProperties/AcqProp/ResolutionAI

Derived target- and item-string:

- Target: “BoardID0/AcqProp”
- Item: “ResolutionAI”

Final function call:

.. code:: c
    
    DeweSetParamStruct_str( "BoardID0/AcqProp", "ResolutionAI", "16" );



Channel Properties
^^^^^^^^^^^^^^^^^^

As discussed in the first level on a single channel
within the XML-File is a list of various modes grouping the properties
meaningful for the given mode.

This has two consequences when deriving target and item information:

#. Mode itself is a valid Item
#. When addressing any property the mode-node is omitted within the
   target


Example: How to set the mode of an analogue channel to Resistance
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Path within XML-File: ChannelProperties/AI0/Mode
Derived target- and item-string:

- Target: “BoardID0/AI0”
- Item: “Mode”

Final function call:

.. code:: c
    
    DeweSetParamStruct_str( "BoardID0/AI0", "Mode", "Resistance" );


Example: How to set the Range of an analogue channel in Resistance-mode
'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

Path within XML-File: ChannelProperties/AI0/Mode/Range

Derived target- and item-string:

- Target: “BoardID0/AI0”
- Item: “Range”

Final function call:

.. code:: c

    DeweSetParamStruct_str( "BoardID0/AI0", "Range", "3000" );


BoardConfig XML-File
--------------------

Purpose of the File
~~~~~~~~~~~~~~~~~~~

The detailed layout of the configuration-file is boar specific. It can
be derived from the content of the BoardProperties XML-File.

The file has three major sections:

<BoardInfo>

This section holds basic information about the board from which this
configuration was generated from. This includes the BaseModel-Number and
the human readable BoardName (BoardType, eg. TRION-BASE).

<Acquisition>

This is the counterpart to the Node <AcquisitionProperties> in the
BoardProperties-XML-Document.

Each property from the BoardProperties-XML-Document, that is not marked
with the attribute “Config = ‘False’” is also present in the
<Acquisition> Node in the configuration document.

This includes some obvious information like for example the “SampleRate”
as well, as for example detailed routing information for the
PXI-trigger-lines or the Star-Hub.

<Channel>

This is the counterpart to the Node
<ChannelProperties> in the BoardProperties-XML-document.

Each single channel is present within this node, with each of the
settable properties, that is not marked with the attribute “Config =
‘False’” inside the BoardProperties-XML-document.


.. figure:: _img/image12.png
    :align: center

    Configuration-XML major sections


.. figure:: _img/image13.png
    :align: center

    Configuration XML Section: Acquisition


.. figure:: _img/image14.png
    :align: center

    Configuration XML Section: Channel


Result XML Document
'''''''''''''''''''

The result xml-document has the same layout, as the configuration
xml-document.

It’s main purpose is to provide a fine granulated feedback about any
set-configuration command. When loading (setting) a configuration, each
property is applies one after the other. Every single setting-command
(this means every set single property) ends up in a defined result
state. Ideally the operation succeeds with ERR_NONE. However - as any
arbitrary configuration can be presented to any board, this isnot
guaranteed at all.

To make diagnostics easier, this result-file offers the possibility to
see the exact result of each single property.

Depending on the API-configuration itself, either all results are
returned, or only results <> ERR_NONE.

The following figure shows an example for a result-file, when trying to
apply a 8-AI-Channel-board configuration to a board, that only supports
6 AI-channels.

All settings are applied ok, except for the non-existing channels AI6
and AI7.

.. code-block:: XML
    :caption: Configuration load result XML example

    <?xml version="1.0"?>
    <Results>
        <Acquisition>
            <AcqProp>
                <StartCondition>Warning -190910, WARNING_STARTCONDITION_NOT_USED (-190910)</StartCondition>
            </AcqProp>
        </Acquisition>
        <Channel>
            <AI6>
                <Mode>Error 120012, ERROR_AI_CHANNEL_NOT_VALID (120012)</Mode>
            </AI6>
            <AI7>
                <Mode>Error 120012, ERROR_AI_CHANNEL_NOT_VALID (120012)</Mode>
            </AI7>
        </Channel>
    </Results>





Scan Descriptor
~~~~~~~~~~~~~~~

TODO


TEDS
~~~~

TODO
