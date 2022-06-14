#if DECOMPILE
namespace UELib.Core
{
    public partial class UField
    {
        public string DecompileMeta()
        {
            return MetaData == null ? string.Empty : MetaData.Decompile();
        }

        public string FormatTooltipMetaData()
        {
            string tooltipValue = null;
            MetaData?.Tags.TryGetValue("ToolTip", out tooltipValue);
            if (tooltipValue == null)
            {
                return string.Empty;
            }

            var comment = $"{UDecompilingState.Tabs}/** ";
            // Multiline comment?
            if (tooltipValue.IndexOf('\n') != -1)
            {
                comment +=
                    " \r\n" +
                    $"{UDecompilingState.Tabs} *{tooltipValue.Replace("\n", "\n" + UDecompilingState.Tabs + " *")}" +
                    $"\r\n{UDecompilingState.Tabs}";
            }
            else
            {
                comment += tooltipValue;
            }

            return $"{comment} */\r\n";
        }

        // Introduction of the change from intrinsic to native.
        private const uint NativeVersion = 69;

        // Introduction of the change from expands to extends.
        private const uint ExtendsVersion = 69;
        protected const uint PlaceableVersion = 69;

        protected string FormatNative()
        {
            return Package.Version >= NativeVersion ? "native" : "intrinsic";
        }

        protected string FormatExtends()
        {
            return Package.Version >= ExtendsVersion ? "extends" : "expands";
        }
    }
}
#endif