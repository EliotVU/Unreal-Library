[![Build status](https://ci.appveyor.com/api/projects/status/451gy3lrr06wfxcw?svg=true)](https://ci.appveyor.com/project/EliotVU/unreal-library) 
[![Gratipay](https://img.shields.io/gratipay/EliotVU.svg)](https://www.gratipay.com/eliotvu/)

What is UE Library
==============

This provides you an API to parse/deserialize package files from the Unreal Engine such as .UDK, .UPK, etc.
The API grants access to every object that resides within such packages. 

At the moment these are all the object classes that are supported by this API:

    UObject, UField, UConst, UEnum, UProperty, UStruct, UFunction, UState,
    UClass, UTextBuffer, UMetaData, UFont, USound, UPackage


Installation
==============

Either use NuGet's package manager console or download from: https://www.nuget.org/packages/Eliot.UELib.dll/

    PM> Install-Package Eliot.UELib.dll

Usage
==============

Include the either the library's .dll file or the forked source code into your own project.
Once referenced, you can start using the library by using the namespace UELib as follows: "using UELib;"

See further instructions at: https://github.com/EliotVU/Unreal-Library/wiki/Usage

Contribute
==============

Feel free to fill in an issue to request documentation for a specific feature. Or contribute missing documentation by editing this file.

TODO
==============
* Re-organize and rename most of the files.
* Decompress LZO .upk files.
* Full UE4 Support.
* Make it Mono compatible.
