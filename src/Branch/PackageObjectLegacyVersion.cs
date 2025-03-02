using System.Runtime.CompilerServices;

namespace UELib.Branch
{
    public enum PackageObjectLegacyVersion
    {
        Undefined = 0,

        /// <summary>
        ///     FIXME: Version 61 is the lowest package version I know that supports StateFlags.
        /// </summary>
        AddedStateFlagsToUState = 61,

        /// <summary>
        ///     This should mark the first approximated version with dynamic arrays that are accessible using UnrealScript.
        ///     FIXME: Version, generally not accessible in Unreal Engine 1 except for some, so we'll map the tokens for v62.
        /// </summary>
        DynamicArrayTokensAdded = 62,

        /// <summary>
        ///     Mixed changes.
        /// </summary>
        Release62 = 62,
        ReturnExpressionAddedToReturnToken = 62,
        SphereExtendsPlane = 62,
        LazyArraySkipCountChangedToSkipOffset = 62,

        /// <summary>
        ///     Mixed changes.
        /// </summary>
        Release64 = 64,

        CharRemapAddedToUFont = 69,

        /// <summary>
        ///     FIXME: Unknown version.
        /// </summary>
        CastStringSizeTokenDeprecated = 70,

        PanUVRemovedFromPoly = 78,

        CompMipsDeprecated = 84,

        // FIXME: Version, attested as of UE2
        DynamicArrayInsertTokenAdded = 95,

        /// <summary>
        ///     FIXME: Version, set 95 (Deus Ex: IW)
        /// </summary>
        PrimitiveCastTokenAdded = 95,

        AddedHideCategoriesToUClass = 99,

        LightMapScaleAddedToPoly = 106,

        KerningAddedToUFont = 119,
        AddedCppTextToUStruct = 120,
        FontPagesDisplaced = 122,

        // FIXME: Version 138? Cheating with 160 to avoid issues with DNF and Spellborn
        AddedFuncMapToUState = 160,

        // FIXME: Version 138? Cheating with 160 to avoid issues with DNF and Spellborn
        MovedFriendlyNameToUFunction = 160,

        // The estimated version changes that came after the latest known UE2 build.
        TextureDeprecatedFromPoly = 170,
        MaterialAddedToPoly = 170,

        UE3 = 178,
        CompactIndexDeprecated = 178,

        /// <summary>
        ///     Present in all released UE3 games (starting with RoboBlitz).
        ///     FIXME: Unknown version.
        /// </summary>
        IsLocalAddedToDelegateFunctionToken = 181,

        // FIXME: Version 128-178
        AddedDelegateSourceToUDelegateProperty = 185,

        AddedAutoExpandCategoriesToUClass = 185,
        ClassDependenciesDeprecated = 186,

        // FIXME: Version
        RangeConstTokenDeprecated = UE3,

        // FIXME: Version 118?
        FastSerializeStructs = UE3,

        // FIXME: Version
        EnumTagNameAddedToBytePropertyTag = UE3,

        // FIXME: Version
        DisplacedHideCategories = UE3,

        /// <summary>
        /// FIXME: Version
        /// No version check in RoboHordes (200)
        /// 
        /// Deprecated with <seealso cref="ComponentMapDeprecated"/>
        /// </summary>
        AddedComponentMapToExports = UE3,

        // Added somewhere between 186 ... 230
        // 189 according to RoboHordes
        AddedStateStackToUStateFrame = 189,

        ObjectFlagsSizeExpandedTo64Bits = 195,

        // FIXME: Version, def not <= 178, not found in RoboHordes (198,200), but found in GoW without a version check, so this approximation should do :)
        TemplateDataAddedToUComponent = 201,

        // 208 according to EndWar
        PackageImportsDeprecated = 208,

        // 210 according to EndWar
        AddedComponentTemplatesToUClass = 210,

        // 219 according to EndWar
        DisplacedScriptPropertiesWithClassDefaultObject = 219,

        ArchetypeAddedToExports = 220,

        /// <summary>
        /// FIXME: Version
        /// No version check in all of RoboHordes (200), EndWar (223), and R6Vegas (241)
        /// </summary>
        PropertyFlagsSizeExpandedTo64Bits = UE3,

        // 222 according to EndWar
        AddedInterfacesFeature = 222,

        // 223 according to EndWar
        RefactoredPropertyTags = 223,

        // 227 according to the GoW client
        FixedVerticesToArrayFromPoly = 227,

        /// <summary>
        /// FIXME: Version
        /// Not attested with EndWar (223) and R6Vegas (241)
        /// </summary>
        AddedStructFlagsToScriptStruct = 242,

        ExportFlagsAddedToExports = 247,
        ComponentClassBridgeMapDeprecated = 248,
        SerialSizeConditionRemoved = 249,

