using System.Runtime.CompilerServices;

namespace UELib.Branch
{
    public enum PackageObjectLegacyVersion
    {
        Undefined = 0,

        /// <summary>
        /// The lowest version that is supported.
        /// </summary>
        LowestVersion = 61,

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

        HeritageTableDeprecated = 68,

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

        SerializeStructTags = 118,
        KerningAddedToUFont = 119,
        AddedCppTextToUStruct = 120,
        FontPagesDisplaced = 122,

        // FIXME: Version 138 (UC2)? (134 according to Advent Rising) Cheating with 160 to avoid issues with DNF and Spellborn
        AddedFuncMapToUState = 160,

        // FIXME: Version 138 (UC2)? (133 according to Advent Rising) Cheating with 160 to avoid issues with DNF and Spellborn
        MovedFriendlyNameToUFunction = 160,

        // The estimated version changes that came after the latest known UE2 build.
        TextureDeprecatedFromPoly = 170,
        MaterialAddedToPoly = 170,

        UE3 = 178,

        /// <summary>
        /// FIXME: Version 145 (UC2)? (144 according to Advent Rising)
        ///
        /// Generally the type was deprecated with 178, but the serialization change from 1-5 bytes to UInt32 was applied much earlier.
        /// </summary>
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

        // 196 according to EndWar and R6 Vegas
        AddedComponentGuid = 196,

        // 200 (RoboHordes, EndWar, R6Vegas)
        RemovedMinAlignmentFromUStruct = 200,

        // 208 according to EndWar
        PackageImportsDeprecated = 208,

        // 210 according to EndWar
        AddedComponentTemplatesToUClass = 210,

        // 219 according to EndWar
        DisplacedScriptPropertiesWithClassDefaultObject = 219,

        /// <summary>
        ///     And ComponentMap
        /// </summary>
        ArchetypeAddedToExports = 220,

        // 220, but doesn't appear to be true for RoboHordes(198,200)
        PropertyFlagsSizeExpandedTo64Bits = 190,

        // 222 according to EndWar
        AddedInterfacesFeature = 222,

        // 223 according to EndWar
        RefactoredPropertyTags = 223,

        // FIXME: Version, def not <= 178, not found in RoboHordes (198,200), but found in GoW without a version check, so this approximation should do :)
        // Set to 224 to skip EndWar (223)
        TemplateDataAddedToUComponent = 224,

        // 227 according to the GoW client
        FixedVerticesToArrayFromPoly = 227,

        /// <summary>
        /// FIXME: Version
        /// Not attested with EndWar (223) and R6Vegas (241)
        /// </summary>
        AddedStructFlagsToScriptStruct = 242,
        AddedEngineVersion = 245,

        ExportFlagsAddedToExports = 247,
        ComponentClassBridgeMapDeprecated = 248,
        AddedTotalHeaderSize = 249,
        SerialSizeConditionRemoved = 249,

        // Thanks to @https://www.gildor.org/ for reverse-engineering the lazy-loader version changes.
        LazyLoaderFlagsAddedToLazyArray = 251,
        StorageSizeAddedToLazyArray = 254,
        PackageNameAddedToLazyArray = 260,
        LazyArrayReplacedWithBulkData = 266,

        ComponentTemplatesDeprecated = 267,

        // 267 according to the GoW client,
        // -- albeit the exact nature is not clear
        // -- whether if this indicates the addition of such an ObjectFlag or just the conditional test.
        ClassDefaultCheckAddedToTemplateName = 267,

        AddedFolderName = 269,

        ComponentGuidDeprecated = 273,
        ClassGuidDeprecated = 276,
        AddedCookerVersion = 277,

        /// <summary>
        ///     Structs marked with 'Immutable' should use binary serialization.
        /// </summary>
        AddedImmutableStructs = 278,

        /// <summary>
        ///    Structs that extend a struct marked with 'Immutable' should not use binary serialization.
        /// </summary>
        StructsShouldNotInheritImmutable = 279,

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

        UpdatedDecalMaterial = 312,

        // 321 according to the GoW client
        ElementOwnerAddedToUPolys = 321,

        NetObjectCountAdded = 322,

        AddedXenonSoundData = 327,

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

        /// <summary>
        ///     Oldest attest MOHA (v421), but not MKKE (v472, non standard)
        ///     FIXME: Unknown version
        /// </summary>
        IsCopyAddedToStructMember = GameStranglehold + 1,

        AddedPS3SoundData = 376,

        /// <summary>
        /// And deprecated FileType from USoundNodeWave.
        /// </summary>
        AddedPCSoundData = 380,

        AddedChannelsSoundInfo = 385,
        AddedChannelCountSoundInfo = 390,

        AddedArrayEnumToUProperty = 401,

        AddedDependsTable = 415,

        // 417 according to the GoW client
        LightingChannelsAddedToPoly = 417,

        DisplacedSoundChannelProperties = 420,

        [Discardable] GameFFOW = 433, // Engine Version: 2917

        /// <summary>
        ///     Oldest attest FFOW (v433), but not MKKE (v472, non standard)
        ///     FIXME: Unknown version
        /// </summary>
        IsModificationAddedToStructMember = GameFFOW,

        AddedPackageSource = 482,

        /// <summary>
        ///     Invalid for Stargate Worlds.
        /// </summary>
        PackageFlagsAddedToExports = 475,

        [Discardable] GameGOWPC = 490,
        [Discardable] GameHuxley = 496,

        /// <summary>
        ///     FIXME: Version, not attested in (Huxley v496)
        /// </summary>
        SkipSizeAddedToArrayTokenIntrinsics = GameHuxley + 1,

        VerticalOffsetAddedToUFont = 506,
        CleanupFonts = 511,

        AddedAdditionalPackagesToCook = 516,

        /// <summary>
        ///     FIXME: Version, not versioned in any assembly; but, generally coincides with 541.
        /// </summary>
        ChangedUMetaDataObjectPathToReference = 541,

        ComponentMapDeprecated = 543,

        /// <summary>
        ///     Added with <see cref="ClassGuidDeprecated" />
        /// </summary>
        ClassPlatformFlagsDeprecated = 547,

        StateFrameLatentActionReduced = 566,

        AddedTextureFileCacheGuidToTexture2D = 567,

        AddedThumbnailTable = 584,

        AddedDingoSoundData = 593,
        AddedOrbisSoundData = 594,

        AddedObjectClassNameToThumbnail = 597,

        LightmassAdded = 600,

        AddedDontSortCategoriesToUClass = 603,

        UProcBuildingReferenceAddedToPoly = 606,

        AddedEditorDataToUSoundClass = 613,

        AddedDominantLightShadowMapToDominantDirectionalLightComponent = 617,

        AddedImportExportGuidsTable = 623,

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

        AddedDominantLightShadowMapToUDominantSpotLightComponent = 682,

        ProbeMaskReducedAndIgnoreMaskRemoved = 691,
        ForceScriptOrderAddedToUClass = 749,
        SuperReferenceMovedToUStruct = 756,

        AddedTextureAllocations = 767,

        AddedClassGroupsToUClass = 789,
        AddedNativeClassNameToUClass = 813,

        // 829 according to Borderlands2
        RemovedConvexVolumes = 829,

        AddedWiiUSoundData = 845,
        AddedIPhoneSoundData = 851,
        AddedFlashSoundData = 854,
        AddedATITCToUTexture2D = 857,
        AddedQualityMaskToUMaterial = 858,
        AddedETCToUTexture2D = 864,

        Next,
        HighestVersion = Next - 1,
    }
}
