namespace UELib
{
    public static class UDecompilingState
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage",
            "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static string Tabs = string.Empty;

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