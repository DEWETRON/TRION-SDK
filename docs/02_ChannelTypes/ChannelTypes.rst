Channel Types
=============

TRION and TRION3 modules support multiple different channel types.
These channels futher support different measurement modes.

The following sections describe the different channels and provide an
extensive list of the available modes.


As an SDK developer you do not have to create your own database
of boards and their channels and modes. A boards abilities
are reported by its *Properties.xml* document.
It can be requested during runtime for every device and is described
in more detail in the XML Reference chapter.



Analog Channels (AI)
--------------------

Technically the path for analog measurement data consist of three
distinct parts on TRION™-boards.

1. The analog input-path performing the signal-conditioning
2. The A/D conversion
3. Digital data post-processing


The TRION-API however encapsulates the exact details of this chain
in a way, so that the various differences in implementation depending
on the exact board-type are not visible above the interface. This allows
an application to choose a rather generic approach toward analog
channels in general, and frees the application developer from the need
to develop against a specific board-type. The property-set for analog
channels basically describes the whole chain from signal-conditioning
to postprocessing in an uniform way.


Channel Properties
~~~~~~~~~~~~~~~~~~

The property set for analog channels is organized beneath
various measurement modes. Under each mode a selected set of
configurable parameters exists. Not all properties are available for
all TRION™-boards. But for each mode a minimum-set of obvious common
configuration items can be enumerated. This chapter will provide an
overview over all currently used properties, sorted by the currently
supported measurement modes, split into the parameters available on
all TRION analog channels, and those available on specific
boards only.

Each property has a list of potential allowed settings. The list has
a minimal size of one entry, if the property has a use within the
given mode.
For non-trivial measurement modes some of the properties have non-trivial
constraints. Those constraints are derivable from the
board-properties-xml-document.

The application does not strictly need to pre-validate properties
against those constraints.
The API will usually adjust set property-values to satisfy those
constraints, and will issue a WRNING-Level errorcode to indicate
this to the application. In such a case, it would be a viable strategy
to invoke the property-getter to retrieve the adjusted value for further
application-processing. However: As this approach might not be suitable
for all types of applications an exhaustive overview over those
property-constraints, and how to validate them on application level.



General Attributes
~~~~~~~~~~~~~~~~~~

Default Attribute
^^^^^^^^^^^^^^^^^

This indicates the index of the default-setting for the property.
The API will set all settings to their default-values, when the
mode is switched.

In the following code block *Default = "2"* is selects
<ID2>10</ID2> as its default value.

.. code-block:: XML
    :caption: Default attribute

    <Range
        Default = "2">
        <ID0>100</ID0>
        <ID1>30</ID1>
        <ID2>10</ID2>
        <ID3>3</ID3>
        <ID4>1</ID4>
        <ID5>0.1</ID5>
    </Range>



ProgMin ProgMax Attribute
^^^^^^^^^^^^^^^^^^^^^^^^^

Some properties can be programmable in a given interval. If this
is the case for a given Property, it is indicated by presence of
the two attributes ProgMin and ProgMax.
Both attributes are always in the same unit as the underlying
property.

.. code-block:: XML
    :caption: ProgMin ProgMax attribute

    <Range
        ProgMax = "100"
        ProgMin = "-100">
    </Range>



Unit Attribute
^^^^^^^^^^^^^^

Generally indicates the Unit used with the given property. This
includes all fixed list-entries of the list, as well as the unit
for ProgMin and ProgMax if given.
In certain modes like Bridge for example, the attribute unit can
also work a distinction-predicate, if one property with all its
definition may exist multiple times. In bridge-mode this would be
for example the case for the property “Range”, which exists once
with unit = “mV/V” and once with the unit = “mV/mA”.

.. code-block:: XML
    :caption: Unit attribute

    <Range
        Unit = "V">
    </Range>


Voltage Mode
~~~~~~~~~~~~

On most TRION™-boards the modes “Voltage” and “Calibration” are
very similar. The Calibration mode usually is more restrictive on
the Range-property, but less restrictive on the Input-Types. The
Calibration Mode usually allows for signal routing to onboard
calibration-sources that have barely a use in normal measurement.
On the range-side it usually does not allow to use a free
programmable value.

.. code-block:: XML
    :caption: Range element

    <Mode Mode = "Voltage">
        <Range>..</Range>
        <InputOffset>..</InputOffset>
        <Excitation>..</Excitation>
        <LPFilter_Type>..</LPFilter_Type>
        <LPFilter_Order>..</LPFilter_Order>
        <LPFilter_Val>..</LPFilter_Val>
        <HPFilter_Type>..</HPFilter_Type>
        <HPFilter_Order>..</HPFilter_Order>
        <HPFilter_Val>..</HPFilter_Val>
        <InputType>..</InputType>
        <IIRFilter_Type>..</IIRFilter_Type>
        <IIRFilter_Order>..</IIRFilter_Order>
        <IIRFilter_Val>..</IIRFilter_Val>
        <HPIIRFilter_Type>..</HPIIRFilter_Type>
        <HPIIRFilter_Order>..</HPIIRFilter_Order>
        <HPIIRFilter_Val>..</HPIIRFilter_Val>
        <InputImpedance>..</InputImpedance>
        <ChannelFeatures>..</ChannelFeatures>
        <TEDSOptions>..</TEDSOptions>
    </Mode>


