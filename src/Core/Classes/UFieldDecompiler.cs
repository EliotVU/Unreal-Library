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
            if (MetaData?.Tags?.TryGetValue(UnrealName.Tooltip, out tooltipValue) != true)
            {
                return string.Empty;
            }

            string comment = $"{UDecompilingState.Tabs}/** ";
            // Multiline comment?
            if (tooltipValue!.IndexOf('\n') != -1)
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
