using System.Runtime.CompilerServices;

namespace UELib.Branch
{
    public enum PackageObjectLegacyVersion
    {
        [Discardable]
        GameRoboblitz = 369,

        /// <summary>
        /// Not attested in the oldest UE3 game Roboblitz
        /// 
        /// FIXME: Unknown version
        /// </summary>
        StructReferenceAddedToStructMember = GameRoboblitz + 1,

        [Discardable]
        GameFFOW = 433,

        /// <summary>
        /// First attested in FFOW (v433), but not MKKE (v472, non standard).
        /// 
        /// FIXME: Unknown version
        /// </summary>
        IsModificationAddedToStructMember = GameFFOW,
    }
}
