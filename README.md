[![Nuget](https://img.shields.io/nuget/dt/Eliot.UELib.dll?style=for-the-badge)](https://www.nuget.org/packages/Eliot.UELib.dll/)
[![Nuget](https://img.shields.io/nuget/v/Eliot.UELib.dll?style=for-the-badge)](https://www.nuget.org/packages/Eliot.UELib.dll/)

The Unreal library (UELib) provides you an API to read (parse/deserialize) the contents of Unreal Engine game package files such as .UDK, .UPK.
Its main purpose is to decompile the UnrealScript byte-code to its original source-code.

It accomplishes this by reading the necessary Unreal data classes such as:

    UObject, UField, UConst, UEnum, UProperty, UStruct, UFunction, UState,
    UClass, UTextBuffer, UMetaData, UFont, USound, UPackage

Classes such as UStruct, UState, UClass, and UFunction contain the UnrealScript byte-code which we can deserialize in order to re-construct the byte-codes to its original UnrealScript source.

How to use
==============
To use this library you will need [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) (The library will move to .NET 6 or higher at version 2.0)

Install using either:
* Package Manager:
```
    Install-Package Eliot.UELib.dll
```
* NuGet: <https://www.nuget.org/packages/Eliot.UELib.dll>

See [usage](https://github.com/EliotVU/Unreal-Library/wiki/Usage) for further instructions on how to use the library in your project.

If you're looking to modify the library for the sole purpose of modding [UE Explorer](https://eliotvu.com/portfolio/view/21/ue-explorer), I recommend you to clone or fork this repository and install UE Explorer within your ```UELib/src/bin/Debug/``` folder, or change the project's configuration to build inside of the UE Explorer's folder.

Want to try out the [latest library release](https://github.com/EliotVU/Unreal-Library/releases)? Then you can simply save ```Eliot.UELib.dll``` to the folder where you have installed UE Explorer at. Note that the current release of UE Explorer is using version [1.2.7.1](https://github.com/EliotVU/Unreal-Library/releases/tag/release-1.2.7.1).

How to contribute
==============

* Open an issue
* Or make a pull-request by creating a [fork](https://help.github.com/articles/fork-a-repo/) of this repository, create a new branch and commit your changes to that particular branch, so that I can easily merge your changes.

Compatible Games
==============

This is a table of games that are confirmed to be compatible with the current state of UELib, the table is sorted by Package-Version.

| Name    | Engine    | Package/Licensee    | Support State     |
| ------- | --------- | ------------------- | -----------------
| Unreal | 100-226 | 61/000 | |
| [Star Trek: The Next Generation: Klingon Honor Guard](Star%20Trek:%20The%20Next%20Generation:%20Klingon%20Honor%20Guard) | Unknown | 61/000 | |
| Unreal Mission Pack: Return to Na Pali | 226b | 68/000 | |
| Unreal Tournament | 338-436 | 68/000 | |
| Deus Ex | Unknown | Unknown |     |
|     |     |     |     |
|     |     |     |     |
| XIII | Unknown | 100/058 |     |
| Tom Clancy's Rainbow Six 3: Raven Shield | 600-? | 118/012:014 | |
| Unreal Tournament 2003 | 1077-2225 | 119/025 | |
| Unreal II: The Awakening | 829-2001 | 126/2609 | |
| Unreal II: eXpanded MultiPlayer | 2226 | 126/000 | Custom features are not decompiled |
| Unreal Tournament 2004 | 3120-3369 | 128/029 | |
| America's Army 2 | 3369 | 128/032:033 | 2.5, 2.6, 2.8 |
| America's Army (Arcade) | 3369 | 128/032 | 2.6 |
| Red Orchestra: Ostfront 41-45 | 3323-3369 | 128/029 | |
| Killing Floor | 3369 | 128/029 | |
| Battle Territory Battery | Unknown | Unknown |     |
| Swat 4 | Vengeance-Unknown | 129/027 | |
| Bioshock | Vengeance-Unknown | 141/056 | Incomplete but usable |
| Bioshock 2 | Vengeance-Unknown | 143/059 | Incomplete but usable |
| Unreal Championship 2: Liandri Conflict | 3323 | 151/002 | (Third-party) <https://forums.beyondunreal.com/threads/unreal-championship-2-script-decompiler-release.206036/> |
| The Chronicles of Spellborn | Unknown | 159/029 | |
|     |     |     |     |
|     |     |     |     |
| Roboblitz | 2306 | 369/006 |     |
| Medal of Honor: Airborne | 2859 | 421/011 |     |
| Mortal Kombat Komplete Edition | 2605 | 472/046 |     |
| Stargate Worlds | 3004 | 486/007 |     |
| Gears of War | 3329 | 490/009 | |
| Unreal Tournament 3 | 3809 | 512/000 | |
| Mirrors Edge | 3716 | 536/043 |     |
| Alpha Protocol | 3857 | 539/091 |     |
| APB: All Points Bulletin | 3908 | 547/028:032 |     |
| Gears of War 2 | 4638 | 575/000 | |
| CrimeCraft | 4701 | 576/005 | |
| Singularity | 4869 | 584/126 | |
| MoonBase Alpha | 4947 | 587/000 | |
| Saw | Unknown | 584/003 | |
| The Exiled Realm of Arborea or TERA | 4206 | 610/014 |     |
| Monday Night Combat | 5697 | 638/000 |     |
| DC Universe Online | 5859 | 638/6405 |     |
| Unreal Development Kit | 6094-12791 | 664-868 | |
| Blacklight: Tango Down | 6165 | 673:002 | |
| Dungeon Defenders | 6262 | 678/002 | Earlier releases only |
| Alice Madness Returns | 6760 | 690/000 |     |
| The Ball | 6699 | 706/000 | |
| Bioshock Infinite | 6829 | 727/075 |     |
| Bulletstorm | 7052 | 742/029 | |
| Red Orchestra 2: Heroes of Stalingrad | 7258 | 765/Unknown | |
| Aliens: Colonial Marines | Unknown | 787/047 | |
| [Dishonored](http://www.dishonored.com/) | 9099 | 801/030 |     |
| Tribes: Ascend | 7748 | 805/Unknown |     |
| Rock of Ages | 7748 | 805/000 |     |
| Sanctum | 7876 | 810/000 |     |
| AntiChamber | 7977 | 812/000 |     |
| Waves | 8171 | 813/000 |     |
| Super Monday Night Combat | 8364 | 820/000 |     |
| Gears of War 3 | 8653 | 828/000 | |
| Quantum Conundrum | 8623 | 832/32870 | |
| Borderlands | Unknown | Unknown |  |
| Borderlands 2 | 8623/023 | 832/056 | |
| Remember Me | 8623 | 832/021 |     |
| The Haunted: Hells Reach | 8788 | 841/000 |     |
| Blacklight Retribution | 8788-10499 | 841-864/002 |     |
| Infinity Blade 2 | 9059 | 842/001 |     |
| Q.U.B.E | 8916 | 845/000 |     |
| XCOM: Enemy Unknown | 8916 | 845/059 | |
| Gears of War: Judgement | 10566 | 846/000 |     |
| InMomentum | 8980 | 848/000 |     |
| [Unmechanical](http://unmechanical.net/) | 9249 | 852/000 |     |
| Deadlight | 9375 | 854/000 |     |
| Ravaged | 9641 | 859/000 |     |
| [The Five Cores](http://neeblagames.com/category/games/thefivecores/) | 9656 | 859/000 |     |
| Painkiller HD | 9953 | 860/000 |     |
| Hawken | 10681 | 860/004 |     |
| Guilty Gear Xrd | 10246 | 868/003 | [Decryption required](https://github.com/gdkchan/GGXrdRevelatorDec) |
| Gal*Gun: Double Peace | 10897 | 871/000 | |
| [Might & Magic Heroes VII](https://en.wikipedia.org/wiki/Might_%26_Magic_Heroes_VII) | 12161 | 868/004 | (Signature and custom features are not supported) 
| Soldier Front 2 | 6712 | 904/009 |     |
| Rise of the Triad | Unknown | Unknown |     |
| Outlast | Unknown | Unknown |     |
| Sherlock Holmes: Crimes and Punishments | Unknown | Unknown | |
| Alien Rage | Unknown | Unknown |     |

**Beware, opening an unsupported package could crash your system! Make sure you have
saved everything before opening any file!**

**Note** UE3 production-ready packages are often **compressed** and must first be decompressed, [Unreal Package Decompressor](https://www.gildor.org/downloads) by **Gildor** is a tool that can decompress most packages for you; for some games you need a specialized decompressor, see for example [RLUPKTool](https://github.com/AltimorTASDK/RLUPKTool).

Want to add support for a game? See [adding support for new Unreal classes](https://github.com/EliotVU/Unreal-Library/wiki/Adding-support-for-new-Unreal-classes)

Do you know a game that is compatible but is not listed here? Click on the top right to edit this file!