        // Thanks to @https://www.gildor.org/ for reverse-engineering the lazy-loader version changes.
        LazyLoaderFlagsAddedToLazyArray = 251,
        StorageSizeAddedToLazyArray = 254,
        L8AddedToLazyArray = 260,
        LazyArrayReplacedWithBulkData = 266,

        ComponentTemplatesDeprecated = 267,

        // 267 according to the GoW client,
        // -- albeit the exact nature is not clear
        // -- whether if this indicates the addition of such an ObjectFlag or just the conditional test.
        ClassDefaultCheckAddedToTemplateName = 267,

        ComponentGuidDeprecated = 273,
        ClassGuidDeprecated = 276,

        InterfaceClassesDeprecated = 288,

        AddedConvexVolumes = 294,

        /// <summary>
        ///     Some properties like SizeX, SizeY, Format have been displaced to ScriptProperties.
        /// </summary>
        DisplacedUTextureProperties = 297,

        // FIXME: Not attested in the GoW client, must have been before v321
        LightMapScaleRemovedFromPoly = 300,

        // FIXME: Not attested in the GoW client, must have been before v321
        ShadowMapScaleAddedToPoly = 300,

        // 321 according to the GoW client
        ElementOwnerAddedToUPolys = 321,

        NetObjectCountAdded = 322,

        CompressionAdded = 334,

        NumberAddedToName = 343,
        
        [Discardable] GameGOW = 374, // Engine Version: 2451
        [Discardable] GameStranglehold = 375, // Engine Version: 2605

        /// <summary>
        ///     Possibly attested first with Stranglehold (v375)
        ///     FIXME: Version 375-491; Delegate source type changed from Name to Object
        /// </summary>
        ChangedDelegateSourceFromNameToObject = GameStranglehold,

        /// <summary>
        ///     Not attested in GoW v374), oldest attests (v375,v421)
        ///     FIXME: Version
        /// </summary>
        SkipSizeAddedToArrayFindTokenIntrinsics = GameGOW + 1,

        /// <summary>
        ///     Not attested in GoW (v374), oldest attests (v375,v421)
        ///     FIXME: Unknown version
        /// </summary>
        StructReferenceAddedToStructMember = GameStranglehold,

        // 417 according to the GoW client
        LightingChannelsAddedToPoly = 417,

        AddedArrayEnumToUProperty = 401,

        /// <summary>
        ///     Oldest attest MOHA (v421), but not MKKE (v472, non standard)
        ///     FIXME: Unknown version
        /// </summary>
        IsCopyAddedToStructMember = GameStranglehold + 1,

        [Discardable] GameFFOW = 433, // Engine Version: 2917

        /// <summary>
        ///     Oldest attest FFOW (v433), but not MKKE (v472, non standard)
        ///     FIXME: Unknown version
        /// </summary>
        IsModificationAddedToStructMember = GameFFOW,

        [Discardable] GameGOWPC = 490,
        [Discardable] GameHuxley = 496,

        /// <summary>
        ///     FIXME: Version, not attested in (Huxley v496)
        /// </summary>
        SkipSizeAddedToArrayTokenIntrinsics = GameHuxley + 1,

        VerticalOffsetAddedToUFont = 506,
        CleanupFonts = 511,

        ComponentMapDeprecated = 543,

        /// <summary>
        ///     Added with <see cref="ClassGuidDeprecated" />
        /// </summary>
        ClassPlatformFlagsDeprecated = 547,

        StateFrameLatentActionReduced = 566,

        AddedTextureFileCacheGuidToTexture2D = 567,

        LightmassAdded = 600,

        AddedDontSortCategoriesToUClass = 603,

        UProcBuildingReferenceAddedToPoly = 606,

        EnumNameAddedToBytePropertyTag = 633,

        LightmassExplicitEmissiveLightRadiusAdded = 636,

        AddedDataScriptSizeToUStruct = 639,

        // FIXME: Version
        EndTokenAppendedToArrayTokenIntrinsics = 649,
        LightmassShadowIndirectOnlyOptionAdded = 652,

        AddedDLLBindFeature = 655,

        PolyRulesetVariationTypeChangedToName = 670,

        BoolValueToByteForBoolPropertyTag = 673,

        AddedPVRTCToUTexture2D = 674,

        ProbeMaskReducedAndIgnoreMaskRemoved = 691,
        ForceScriptOrderAddedToUClass = 749,
        SuperReferenceMovedToUStruct = 756,

        AddedClassGroupsToUClass = 789,
        AddedNativeClassNameToUClass = 813,

        AddedATITCToUTexture2D = 857,
        AddedETCToUTexture2D = 864,
    }
}
