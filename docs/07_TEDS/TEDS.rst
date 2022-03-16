TEDS Interface
==============
A transducer electronic data sheet (TEDS) is a standardized method of storing transducer (sensors or actuators) identification, calibration, correction data, and manufacturer-related information.
(from wikipedia)


Prerequisites
-------------

TEDS is available for TRION analog channels that support TEDS in dedicated measurment modes. Modes supporting it have
the channel feature "SupportTEDS" listed in the channel properties xml.


.. code-block:: XML
    :caption: ChannelProperties - ChannelFeatures Node

    <Mode Mode = "Voltage">
        
        ...

        <ChannelFeatures Config = "False" Count = "2">
            <ID0>AmplifierZero</ID0>
            <ID1>SupportTEDS</ID1>
        </ChannelFeatures>
        <TEDSOptions Config = "False">
            <Feature Name = "TEDSScanDefault">True</Feature>
        </TEDSOptions>
    </Mode>


The other setting TEDSScanDefault is a hint to an application, that TEDS can be used during measurement.


.. note::
    Depending on the hardware implementation, an active TEDS scan may lead to false measurement results.
    TEDSScanDefault set to True means a scan during a measurement will not add extra noise to the read data.



API Functions
-------------


TedsReadEx
~~~~~~~~~~

TedsReadEx reads a TEDSData XML document. The target string addresses the dedicated analog channel to ask for TEDS 
information. TEDS_DATA should be string buffer large enough for the read document.


.. code:: c

    char TEDS_DATA[8*1024] = { 0 };
    int DeWeGetParamStruct_str( "BoardID0/AI0", "TedsReadEx",
        TEDS_DATA, sizeof(TEDS_DATA) );



.. code-block:: XML
    :caption: TEDSProperties - TEDS.xml

    <?xml version="1.0"?>
    <TEDSData>
        <TEDSType>DS2431</TEDSType>
        <SerialNumber>0000005CA657</SerialNumber>
        <MemoryRegion Name = "EEPROM" Writeable = "true">
            <MemorySize Unit = "Byte">128</MemorySize>
            <Data>1F40620000000000841C000000000000000002000000000000006418008021C10E
            C34800000000E00000000000000000000000000000000000000000000000000000000000
            000000000000000000000000000000000000000000000000000000000000000000000000
            00000000000000000000000000000000000000</Data>
        </MemoryRegion>
        <ROMCodeRaw>2D57A65C00000019</ROMCodeRaw>
        <TEDSInfo Manufacturer = "31" Serial = "0" TedsVersion = "2" Model = "393" VersionLetter = " " VersionNumber = "0">
            <Template Number = "33" Manufacturer = "0" Title = "Bridge Sensor" Abstract = "IEEE 1451.4 Default Bridge Sensor Template&#10;For sensors that provide a bridge circuit to a measuring device">
                <Property Name = "ElecSigType" Type = "5">3</Property>
                <Property Name = "MinPhysVal" Type = "2">0</Property>
                <Property Name = "MaxPhysVal" Type = "2">0</Property>
                <Property Name = "MinElecVal" Type = "2">0</Property>
                <Property Name = "MaxElecVal" Type = "2">0</Property>
                <Property Name = "MapMeth" Type = "5">0</Property>
                <Property Name = "BridgeType" Type = "5">1</Property>
                <Property Name = "SensorImped" Type = "2">40</Property>
                <Property Name = "RespTime" Type = "2">9.9999999999999995e-07</Property>
                <Property Name = "ExciteAmplNom" Type = "2">2.5000000000000004</Property>
                <Property Name = "ExciteAmplMin" Type = "2">1</Property>
                <Property Name = "ExciteAmplMax" Type = "2">6</Property>
                <Property Name = "CalDate" Type = "4">37254</Property>
                <Property Name = "CalInitials" Type = "1"></Property>
                <Property Name = "CalPeriod" Type = "3">0</Property>
                <Property Name = "MeasID" Type = "3">0</Property>
            </Template>
        </TEDSInfo>
    </TEDSData>



The following list explains all possible XML Elements and their XML attributes:

TEDSData description
^^^^^^^^^^^^^^^^^^^^

.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|p{9cm}|

