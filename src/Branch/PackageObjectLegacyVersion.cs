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
        LazyArrayAdded = 63,

        CharRemapAddedToUFont = 69,

        /// <summary>
        /// FIXME: Unknown version.
        /// </summary>
        CastStringSizeTokenDeprecated = 70,
        
        /// <summary>
        /// FIXME: Version, set 95 (Deus Ex: IW)
        /// </summary>
        PrimitiveCastTokenAdded = 95,
        
        
        KerningAddedToUFont = 119,
        FontPagesDisplaced = 122,
        
        UE3 = 184,
        RangeConstTokenDeprecated = UE3,

        /// <summary>
        /// Present in all released UE3 games (starting with RoboBlitz).
        /// 
        /// FIXME: Unknown version.
        /// </summary>
        IsLocalAddedToDelegateFunctionToken = 181,
        
        VerticalOffsetAddedToUFont = 506,
        CleanupFonts = 511,
        
        ProbeMaskReducedAndIgnoreMaskRemoved = 692,
        ForceScriptOrderAddedToUClass = 749,
        SuperReferenceMovedToUStruct = 756,
    }
}