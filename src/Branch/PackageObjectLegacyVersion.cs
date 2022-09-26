using System.Runtime.CompilerServices;

namespace UELib.Branch
{
    public enum PackageObjectLegacyVersion
    {
        /// <summary>
        /// FIXME: Unknown version.
        /// </summary>
        CastStringSizeTokenDeprecated = 70,
        
        /// <summary>
        /// FIXME: Version, set 95 (Deus Ex: IW)
        /// </summary>
        PrimitiveCastTokenAdded = 95,
        
        UE3 = 184,
        RangeConstTokenDeprecated = UE3,

        /// <summary>
        /// Present in all released UE3 games (starting with RoboBlitz).
        /// 
        /// FIXME: Unknown version.
        /// </summary>
        IsLocalAddedToDelegateFunctionToken = 181,
        ProbeMaskReducedAndIgnoreMaskRemoved = 692,
        ForceScriptOrderAddedToUClass = 749,
        SuperReferenceMovedToUStruct = 756,
    }
}