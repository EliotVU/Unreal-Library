# [1.5.1](https://github.com/EliotVU/Unreal-Library/releases/tag/1.5.1)

* Fixed regression #74; The deprecated `UnrealConfig.CookedPlatform` field was ignored, which is still relevant for legacy-code.
* Updated auto-detected builds for Infinity Blade's series

# [1.5.0](https://github.com/EliotVU/Unreal-Library/releases/tag/1.5.0)

* 1ef135d Improved support for A Hat in Time (UE3), contributed by @Un-Drew

# [1.4.0](https://github.com/EliotVU/Unreal-Library/releases/tag/1.4.0)

Notable changes that affect UnrealScript output:

* Improved decompilation output of string and decimal literals.
* 5141285c Improved decompilation of delegate assignments (in a T3D context)
* 6d889c87 Added decompilation of optional parameter assignments e.g. `function MyFunction(option bool A = true);`.
* e55cfce0 Fixed decompilation with arrays of bools

Notable changes that affect support of games:

General deserialization fixes that affect all of UE1, UE2, and UE3 builds, as well as more specifically:

* 13460cca Support for Battleborn
* 4aff61fa Support for Duke Nukem Forever (2011)
* bce38c4f Support for Tom Clancy's Splinter Cell
* 809edaad Support for Harry Potter (UE1) data class {USound}
* b3e1489d Support for Devastation (UE2, 2003)
* 4780771a Support for Clive Barker's Undying (UE1) data classes {UClass, UTextBuffer, UPalette, USound}
* 01772a83 Support for Lemony Snicket's A Series of Unfortunate Events data class {UProperty}
* c4c1978d Fixed support for Dungeon Defenders 2 (versions 687-688/111-117)
* 86538e5d Fixed support for Vanguard: Saga of Heroes
* eb82dba5 Fixed support for Rocket League (version 868/003)
* 6ed6ed74 Fixed support for Hawken (version 860/002)
* b4b79773 Fixed ResizeStringToken for UE1 builds
* 3653f8e1 Fixed ReturnToken and BeginFunctionToken for UE1 builds (with a package version of 61)
* 9a659549 Fixed deserialization of Heritages for UE1 builds (with a package version older than 68)

Notable changes that affect various data structures:

* Improved detection of UComponent objects and class types.
* ea3c1aa5 Support for UE4 .uasset packages (earlier builds only)
* e37b8a12 Support for class {UTexture}, f1b74af1 {UPrimitive, UTexture2D and its derivatives} (UE3)
* aa5ca861 Support for classes: {UFont, UMultiFont}
* ab290b6c Support for types {UPolys, FPoly}
* 02bea77b Support for types {FUntypedBulkData} (UE3) and {TLazyArray} (UE1, UE2)
* 94e02927 Support for structures: {FPointRegion, FCoords, FPlane, FScale, FSphere, FRotator, FVector, FGuid, FBox, FLinearColor, FMatrix, FQuat, FRange, FRangeVector, FVector2D, FVector4}
* 09c76240 Support for class {USoundGroup} (UE2.5)

**Support for the data types listed above have only been implemented for the standard structure that Epic Games uses**

# [1.3.1](https://github.com/EliotVU/Unreal-Library/releases/tag/1.3.1)

Notable changes back-ported from 'develop' version 1.4.0:

* Added support for various data structures: FColor; and data class UBitmapMaterial (UE2)

* Improved support for Batman series
* Improved support for Transformers series
* e8308284 Fixed decompilation of primitive castings for UE1 builds
* ffaca763 Fixed decompilation of interface castings i.e. `InterfaceToBool` with `InterfaceToObject` (UE3).
* 3317e06a Fixed a missing package version check in UStateFrame (this affected some object classes that are usually found in map package files).

* 42783b16 Added the capability to override the interpreted version for packages of builds that are auto-detected.

# [1.3.0](https://github.com/EliotVU/Unreal-Library/releases/tag/1.3.0.0)

Notable changes:

* Support for Vengeance which includes BioShock 1 & 2, Swat4, and Tribes: Vengeance
* Support for Batman series (to the release branch, incomplete)
* Support for Thief: Deadly Shadows and Deus Ex: Invisible War
* Support for [America's Army 2 (and Arcade)](https://github.com/EliotVU/Unreal-Library/commit/4ae2ae2d25d8101495f0a7ae8d080156fd4bd10f)
* Support for Unreal II: eXpanded MultiPlayer
* Support for [The Chronicles of Spellborn](https://github.com/EliotVU/Unreal-Library/commit/0747049acfcf258efdcee746bf236243c87edc37)
* Improved general support for UE1 (Unreal 1), UE2 (Rainbow Six etc) & UE2.5, and UE3 (UDK etc)
* Fixes to DefaultProperties
