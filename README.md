# DEWETRON TRION SDK
DEWETRON TRION SDK. TRION API SDK description and example sources. 

[![Github All Releases](https://img.shields.io/github/downloads/DEWETRON/TRION-SDK/total)]()


A development environment allowing to directly access DEWETRON hardware:

* DEWETRON TRION PXI boards
* DEWETRON TRION3 PXIe boards
* DEWETRON TRIONet devices
* DEWETRON PUREC devices
* DEWETRON NEXDAQ devices


# NEWS

New DEWETRON TRION API package:

* 2024-05-13 Release of TRION API R7.1.0
* 2024-05-13 Release of TRIONET FIRMWARE R7.1.0

Get those for optimal results.


# Prerequisites
To use the DEWETRION TRION SDK you need a current TRION API installation.
The TRION Application installer is available on DEWETRON's customer care center:

TRION Application Installer:
https://ccc.dewetron.com/pl/trion-3-modules

TRIONET Firmware:
https://ccc.dewetron.com/pr/trionet

Or get it from this projects Releases section.

# Documentation

An online version of the TRION SDK documentation is hosted here:

https://dewetron.github.io/TRION-SDK/

The docs directory contains pdf and html versions of the complete documentation.

For offline use we provide this pdf: https://github.com/DEWETRON/TRION-SDK/releases/download/TRION-R7.0.0/dewetrontrionsdk.pdf

# Directory structure

## trion_api

This directory contains language bindings for C, C# and Python

## trion

This directory contains examples directly accessing TRION boards.
Usually for programms directly running on DEWE2 & DEWE3 enclosures.

## trionet
This directory contains examples for accessing TRIONET devices.


# Building the examples

## Examples in C 
Building the C examples require CMAKE (https://cmake.org/).
```cmd
cd trion\C
mkdir build
cd build
```
For a Visual Studio 2019 run:
```cmd
cmake -G "Visual Studio 16 2019" ..
```
Other generators are listed when calling "cmake -G"

Open the generated solution:
```cmd
start TRION_SDK_C.sln
```

## Examples in C#
Building the C# examples also require CMAKE (https://cmake.org/).
```cmd
cd trionet\CS
mkdir build
cd build
```
For a Visual Studio 2019 run:
```cmd
cmake -G "Visual Studio 16 2019" ..
```
Other generators are listed when calling "cmake -G"

Open the generated solution:
```cmd
start TRION_SDK_CSHARP.sln
```


# Contact

**Company information**

[www.dewetron.com](https://www.dewetron.com)

**For general questions please contact:**

support@dewetron.com


**For technical questions please contact:**

- Gunther Laure <gunther.laure@dewetron.com>
- Martin Poniz <martin.poniz@dewetron.com>



# License
MIT License

Copyright (c) 2018 DEWETRON

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
LICENSE (END)
