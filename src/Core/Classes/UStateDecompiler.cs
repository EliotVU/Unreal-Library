#if DECOMPILE
using System;
using System.Collections.Generic;
using System.Linq;

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
            return HasStateFlag(Flags.StateFlags.Auto) ? "auto " : string.Empty;
        }

        private string GetSimulated()
        {
            return HasStateFlag(Flags.StateFlags.Simulated) ? "simulated " : string.Empty;
        }

        private string GetEdit()
        {
            return HasStateFlag(Flags.StateFlags.Editable) ? "()" : string.Empty;
        }

        protected override string FormatHeader()
        {
            var output = $"{GetAuto()}{GetSimulated()}state{GetEdit()} {Name}";
            if (Super != null && Super.Name != Name
                /* Not the same because when overriding states it automatic extends the parent state */)
            {
                output += $" {FormatExtends()} {Super.Name}";
            }

            return output;
        }

        private string FormatIgnores()
        {
            if (IgnoreMask == ulong.MaxValue || Functions == null || !Functions.Any())
            {
                return string.Empty;
            }

            var output = $"\r\n{UDecompilingState.Tabs}ignores ";
            var ignores = new List<string>();
            foreach (var func in Functions.Where(func => !func.HasFunctionFlag(Flags.FunctionFlags.Defined)))
            {
                ignores.Add(func.Name);
            }

            for (var i = 0; i < ignores.Count; ++i)
            {
                const int ignoresPerRow = 5;
                output += ignores[i] +
                          (
                              ignores[i] != ignores.Last()
                                  ? ", " +
                                    (
                                        i % ignoresPerRow == 0 && i >= ignoresPerRow
                                            ? "\r\n\t" + UDecompilingState.Tabs
                                            : string.Empty
                                    )
                                  : ";\r\n"
                          );
            }

            return ignores.Count > 0 ? output : string.Empty;
        }

        protected string FormatFunctions()
        {
            if (Functions == null || !Functions.Any())
                return string.Empty;

            // Remove functions from parent state, e.g. when overriding states.
            var formatFunctions = GetType() == typeof(UState)
                ? Functions.Where(f => f.HasFunctionFlag(Flags.FunctionFlags.Defined))
                : Functions;

            var output = string.Empty;
            foreach (var scriptFunction in formatFunctions)
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