.. table:: TEDS XML description
   :widths: 10 10 80

   +---------------+--------------------------+------------------------------------------------------+
   | **Element**   | **Attribute**            | **Description**                                      |
   +===============+==========================+======================================================+
   | TEDSData      |                          | TEDS root element                                    |
   +---------------+--------------------------+------------------------------------------------------+
   | TEDSType      |                          | TEDS EEPROM chip                                     |
   +---------------+--------------------------+------------------------------------------------------+
   | SerialNumber  |                          | Read only TEDS Serial Number                         |
   +---------------+--------------------------+------------------------------------------------------+
   | MemoryRegion  |                          | One or more memory regions                           |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Name                     | Region name                                          |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Writeable                | 'true' if writeable                                  |
   +---------------+--------------------------+------------------------------------------------------+
   | MemorySize    |                          | Size of the EEPROM                                   |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Unit                     | Usually "Byte"                                       |
   +---------------+--------------------------+------------------------------------------------------+
   | Data          |                          | EEPROM content as hexadecimal coded string           |
   +---------------+--------------------------+------------------------------------------------------+
   | ROMCodeRaw    |                          | Read only, family code plus serial number            |
   +---------------+--------------------------+------------------------------------------------------+
   | TEDSInfo      |                          | Sensor information                                   |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Manufacturer             | Sensor manufacturer                                  |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Serial                   | Sensor serial number                                 |
   +---------------+--------------------------+------------------------------------------------------+
   |               | TedsVersion              | Usually 2 (1=IEEE 1451.4 D0.9x; 2=IEEE 1451.4 final) |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Model                    | Model Number of the sensor                           |
   +---------------+--------------------------+------------------------------------------------------+
   |               | VersionLetter            | Version letter of the sensor                         |
   +---------------+--------------------------+------------------------------------------------------+
   |               | VersionNumber            | Version number of the sensor                         |
   +---------------+--------------------------+------------------------------------------------------+
   | Template      |                          | Recognized and decoded template                      |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Number                   | The number of the template                           |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Manufacturer             | Template manufacturer                                |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Title                    | Template name                                        |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Abstract                 | Descriptive text about the sensor template           |
   +---------------+--------------------------+------------------------------------------------------+
   | Property      |                          | List of all the decoded template properties          |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Name                     | Property name                                        |
   +---------------+--------------------------+------------------------------------------------------+
   |               | Type                     | Number describing the data type                      |
   +---------------+--------------------------+------------------------------------------------------+


TedsWriteEx - Writing <Data>
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

TedsWriteEx writes the <Data> content of a TEDSData XML document into a attached TEDS eeprom.
The target string addresses the dedicated analog channel to write the TEDS 
information to. TEDS_DATA has to contain the necessary XML document.

.. code:: c

    char TEDS_DATA[8*1024] = { "<TEDSDATA>..." };
    int DeWeSetParamStruct_str( "BoardID0/AI0", "TedsWriteEx", TEDS_DATA);


.. code-block:: XML
    :caption: Write TEDS - TEDS.xml

    <?xml version="1.0"?>
    <TEDSData>
        <TEDSType>DS2431</TEDSType>
        <SerialNumber>0000005CA657</SerialNumber>
        <MemoryRegion Name = "EEPROM" Writeable = "true">
            <MemorySize Unit = "Byte">128</MemorySize>
            <Data>1F40620000000000841C000000000000000002000000000000006418008021C10EC34800000000E0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000</Data>
        </MemoryRegion>
        <ROMCodeRaw>2D57A65C00000019</ROMCodeRaw>
    </TEDSData>


It is not necessary that the document contains a TEDSInfo node. Changes to a TEDSInfo node will be ignored.



Editing TEDS Properties
~~~~~~~~~~~~~~~~~~~~~~~

The i32 commands: CMD_BOARD_AITEDSEX_READ, CMD_BOARD_AITEDSEX_SYNCHRONIZE and CMD_BOARD_AITEDSEX_WRITE allows
developers to edit TEDS properties without the need to create the <Data> hexadecimal representation.

For this mode the changes have to be written into an API internal TEDS document addressable by the target string:
"BoardId0/aitedsex/AI0". 

Do all changes to this document and then the changes can be applied to the EEPROM.


