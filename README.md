[![Nuget](https://img.shields.io/nuget/dt/Eliot.UELib.dll?style=for-the-badge)](https://www.nuget.org/packages/Eliot.UELib.dll/)
[![Nuget](https://img.shields.io/nuget/v/Eliot.UELib.dll?style=for-the-badge)](https://www.nuget.org/packages/Eliot.UELib.dll/)

The Unreal library provides you an API to read (parse/deserialize) the contents of Unreal Engine game package files such as .UDK, .UPK.
Its main purpose is to decompile the UnrealScript byte-code to its original source-code.

It accomplishes this by reading the necessary Unreal data classes such as:

    UObject, UField, UConst, UEnum, UProperty, UStruct, UFunction, UState,
    UClass, UTextBuffer, UMetaData, UFont, USound, UPackage

Classes such as UStruct, UState, UClass, and UFunction contain the UnrealScript byte-code which we can deserialize in order to re-construct the byte-codes to its original UnrealScript source.

Installation
==============

Either use NuGet's package manager console or download from: <https://www.nuget.org/packages/Eliot.UELib.dll/>

    PM> Install-Package Eliot.UELib.dll

Instructions
==============

See <https://github.com/EliotVU/Unreal-Library/wiki/Usage>

User Interface
==============

The latest build is always compatible with the [latest UE Explorer](https://eliotvu.com/portfolio/view/21/ue-explorer)'s release, and because of that, you can replace the Eliot.UELib.dll in the installation folder with yours.

Compatible Games
==============

Below is a table of games that are known to be compatible with the UELib.

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
| Unreal Tournament 2003 | 1077-2225 | 119/025 | |
| Unreal II | 829-2001 | 126/2609 | |
| Unreal Tournament 2004 | 3120-3369 | 128/029 | |
| America's Army 2 | 3369 | 128/032:033 | 2.5, 2.6, 2.8 |
| America's Army (Arcade) | 3369 | 128/032 | 2.6 |
| Red Orchestra: Ostfront 41-45 | 3323-3369 | 128/029 | |
| Killing Floor | 3369 | 128/029 | |
| Battle Territory Battery | Unknown | Unknown |     |
| Swat 4 | Unknown | 129/027 |     |
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
| APB: All Points Bulletin | 3908 | 547/032 |     |
| Gears of War 2 | 4638 | 575/000 | |
| CrimeCraft | 4701 | 576/005 | |
| Singularity | 4869 | 584/126 | |
| MoonBase Alpha | 4947 | 587/000 | |
| Saw | Unknown | 584/003 | |
| The Exiled Realm of Arborea or TERA | 4206 | 610/014 |     |
| Monday Night Combat | 5697 | 638/000 |     |
| DC Universe Online | 5859 | 638/6405 |     |
| Unreal Development Kit | 6094-10246 | 664-860 | |
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
| Blacklight Retribution | 8788 | 841/Unknown |     |
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
| [Might & Magic Heroes VII](https://en.wikipedia.org/wiki/Might_%26_Magic_Heroes_VII) | 12161 | 868/004 | (Signature and custom features are not supported) |
| Soldier Front 2 | 6712 | 904/009 |     |
| Rise of the Triad | Unknown | Unknown |     |
| Outlast | Unknown | Unknown |     |
| Sherlock Holmes: Crimes and Punishments | Unknown | Unknown | |
| Alien Rage | Unknown | Unknown |     |

**Beware, opening an unsupported package could crash your system! Make sure you have
saved everything before opening any file!**

**Note** UE3 production-ready packages are often **compressed** and must first be decompressed, [Unreal Package Decompressor](https://www.gildor.org/downloads) by **Gildor** is a tool that can decompress most packages for you; for some games you need a specialized decompressor, see for example [RLUPKTool](https://github.com/AltimorTASDK/RLUPKTool).

Want to add support for a game? See [adding support for new Unreal classes](https://github.com/EliotVU/Unreal-Library/wiki/Adding-support-for-new-Unreal-classes)

Contribute
==============

To contribute click the [fork button at the top right](https://help.github.com/articles/fork-a-repo/) and follow it by cloning your fork of this repository.

This project uses Visual Studio for development, while it is not restricted to Visual Studio it is recommended to use VS because it has the best support for C#, you can get Visual Studio from <http://www.visualstudio.com/> for free, if you already have Visual Studio, it should be atleast Visual Studio 2010+.

The following kind of contributions are welcome:

* Any bug fix or issue as reported under "issues" on this github repository.
* Support for a new game.
* Support for decompression, and/or decryption.
* Documentation on how to use this library.
* General improvements in the decompilation output.
* Mono compatibility.

Code style
==============

Any contribution should try to follow the already existing neighbouring code.
