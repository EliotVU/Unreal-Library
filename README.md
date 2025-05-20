[![Nuget](https://img.shields.io/nuget/v/Eliot.UELib?style=for-the-badge)](https://www.nuget.org/packages/Eliot.UELib/)
![Platform](https://img.shields.io/badge/platform-.NET-blue.svg?style=for-the-badge)
![License](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)

# UELib

The Unreal library (UELib) provides you an API to read (parse/deserialize) the contents of Unreal Engine game package files such as .UDK, .UPK.
Its main purpose is to decompile the UnrealScript byte-code to its original source-code.

It accomplishes this by reading the necessary Unreal data classes such as:

    UObject, UField, UConst, UEnum, UProperty, UStruct, UFunction, UState, UClass,
    UTextBuffer, UMetaData, UPackage

Classes such as UStruct, UState, UClass, and UFunction contain the UnrealScript byte-code which we can deserialize in order to re-construct the byte-codes to its original UnrealScript source.

Additionally UELib is also capable of deserializing of many more data classes such as:

    UFont, USound, UPalette, UTexture,
    UTexture2D, UTexture2DDynamic, UTexture2DComposite, UTexture3D,
    UTextureCube, UTextureFlipBook, UTextureMovie
    UPrimitive, UPolys

## Install

```cmd
    Install-Package Eliot.UELib
```

| Module | Version | License | 
|---|---|---|
| <img src="https://img.shields.io/badge/UELib-brightgreen.svg?style=for-the-badge" /> | [![Nuget](https://img.shields.io/nuget/v/Eliot.UELib?label=Version&logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/Eliot.UELib/) | ![GitHub](https://img.shields.io/github/license/EliotVU/Unreal-Library.svg?style=for-the-badge) |

* Usage: See the [documentation](https://github.com/EliotVU/Unreal-Library/wiki/Usage) for more examples.

```csharp
    using UELib;

    // Instantiates a FileStream and deserializes the package's header, will also auto-detected a suitable build to associate the package with.
    var package = UnrealLoader.LoadPackage(@"C:\Path\Package.upk", System.IO.FileAccess.Read);
    Console.WriteLine($"Version: {package.Summary.Version}");

    // Necessary if working with packages that have been cooked for a console platform, and IF the build was not properly auto-detected.
    // package.CookerPlatform = BuildPlatform.Console;

    // Initializes the registered classes, constructs and deserializes(loads) the package objects.
    package.InitializePackage();

    // Now we can iterate over all the initialized objects, but beware! This includes fake-import objects.
    foreach (var obj in package.Objects)
    {
        // If positive then we have an export, or import if negative, if null, we are working with a 'None' object, but this shouldn't occur here.
        if ((int)obj > 0)
        {
            Console.WriteLine($"Export:");
        }
        else if ((int)obj < 0)
        {
            Console.WriteLine($"Import:");
        }

        Console.WriteLine($"ObjectIndex: {(int)obj}");
        Console.WriteLine($"Name: {obj.Name}");
        Console.WriteLine($"Class: {obj.Class?.Name}");
        Console.WriteLine($"Outer: {obj.Outer}");
        Console.WriteLine($"Path: {obj.GetReferencePath()}");
    }
```

If you're looking to modify the library for the sole purpose of modding [UE Explorer](https://github.com/UE-Explorer/UE-Explorer), I recommend you to clone or fork this repository and install UE Explorer within your ```UELib/src/bin/Debug/``` folder, or change the project's configuration to build inside of the UE Explorer's folder.

## Compatible Games

This is a table of games that are confirmed to be compatible with the current state of UELib, the table is sorted by Package-Version.

| Name                  | Engine:Branch    | Package/Licensee    | Support State     |
| --------------------- | ---------------- | ------------------- | ----------------- |
| Unreal | 100-226 | 61/000 | |
| [Star Trek: The Next Generation: Klingon Honor Guard](Star%20Trek:%20The%20Next%20Generation:%20Klingon%20Honor%20Guard) | 219 | 61/000 | |
| X-COM: Alliance | 200-220 | 61/000 | Bad output at the start of functions (BeginFunctionToken) |
| Dr. Brain: Action Reaction | 224 | 63-68/000 | |
| Nerf Arena Blast | 225 | 63-68/000 | |
| The Wheel of Time | 225:WoT | 63-68/000 | |
| Unreal Mission Pack: Return to Na Pali | 226b | 68/000 | |
| Unreal Tournament | 338-436 | 68-69/000 | |
| Deus Ex | 400-436 | 68/000 | |
| Jazz Jackrabbit 3D | 400 | 68/000 | |
| Duke Nukem Forever (2001) | 613 | 68/002 | UStruct offsets are off leading to bad output code |
| Rune | 400 | 69/000 | |
| Unrealty | 405 | 69/000 | |
| X-COM: Enforcer | 420 | 69/000 | |
| Tactical Ops: Assault on Terror | 436 | 69/000 | |
| Star Trek: Deep Space Nine: The Fallen | 338 | 73/000 | |
| Harry Potter and the Sorcerer's Stone | 436 | 76/000 | |
| Harry Potter and the Chamber of Secrets | 433 | 79/000 | |
| Disney's Brother Bear | 433 | 80/000 | [Link](https://github.com/metallicafan212/HarryPotterUnrealWiki/wiki/Main-Resources#other-kw-games) |
| Mobile Forces | 436 | 81-83/000, 69 | |
| Clive Barker's Undying | 420 | 72-85/000 | Versions 72 to 83 are not auto detected. |
| Thief: Deadly Shadows | 777:Flesh | 95/133 | Thanks in part to @hanfling; LinkedData not supported |
| Deus Ex: Invisible War | 777:Flesh | 95/069 | Thanks in part to @hanfling; LinkedData not supported |
|     |     |     |     |
|     |     |     |     |
| XIII | 829 | 100/058 |     |
| Tom Clancy's Splinter Cell | 829 | 100/017 | |
| Tom Clancy's Splinter Cell: Chaos Theory | 829 | 100/120:SCX, 175/000:ShadowStrike | (PC) demo version, Versus mode not supported |
| Tom Clancy's Splinter Cell: Double Agent | 829 | 100/167:SCX, 275/000:ShadowStrike | (PC) Offline mode version, Online mode not supported |
| Tom Clancy's Rainbow Six 3: Raven Shield | 600-927 | 118/012-014 | |
| Postal 2: Paradise Lost | 1417 | 118/002 | |
| Unreal Tournament 2003 | 1077-2225 | 119/025 | |
| Devastation | 600-? | 118-120/004-008 | |
| Unreal II: The Awakening | 829-2001 | 126/2609 | |
| Unreal II: eXpanded MultiPlayer | 2226 | 126/000 | Custom features are not decompiled |
| Desert Thunder | 2226 | 126/000 | |
| Land of the Dead: Road to Fiddler's Green | 2226 | 126/001 | |
| Unreal Tournament 2004 | 3120-3369 | 128/029 | |
| America's Army 2 | 3339 | 128/032:033 | 2.5, 2.6, 2.8 |
| America's Army (Arcade) | 3339 | 128/032 | 2.6 |
| Red Orchestra: Ostfront 41-45 | 3323-3369 | 128/029 | |
| Killing Floor | 3369 | 128/029 | |
| Battle Territory: Battery | 3369 | 128/029? | |
| Shadow Ops: Red Mercury | 2110 | 129/010 | |
| Vanguard: Saga of Heroes | 2226 | 129/035 | |
| Harry Potter and the Prisoner of Azkaban | 2226 | 129/000 | [Link](https://github.com/metallicafan212/HarryPotterUnrealWiki/wiki/Main-Resources#hp3) |
| Shrek 2 | 2226 | 129 | |
| Shark Tale | 2226 | 129/003 | |
| Lemony Snicket's A Series of Unfortunate Events | 2226 | 129/003 | |
| Swat 4 | 2226:Vengeance | 129/027 | |
| Stargate SG1: The Alliance | 4180 | 130/004 | |
| Tribes: Vengeance | 2226:Vengeance | 130/027 | |
| Bioshock | 2226:Vengeance | 130-141/056 | |
| Men of Valor | 926 | 137/000 | PC |
| Bioshock 2 | 2226:Vengeance | 143/059 | |
| Unreal Championship 2: Liandri Conflict | 3323 | 151/002 | [Third-party](https://forums.beyondunreal.com/threads/unreal-championship-2-script-decompiler-release.206036/) |
| The Chronicles of Spellborn | 3323 | 159/029 | |
| Duke Nukem Forever (2011) | Unknown | 156/036 | [Extraction is required](https://github.com/DaZombieKiller/MegaPackageExtractor) |
|     |     |     |     |
|     |     |     |     |
| RoboHordes | Unknown | 198-200/000 | |
| Tom Clancy's Rainbow Six: Vegas | Unknown | 241/066 | |
| Tom Clancy's Rainbow Six: Vegas 2 | Unknown | 241/071 | |
| Tom Clancy's EndWar | Unknown | 329/000 | |
| Roboblitz | 2306 | 369/006 |     |
| Stranglehold | 2605 | 375/025 | |
| Mass Effect (Xbox 360) | 2674 | 391/092 | Xenon |
| Medal of Honor: Airborne | 2859 | 421/011 |     |
| Frontlines: Fuel of War | 2917 | 433/052 | |
| Army of Two | 3004 | 445/079 | Overall quality has not been verified |
| Mortal Kombat Komplete Edition | 2605 | 472/046 |     |
| Stargate Worlds | 3004 | 486/007 |     |
| Gears of War | 3329 | 490/009 | |
| Mass Effect (Windows) | 3240 | 491/1008(~112) | PC |
| Huxley | 3240 | 496/016-023 | Contributed by @EDITzDev |
| Robert Ludlum's The Bourne Conspiracy | | 511/039 | |
| Transformers: War for Cybertron | | 511/144,145 | PC, PS3, Xenon |
| Unreal Tournament 3 | 3809 | 512/000 | |
| Mirrors Edge | 3716 | 536/043 |     |
| Alpha Protocol | 3857 | 539/091 |     |
| APB: All Points Bulletin | 3908 | 547/028-032 |     |
| X-Men Origins: Wolverine | 4206 | 568/101 | Overall quality has not been verified |
| Gears of War 2 | 4638 | 575/000 | |
| CrimeCraft | 4701 | 576/005 | |
| Batman: Arkham Asylum | 4701 | 576/21 | Contributed by @etkramer |
| Medal of Honor (2010) | 100075??? | 581/058 | Bad byte-codes |
| Singularity | 4869 | 584/126 | |
| MoonBase Alpha | 4947 | 587/000 | |
| Saw | Unknown | 584/003 | |
| The Exiled Realm of Arborea or TERA | 4206 | 610/014 |     |
| Monday Night Combat | 5697 | 638/000 |     |
| DC Universe Online | 5859 | 638/6405 | Contributed by @theFoof |
| Unreal Development Kit | 5860-12791 | 664-868 | |
| Blacklight: Tango Down | 6165 | 673/002 | |
| Dungeons & Dragons: Daggerdale | 6165 | 674/000 | |
| Dungeon Defenders | 6262 | 678/002 | |
| Mass Effect 3 (Windows) | 6383 | 684/194 | PC (platform must be set to `Console`) |
| Mass Effect: Legendary Edition (Windows) | 6383 | 684/171 | PC (platform must be set to `Console`) |
| Alice Madness Returns | 6760 | 690/000 |     |
| The Ball | 6699 | 706/000 | |
| Game of Thrones | 6829 | 727/000 | |
| Bioshock Infinite | 6829 | 727/075 |     |
| Bulletstorm | 7052 | 742/029 | |
| Red Orchestra 2: Heroes of Stalingrad | 7258 | 765/Unknown | |
| Rising Storm 2: Vietnam | 7258 | 765/771 | |
| Aliens: Colonial Marines | 4170 | 787/047 | |
| Infinity Blade 1 | 7595 | 788/001 | Console |
| [Dishonored](http://www.dishonored.com/) | 9099 | 801/030 |     |
| Tribes: Ascend | 7748 | 805/Unknown |     |
| Tony Hawk's Pro Skater HD | | | |
| Rock of Ages | 7748 | 805/000 |     |
| Batman: Arkham City | 7748 | 805/101 | |
| Batman: Arkham Origins | 7748 | 807/138 | Not verified |
| Sanctum | 7876 | 810/000 |     |
| AntiChamber | 7977 | 812/000 |     |
| Waves | 8171 | 813/000 |     |
| Super Monday Night Combat | 8364 | 820/000 |     |
| Gears of War 3 | 8653 | 828/000 | |
| Quantum Conundrum | 8623 | 832/32870 | |
| Borderlands | 4871 | Unknown |  |
| Borderlands 2 | 8623/0023 | 832/056 | |
| Borderlands: Game of the Year Enhanced | 5001/0001 | 832/058 | |
| Remember Me | 8623 | 832/021 |     |
| The Haunted: Hells Reach | 8788 | 841/000 |     |
| Asura's Wrath | 8788 | 841/000 | -zlib; platform needs to be set to console. |
| Blacklight Retribution | 8788-10499 | 841-864/002 |     |
| Infinity Blade 2 | 9059 | 842-864/001 | Console |
| Q.U.B.E | 8916 | 845/000 |     |
| DmC: Devil May Cry | 8916 | 845/004 | |
| XCOM: Enemy Unknown | 8916 | 845/059 | |
| XCOM 2: War of the Chosen | 8916 | 845/120 | Contributed by @robojumper |
| Gears of War: Judgement | 10566 | 846/000 |     |
| Transformers: Fall of Cybertron | 846(~587)/181 | |
| Deadpool | 846(~587)/181 | |
| InMomentum | 8980 | 848/000 |     |
| [Unmechanical](http://unmechanical.net/) | 9249 | 852/000 |     |
| Deadlight | 9375 | 854/000 |     |
| Land of the Dead | 9375 | 854/000 |     |
| Ravaged | 9641 | 859/000 |     |
| [The Five Cores](http://neeblagames.com/category/games/thefivecores/) | 9656 | 859/000 |     |
| Painkiller HD | 9953 | 860/000 |     |
| Chivalry: Medieval Warfare | 10246 | 860/000 | |
| Hawken | 10681 | 860/004 | |
| Rocket League | 10897 | 867/009 (868/032 has not been tested) | [Decryption required](https://github.com/AltimorTASDK/RLUPKTool) |
| Styx: Master of Shadows | 10499 | 860/004 | |
| Batman: Arkham Knight | | 863/32995 | Not verified  |
| Gigantic: Rampage Edition | 19100 | 867/010 | Contributed by @HyenaCoding |
| Infinity Blade 3 | | 868/000 | Console |
| Guilty Gear Xrd | 10246 | 868/003 | [Decryption required](https://github.com/gdkchan/GGXrdRevelatorDec) |
| Outlast | 12046 | 868/003 | |
| [Might & Magic Heroes VII](https://en.wikipedia.org/wiki/Might_%26_Magic_Heroes_VII) | 12161 | 868/004 | (Signature and custom features are not supported) |
| Bombshell | 11767 | 870/000 | |
| Orcs Must Die! Unchained | 20430 | 870/000 | |
| Gal\*Gun: Double Peace | 10897 | 871/000 | |
| Battleborn | 8623/1055 | 874/078 | |
| A Hat in Time | 12097 | 877-893/005 | Contributed by @Un-Drew |
| Blue Estate The Game | 10246 | 893/000 | |
| Shadow Complex Remastered | 10897 | 893/001 | |
| Borderlands 2 VR | 8637/0029 | 895/046 | |
| Soldier Front 2 | 6712 | 904/009 |     |
| Rise of the Triad | 10508 | Unknown |     |
| Sherlock Holmes: Crimes and Punishments | 10897 | Unknown | |
| Alien Rage | 7255 | Unknown |     |

**Beware, opening an unsupported package could crash your system! Make sure you have
saved everything before opening any file!**

**Note** UE3 production-ready packages are often **compressed** and must first be decompressed, [Unreal Package Decompressor](https://www.gildor.org/downloads) by **Gildor** is a tool that can decompress most packages for you; for some games you need a specialized decompressor, see for example [RLUPKTool](https://github.com/AltimorTASDK/RLUPKTool).

Want to add support for a game? See [adding support for new Unreal classes](https://github.com/EliotVU/Unreal-Library/wiki/Adding-support-for-new-Unreal-classes)

Do you know a game that is compatible but is not listed here? Click on the top right to edit this file!

## How to contribute

* Open an issue
* Or make a pull-request by creating a [fork](https://help.github.com/articles/fork-a-repo/) of this repository, create a new branch and commit your changes to that particular branch, so that I can easily merge your changes.

## Special thanks to

* Epic Games for [UDN: Packages](http://www.hypercoop.tk/infobase/archive/unrealtech/Packages.htm) (general package format)
* [Antonio Cordero Balcazar](https://github.com/acorderob) for [UTPT](https://www.acordero.org/projects/unreal-tournament-package-tool) (game support) and documentation (format)
* [Dmitry Jemerov](https://github.com/yole) for [unhood](https://github.com/yole/unhood) (early UE3 format)
* [Konstantin Nosov](https://github.com/gildor2) for providing help and [UE Viewer](http://www.gildor.org/en/projects/umodel) (game support)
* [Contributors](https://github.com/EliotVU/Unreal-Library/graphs/contributors)
