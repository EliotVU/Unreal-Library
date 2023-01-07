using System.Runtime.CompilerServices;

namespace UELib.Branch
{
    public enum PackageObjectLegacyVersion
    {
        Undefined = 0,
        
        /// <summary>
        /// This is one particular update with A LOT of general package changes.
        /// </summary>
        ReturnExpressionAddedToReturnToken = 62,

        SphereExtendsPlane = 62,
        LazyArraySkipCountChangedToSkipOffset = 62,

        /// <summary>
        /// This should mark the first approximated version with dynamic arrays that are accessible using UnrealScript.
        /// 
        /// FIXME: Version, generally not accessible in Unreal Engine 1 except for some, so we'll map the tokens for v62.
        /// </summary>
        DynamicArrayTokensAdded = 62,

        CharRemapAddedToUFont = 69,

        /// <summary>
        /// FIXME: Unknown version.
        /// </summary>
        CastStringSizeTokenDeprecated = 70,

        PanUVRemovedFromPoly = 78,

        CompMipsDeprecated = 84,

        // FIXME: Version, attested as of UE2
        DynamicArrayInsertTokenAdded = 95,

        /// <summary>
        /// FIXME: Version, set 95 (Deus Ex: IW)
        /// </summary>
        PrimitiveCastTokenAdded = 95,

        LightMapScaleAddedToPoly = 106,

        KerningAddedToUFont = 119,
        FontPagesDisplaced = 122,

        // The estimated version changes that came after the latest known UE2 build.
        TextureDeprecatedFromPoly = 170,
        MaterialAddedToPoly = 170,

        UE3 = 178,
        CompactIndexDeprecated = 178,

        // FIXME: Version, def not <= 178, found in GoW but no version check, so this approximation should do :)
        TemplateDataAddedToUComponent = 200,

        DisplacedScriptPropertiesWithClassDefaultObject = 200,

        /// <summary>
        /// Present in all released UE3 games (starting with RoboBlitz).
        /// 
        /// FIXME: Unknown version.
        /// </summary>
        IsLocalAddedToDelegateFunctionToken = 181,

        // FIXME: Version
        RangeConstTokenDeprecated = UE3,

        // FIXME: Version
        FastSerializeStructs = UE3,

        // FIXME: Version
        EnumTagNameAddedToBytePropertyTag = UE3,

        // 227 according to the GoW client
        FixedVerticesToArrayFromPoly = 227,

        // Thanks to @https://www.gildor.org/ for reverse-engineering the lazy-loader version changes.
        LazyLoaderFlagsAddedToLazyArray = 251,
        StorageSizeAddedToLazyArray = 254,
        L8AddedToLazyArray = 260,
        LazyArrayReplacedWithBulkData = 266,

        // 267 according to the GoW client,
        // -- albeit the exact nature is not clear
        // -- whether if this indicates the addition of such an ObjectFlag or just the conditional test.
        ClassDefaultCheckAddedToTemplateName = 267,
        
        ComponentGuidDeprecated = 273,

        /// <summary>
        /// Some properties like SizeX, SizeY, Format have been displaced to ScriptProperties.
        /// </summary>
        DisplacedUTextureProperties = 297,

        // FIXME: Not attested in the GoW client, must have been before v321
        LightMapScaleRemovedFromPoly = 300,

        // FIXME: Not attested in the GoW client, must have been before v321
        ShadowMapScaleAddedToPoly = 300,

        // 321 according to the GoW client
        ElementOwnerAddedToUPolys = 321,

        NetObjectsAdded = 322,

        NumberAddedToName = 343,

        // 417 according to the GoW client
        LightingChannelsAddedToPoly = 417,

        // FIXME: Version, not attested in (RoboBlitz v369, but attested in GoW v490).
        SkipSizeAddedToArrayFindTokenIntrinsics = 400,

        // FIXME: Version, not attested in (GoW v490)
        SkipSizeAddedToArrayTokenIntrinsics = 491,

        VerticalOffsetAddedToUFont = 506,
        CleanupFonts = 511,

        AddedTextureFileCacheGuidToTexture2D = 567,

        LightmassAdded = 600,
        UProcBuildingReferenceAddedToPoly = 606,

        EnumNameAddedToBytePropertyTag = 633,

        LightmassExplicitEmissiveLightRadiusAdded = 636,

        // FIXME: Version
        EndTokenAppendedToArrayTokenIntrinsics = 649,
        LightmassShadowIndirectOnlyOptionAdded = 652,
        PolyRulesetVariationTypeChangedToName = 670,

        BoolValueToByteForBoolPropertyTag = 673,

        AddedPVRTCToUTexture2D = 674,

        ProbeMaskReducedAndIgnoreMaskRemoved = 692,
        ForceScriptOrderAddedToUClass = 749,
        SuperReferenceMovedToUStruct = 756,

        AddedATITCToUTexture2D = 857,
        AddedETCToUTexture2D = 864,
    }
}
