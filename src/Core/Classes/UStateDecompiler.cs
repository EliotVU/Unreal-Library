#if DECOMPILE
using UELib.Flags;

namespace UELib.Core
{
    public partial class UState
    {
        /// <summary>
        /// Decompiles this object into a text format of:
        ///
        /// [FLAGS] state[()] NAME [extends NAME]
        /// {
        ///     [ignores Name[,Name];]
        ///
        ///     [FUNCTIONS]
        ///
        /// [STATE CODE]
        /// };
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            string content = FormatHeader() + UnrealConfig.PrintBeginBracket();
            UDecompilingState.AddTabs(1);

            string locals = FormatLocals();
            if (locals != string.Empty)
            {
                content += "\r\n" + locals;
            }

            content += FormatIgnores() +
                       FormatConstants() +
                       FormatFunctions() +
                       DecompileScript();
            UDecompilingState.RemoveTabs(1);
            content += UnrealConfig.PrintEndBracket();
            return content;
        }

        private string GetAuto()
        {
            return HasStateFlag(StateFlag.Auto) ? "auto " : string.Empty;
        }

        private string GetSimulated()
        {
            return HasStateFlag(StateFlag.Simulated) ? "simulated " : string.Empty;
        }

        private string GetEdit()
        {
            return HasStateFlag(StateFlag.Editable) ? "()" : string.Empty;
        }

        private string FormatStateFlags()
        {
            ulong copyFlags = StateFlags;
            if (copyFlags == 0)
            {
                return string.Empty;
            }

            string output = $"{GetAuto()}{GetSimulated()}";

            if (!UnrealConfig.SuppressComments && TryGetUnknownFlags(copyFlags, StateFlags, out string undescribedFlags))
            {
                output += undescribedFlags;
            }

            return output;
        }

        public override string FormatHeader()
        {
            var output = $"{FormatStateFlags()}state{GetEdit()} {Name}";
            if (Super != null && Super.Name != Name
                /* Not the same because when overriding states it automatic extends the parent state */)
            {
                output += $" {FormatExtends()} {Super.Name}";
            }

            return output;
        }

        private string FormatIgnores()
        {
            if (IgnoreMask == ulong.MaxValue)
            {
                return string.Empty;
            }

            var ignoredFunctions = EnumerateFields<UFunction>()
                                   .Where(field => !field.FunctionFlags.HasFlag(FunctionFlag.Defined))
                                   .ToList();

            if (ignoredFunctions.Count == 0)
            {
                return string.Empty;
            }

            var output = $"\r\n{UDecompilingState.Tabs}ignores ";
            for (var i = 0; i < ignoredFunctions.Count; ++i)
            {
                const int ignoresPerRow = 5;
                output += ignoredFunctions[i].Name +
                          (
                              ignoredFunctions[i] != ignoredFunctions.Last()
                                  ? ", " +
                                    (
                                        i % ignoresPerRow == 0 && i >= ignoresPerRow
                                            ? "\r\n\t" + UDecompilingState.Tabs
                                            : string.Empty
                                    )
                                  : ";\r\n"
                          );
            }

            return output;
        }

        protected string FormatFunctions()
        {
            // Remove functions from parent state, e.g. when overriding states.
            var definedFunctions = GetType() == typeof(UState)
                ? EnumerateFields<UFunction>()
                    .Where(field => field.FunctionFlags.HasFlag(FunctionFlag.Defined))
                : EnumerateFields<UFunction>();

            var output = string.Empty;
            foreach (var scriptFunction in definedFunctions.Reverse())
            {
                try
                {
                    string functionOutput = $"\r\n{UDecompilingState.Tabs}{scriptFunction.Decompile()}\r\n";
                    output += functionOutput;
                }
                catch (Exception e)
                {
                    output += $"\r\n{UDecompilingState.Tabs}// F:{scriptFunction.Name} E:{e}";
                }
            }

            return output;
        }
    }
}
#endif
