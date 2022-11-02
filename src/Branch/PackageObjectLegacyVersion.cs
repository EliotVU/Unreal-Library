using System.Runtime.CompilerServices;

namespace UELib.Branch
{
    public enum PackageObjectLegacyVersion
    {
        /// <summary>
        /// This is one particular update with A LOT of general package changes.
        /// </summary>
        ReturnExpressionAddedToReturnToken = 62,

        SphereExtendsPlane = 62,
        LazyArraySkipCountToSkipOffset = 62,

        CharRemapAddedToUFont = 69,

        /// <summary>
        /// FIXME: Unknown version.
        /// </summary>
        CastStringSizeTokenDeprecated = 70,
        
        PanUVRemovedFromPoly = 78,
        
        CompMipsDeprecated = 84,

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
        
        /// <summary>
        /// Present in all released UE3 games (starting with RoboBlitz).
        /// 
        /// FIXME: Unknown version.
        /// </summary>
        IsLocalAddedToDelegateFunctionToken = 181,
        
        UE3 = 184,
        
        // FIXME: Version
        RangeConstTokenDeprecated = UE3,
        
        // FIXME: Version
        FastSerializeStructs = UE3,
        
        // FIXME: Version
        EnumTagNameAddedToBytePropertyTag = UE3,

        // 227 according to the GoW client
        FixedVerticesToArrayFromPoly = 227,

        // FIXME: Not attested in the GoW client, must have been before v321
        LightMapScaleRemovedFromPoly = 300,

        // FIXME: Not attested in the GoW client, must have been before v321
        ShadowMapScaleAddedToPoly = 300,
        
        // 321 according to the GoW client
        ElementOwnerAddedToUPolys = 321,
        
        // 417 according to the GoW client
        LightingChannelsAddedToPoly = 417,

        VerticalOffsetAddedToUFont = 506,
        CleanupFonts = 511,

        LightmassAdded = 600,
        UProcBuildingReferenceAddedToPoly = 606,
        
        EnumNameAddedToBytePropertyTag = 633,
        
        LightmassShadowIndirectOnlyOptionAdded = 652,
        LightmassExplicitEmissiveLightRadiusAdded = 636,
        PolyRulesetVariationTypeChangedToName = 670,

        BoolValueToByteForBoolPropertyTag = 673,

        ProbeMaskReducedAndIgnoreMaskRemoved = 692,
        ForceScriptOrderAddedToUClass = 749,
        SuperReferenceMovedToUStruct = 756,
    }
}