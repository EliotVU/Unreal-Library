using System.Runtime.CompilerServices;

namespace UELib.Branch
{
    public enum PackageObjectLegacyVersion
    {
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
