using System.Collections.Generic;
using UELib.Annotations;
using UELib.Core;

namespace UELib
{
    public static class UDecompilingState
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static string Tabs = string.Empty;

        /// <summary>
        /// Objects that have been inlined (if true) in the current decompiling state.
        ///
        /// Internal because this is a hack patch to fix an issue where each object is inlined for every reference. 
        /// </summary>
        [CanBeNull] internal static Dictionary<UObject, bool> s_inlinedSubObjects = new Dictionary<UObject, bool>();

        public static void AddTabs(int count)
        {
            for (var i = 0; i < count; ++i)
            {
                Tabs += UnrealConfig.Indention;
            }
        }

        public static void AddTab()
        {
            Tabs += UnrealConfig.Indention;
        }

        public static void RemoveTabs(int count)
        {
            count *= UnrealConfig.Indention.Length;
            Tabs = count > Tabs.Length ? string.Empty : Tabs.Substring(0, (Tabs.Length) - count);
        }

        public static void RemoveTab()
        {
            // TODO: FIXME! This should not occur but it does in MutBestTimes.KeyConsumed(huge nested switch cases)
            if (Tabs.Length == 0)
                return;

            Tabs = Tabs.Substring(0, Tabs.Length - UnrealConfig.Indention.Length);
        }

        public static void RemoveSpaces(int count)
        {
            if (Tabs.Length < count)
            {
                Tabs = string.Empty;
                return;
            }

            Tabs = Tabs.Substring(0, Tabs.Length - count);
        }

        public static void ResetTabs()
        {
            Tabs = string.Empty;
        }

        public static string OffsetLabelName(uint offset)
        {
            return $"J0x{offset:X2}";
        }
    }
}
