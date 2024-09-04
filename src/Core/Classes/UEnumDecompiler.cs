#if DECOMPILE
namespace UELib.Core
{
    public partial class UEnum
    {
        /// <summary>
        /// Decompiles this object into a text format of:
        ///
        /// enum NAME
        /// {
        ///     [ELEMENTS]
        /// };
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            return UDecompilingState.Tabs + FormatHeader() +
                   UnrealConfig.PrintBeginBracket() +
                   FormatNames() +
                   UnrealConfig.PrintEndBracket() + ";";
        }

        public override string FormatHeader()
        {
            return $"enum {Name}{DecompileMeta()}";
        }

        private string FormatNames()
        {
            string output = string.Empty;
            UDecompilingState.AddTabs(1);

            for (int index = 0; index < Names.Count; index++)
            {
                var enumTagName = Names[index];
                string enumTagText = enumTagName.ToString();

                if (index != Names.Count - 1)
                {
                    enumTagText += ",";
                }

                if (!UnrealConfig.SuppressComments)
                {
                    enumTagText = enumTagText.PadRight(32, ' ');
                    enumTagText += $"// {index}";
                }

                output += "\r\n" + UDecompilingState.Tabs + enumTagText;
            }

            UDecompilingState.RemoveTabs(1);

            return output;
        }
    }
}
#endif