Range Attribute
^^^^^^^^^^^^^^^
Unit: V

Sets the input-range of the amplifier and post processing chain,
usually in V. In terms of Non-TRION™-signal conditioners this is
closely related to the used gain.


InputOffset Attribute
^^^^^^^^^^^^^^^^^^^^^
Unit: V

This property is often used synonymous to “Sensor-Offset”. It’s
main use is to shift the virtual 0 V by a given value. Due to
various physical effects any non-ideal sensor usually has a bias.
With the property input-offset API can be setup to compensate for
this bias.


InputType Attribute
^^^^^^^^^^^^^^^^^^^
Unit: N/A

This property indicates the possible input-type-configurations.
For example: Single-Ended, Differential
Note: some TRION-boards only support one non-switchable input type.
In this case the property still will be present, but only feature
one entry.


Excitation Attribute
^^^^^^^^^^^^^^^^^^^^
Unit: either V, mA or both

This property allows to configure or disable the excitation
(e.g. for sensor-supply).





Current Mode
~~~~~~~~~~~~


Resistance Mode
~~~~~~~~~~~~~~~


Bridge Mode
~~~~~~~~~~~


Potentiometer Mode
~~~~~~~~~~~~~~~~~~


RTD-Temperature Mode
~~~~~~~~~~~~~~~~~~~~


IEPE Mode
~~~~~~~~~


ExcCurrentMonitor Mode
~~~~~~~~~~~~~~~~~~~~~~


ExcVoltMonitor Mode
~~~~~~~~~~~~~~~~~~~


Calibration Mode
~~~~~~~~~~~~~~~~


MSI Modes
~~~~~~~~~


CAN Mode
~~~~~~~~


Advanced Constraints
--------------------

In Voltage-measurement mode, the exact amplifier-setting only
depends on the range-property and the input-offset-attribute.
In the non-trivial measurement modes the amplifier-setting are affected
by more than those two logical parameters. A typical example would be
bridge-mode, where the amplifier settings are affected by logical range,
input-offset and excitation.

While it would be possible to limit each property in a way, so that all
possible combination would yield a legal amplifier setup, it would hurt
the versatility of the single properties.

This chapter will reveal the dependencies of the various parameters in
the different modes, as well as the formulas used to evaluate versus
the given constraints.


Almost all constraints affect the range-property.
Each range-property-node holds several attributes relevant for
constraints checking:

AmplRangeMax, AmplRangeMin, AmplRangeUnit
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
These attributes indicate the legal maximum and minimum values for the
final amplifier-setup. The AmplRangeUnit is always in volt [V].

MaxInputOffset
~~~~~~~~~~~~~~
Maximum allowed input-offset. This is always given in %-of-range.
On most TRION™-boards this is +/-200%, unless already in the highest
possible range, where usually no further input-offset is allowed.

MaxOutputOffset
~~~~~~~~~~~~~~~
The output-offset is the virtual offset introduce by asymmetrical custom
ranges. For example a custom range of 0..10V would yield a output-offset
of -100%. The limit for the output-offset usually is +/-150%







Range calculation
-----------------


As the TRION-API supports asymmetrical custom ranges, the range is split
into RangeMin and RangeMax. RangeMin is the lower value of a given
range-span, whereby RangeMax is the upper value.

.. tabularcolumns:: |p{3cm}|p{3cm}|p{3cm}|

.. table:: Range Examples
   :widths: 30 30 30

   +----------------------+--------------+--------------+
   | Range                | RangeMin     | RangeMax     |
   +======================+==============+==============+
   | 10V (= -10V .. 10V)  | -10V         | 10V          |
   +----------------------+--------------+--------------+
   | -5 .. 10V            | -5V          | 10V          |
   +----------------------+--------------+--------------+
   | 0 .. 10V             | 0V           | 10V          |
   +----------------------+--------------+--------------+
   | 3 .. 10V             | 3V           | 10V          |
   +----------------------+--------------+--------------+
   | -10 .. 5V            | -10V         | 5V           |
   +----------------------+--------------+--------------+
   | -10 .. 0V            | -10V         | 0V           |
   +----------------------+--------------+--------------+

This is the range (in [V]), the amplifier-path has to be set to, to satisfy
the promise, that the interval RangeMin..RangeMax is covered by the
raw-value-full-scale.


HWRangeMin, HWRangeMax, HWInputOffset
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
As the properties Range (RangeMin..RangeMx) and InputOffset are always in
logical units (eg Ohms for resistance mode), a intermediate step of conversion
is necessary, to translate them to the underlying voltage-measurements.
The HWRangeMin/Max and InputOffset are used subsequentially to calculate the
AmplifierRange. The main-purpose of those values is to keep the calculation
comprehensible.


