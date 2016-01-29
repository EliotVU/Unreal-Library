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
  
Grab the latest [UE-Explorer.1.2.7.1.rar](http://eliotvu.com/updates/UE-Explorer.1.2.7.1.rar) and replace the Eliot.UELib.dll with yours, I recommend that you change the output path to your UE Explorer's installation folder.

How-To
==============
[Adding support for new Unreal classes](https://github.com/EliotVU/Unreal-Library/wiki/Adding-support-for-new-Unreal-classes) 

Contribute
==============

To contribute click the [fork button at the top right](https://help.github.com/articles/fork-a-repo/) and follow it by cloning your fork of this repository.

This project uses Visual Studio for development, while it is not restricted to Visual Studio it is recommended to use VS because it has the best support for C#, you can get Visual Studio from http://www.visualstudio.com/ for free, if you already have Visual Studio, it should be atleast Visual Studio 2010+.

The following kind of contributions are welcome:
* Any bug fix or issue as reported under "issues" on this github repository.
* Support for a new game.
* Support for decompression, and/or decryption.
* Documentation on how to use this library.
* General improvements in the decompilation output. 
* Mono compatibility.

Code style
==============

Any contribution should follow the styling of the current code style as seen in the source files:
* 4 indentation spaces.
* _CamelCase for private/protected fields.
* CamelCase naming for everything else but constants which use CAMEL_CASE.
* Keep code lines readable by using spaces and new lines as a way of grouping code statements.
* It is too much to mention every restriction here, so it is best to match the style of the nearby code.
