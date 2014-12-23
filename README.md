[![Build status](https://ci.appveyor.com/api/projects/status/451gy3lrr06wfxcw?svg=true)](https://ci.appveyor.com/project/EliotVU/unreal-library) 
[![Gratipay](https://img.shields.io/gratipay/EliotVU.svg)](https://www.gratipay.com/eliotvu/)

The Unreal library provides you an API to parse/deserialize package files such as .UDK, .UPK, from Unreal Engine games, and provide you the necessary methods to navigate its contents.

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

Interface
==============

Common sense tells me you'd like to test UE Library using an interface, luckily you can use the latest version of UE Explorer to use your latest build of Eliot.UELib.dll by replacing the file in the installed folder of UE Explorer e.g.
  "%programfiles(x86)%\Eliot\UE Explorer\"
Replace Eliot.UELib.dll with yours.

Grab the latest [UE-Explorer.1.2.7.0.rar](http://eliotvu.com/updates/UE-Explorer.1.2.7.0.rar)

How-To
==============
[Adding support for new Unreal classes](https://github.com/EliotVU/Unreal-Library/wiki/Adding-support-for-new-Unreal-classes) 

Contribute
==============

Feel free to fill in an issue to request documentation for a specific feature. Or contribute missing documentation by editing this file.

TODO
==============
* Re-organize and rename most of the files.
* Decompress LZO .upk files.
* Full UE4 Support.
* Make it Mono compatible.