Amplifier Range
~~~~~~~~~~~~~~~
The result of the calculated AmplifierRange must always satisfy following
condition:

    .. math:: AmplRangeMin[V] \leq AmplifierRange[V] \leq AmplRangeMax


Voltage Mode, Calibration Mode
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Depending on properties: Range, InputOffset

    .. math:: HWRangeMin[V] = RangeMin[V]
    .. math:: HWRangeMax[V] = RangeMax[V]
    .. math:: HWInputOffset[V] = InputOffset[V]
    .. math:: AmplifierRange[V] = max(abs(HWRangeMin+HWInputOffset), \\ abs(HWRangeMax+HWInputOffset))


Resistance Mode
~~~~~~~~~~~~~~~
Depending on properties: Range, InputOffset, Excitation

    .. math:: HWRangeMin[V] = RangeMin[\Omega] * Excitation[A]
    .. math:: HWRangeMax[V] = RangeMax[\Omega] * Excitation[A]
    .. math:: HWInputOffset[V] = InputOffset[\Omega] * Excitation[A]
    .. math:: AmplifierRange[V] = max(abs(HWRangeMin+HWInputOffset), \\ abs(HWRangeMax+HWInputOffset))


Bridge Mode
~~~~~~~~~~~
Depending on properties: Range, InputOffset, Excitation

Note: Excitation and Range are related.


.. tabularcolumns:: |p{2.5cm}|p{2.5cm}|

.. table:: Bridge Range Examples
   :widths: 20 20

   +--------------------+---------------------+
   | Excitation Unit    | Range Unit          |
   +====================+=====================+
   | mA                 | mV/mA               |
   +--------------------+---------------------+
   | V                  | mV/mV               |
   +--------------------+---------------------+

The calculation is shown for mA-unit. Formulas also apply for V-excitations

    .. math:: HWRangeMin[V] = \frac{RangeMin[\frac{mV}{mA}] * Excitation[mA]}{1000}
    .. math:: HWRangeMax[V] = \frac{RangeMax[\frac{mV}{mA}] * Excitation[mA]}{1000}
    .. math:: HWInputOffset[V] = \frac{InputOffset[\frac{mV}{mA}] * Excitation[mA]}{1000}
    .. math:: AmplifierRange[V] = max(abs(HWRangeMin+HWInputOffset), \\ abs(HWRangeMax+HWInputOffset))


Potentiometer Mode
~~~~~~~~~~~~~~~~~~
Depending on properties: Range, InputOffset, Excitation

    .. math:: HWRangeMin[V] = \frac{RangeMin[\%] * Excitation[V]}{100}-\frac{Excitation[V]}{2}
    .. math:: HWRangeMax[V] = \frac{RangeMax[\%] * Excitation[V]}{100}-\frac{Excitation[V]}{2}
    .. math:: HWInputOffset = InputOffset[\%] * Excitation[V]
    .. math:: AmplifierRange[V] = max(abs(HWRangeMin+HWInputOffset), \\ abs(HWRangeMax+HWInputOffset))



RTD-Temperature Mode
~~~~~~~~~~~~~~~~~~~~

TBD


Current Mode, ExcCurrentMonitor Mode
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Depending on properties: Range, ShuntRes

    .. math:: HWRangeMin[V] = RangeMin[A] * ShuntRes[\Omega]
    .. math:: HWRangeMax[V] = RangeMax[A] * ShuntRes[\Omega]
    .. math:: HWInputOffset[V] = InputOffset[A] * ShuntRes[\Omega]
    .. math:: AmplifierRange[V] = max(abs(HWRangeMin+HWInputOffset), \\ abs(HWRangeMax+HWInputOffset))



Analog Out Channels
-------------------


MonitorOutput Mode
~~~~~~~~~~~~~~~~~~


MathOutput Mode
~~~~~~~~~~~~~~~


ConstOutput Mode
~~~~~~~~~~~~~~~~


FunctionGenerator Mode
~~~~~~~~~~~~~~~~~~~~~~


StreamOutput Mode
~~~~~~~~~~~~~~~~~




Counter Channels
----------------

Events Mode
~~~~~~~~~~~


Period Mode
~~~~~~~~~~~


PulseWidth Mode
~~~~~~~~~~~~~~~


TwoPulseEdgeSep Mode
~~~~~~~~~~~~~~~~~~~~


Subcounter Period Mode
~~~~~~~~~~~~~~~~~~~~~~


Subcounter TwoPulseEdgeSep Mode
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~


Subcounter Frequency Mode
~~~~~~~~~~~~~~~~~~~~~~~~~




Digital Channels
----------------


DI Mode
~~~~~~~


DIO Mode
~~~~~~~~




CAN Channels
------------


HighSpeed Mode
~~~~~~~~~~~~~~


CANFD Channels
--------------


Currently not supported




RS485 Channels
--------------


Raw Mode
~~~~~~~~


NMEA Mode
~~~~~~~~~