.. code:: c

    int nBoardId = 0;
    int nChannelIndex = 0;
    DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_READ, nChannelIndex);

    // set serial to 1234
    DeWeSetParamXML_str( "BoardID0/AI0", "TEDSData/TEDSInfo/@Serial, "1234");

    // set MinPhysVal to -10
    DeWeSetParamXML_str( "BoardID0/AI0", "TEDSData/TEDSInfo/Template/Property[@Name='MinPhysVal'], "-10");

    // generate the new valid TEDS Data
    DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_SYNCHRONIZE, nChannelIndex);

    // write to the EEPROM
    DeWeSetParam_i32(nBoardId, CMD_BOARD_AITEDSEX_WRITE, nChannelIndex);





TedsReadExChain
~~~~~~~~~~~~~~~

It is allowed to physically chain TEDS EEPROMs. For this case TedsReadExChain has to be used.

TedsReadExChain reads a TEDSChain XML document. The target string addresses the dedicated analog channel to ask for TEDS 
information. TEDS_DATA should be string buffer large enough for the read document.

The XML document presents all found TEDS EEPROMs as TEDSData child elements of TEDSChain.

.. code:: c

    char TEDS_DATA[8*1024] = { 0 };
    int DeWeGetParamStruct_str( "BoardID0/AI0", "TedsReadExChain", TEDS_DATA, sizeof(TEDS_DATA) );



.. code-block:: XML
    :caption: ChannelProperties - ChannelFeatures Node

    <?xml version="1.0"?>
    <TEDSChain>
        <TEDSData>
            <TEDSType>DS2431</TEDSType>
            <SerialNumber>0000005CA657</SerialNumber>
            <MemoryRegion Name = "EEPROM" Writeable = "true">
                <MemorySize Unit = "Byte">128</MemorySize>
                <Data>1F40620000000000841C000000000000000002000000000000006418008021C10EC34800000000E0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000</Data>
            </MemoryRegion>
            <ROMCodeRaw>2D57A65C00000019</ROMCodeRaw>
            <TEDSInfo Manufacturer = "31" Serial = "0" TedsVersion = "2" Model = "393" VersionLetter = " " VersionNumber = "0">
                <Template Number = "33" Manufacturer = "0" Title = "Bridge Sensor" Abstract = "IEEE 1451.4 Default Bridge Sensor Template&#10;For sensors that provide a bridge circuit to a measuring device">
                    <Property Name = "ElecSigType" Type = "5">3</Property>
                    <Property Name = "MinPhysVal" Type = "2">0</Property>
                    <Property Name = "MaxPhysVal" Type = "2">0</Property>
                    <Property Name = "MinElecVal" Type = "2">0</Property>
                    <Property Name = "MaxElecVal" Type = "2">0</Property>
                    <Property Name = "MapMeth" Type = "5">0</Property>
                    <Property Name = "BridgeType" Type = "5">1</Property>
                    <Property Name = "SensorImped" Type = "2">40</Property>
                    <Property Name = "RespTime" Type = "2">9.9999999999999995e-07</Property>
                    <Property Name = "ExciteAmplNom" Type = "2">2.5000000000000004</Property>
                    <Property Name = "ExciteAmplMin" Type = "2">1</Property>
                    <Property Name = "ExciteAmplMax" Type = "2">6</Property>
                    <Property Name = "CalDate" Type = "4">37254</Property>
                    <Property Name = "CalInitials" Type = "1"></Property>
                    <Property Name = "CalPeriod" Type = "3">0</Property>
                    <Property Name = "MeasID" Type = "3">0</Property>
                </Template>
            </TEDSInfo>
        </TEDSData>
    </TEDSChain>



TedsType
~~~~~~~~

Low level functionality to only access the romcode.


.. code:: c

    DeWeGetParamStruct_str( "BoardID0/AI0", "TedsType", TEDS_DATA, sizeof(TEDS_DATA) );
    // TEDS_DATA = 2D0000005CA65719



TedsMem
~~~~~~~

Low level functionality to only access TEDS memory regions.

.. code:: c

    DeWeGetParamStruct_str("BoardID0/AI0", "TedsMem0", TEDS_DATA, sizeof(TEDS_DATA));
    // TEDS_DATA = 801F40620000000000841C000000000000000002000000000000006418008021


.. code:: c

    DeWeSetParamStruct_str("BoardID0/AI0", "TedsMem0", "0022334455667788990011223344556677889900112233445566778899001333");



.. code:: c

    DeWeGetParamStruct_str("BoardID0/AI0", "TedsMem1", TEDS_DATA, sizeof(TEDS_DATA));
    // TEDS_DATA = 46C10EC34800000000E000000000000000000000000000000000000000000000


.. code:: c

    DeWeSetParamStruct_str("BoardID0/AI0", "TedsMem1", "0022334455667788990011223344556677889900112233445566778899001333");



SDK Examples
------------

The use of the TRION API TEDS interface is shown in following examples:

    * interfacingTEDSEx
    * interfacingTEDS
    * InterfacingTEDSCal 


