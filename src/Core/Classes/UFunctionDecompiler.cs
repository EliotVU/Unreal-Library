#if DECOMPILE
using System;
using System.Linq;
using UELib.Branch;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UFunction
    {
        /// <summary>
        /// Decompiles this object into a text format of:
        ///
        /// [FLAGS] function NAME([VARIABLES])[;] [const]
        /// {
        ///     [LOCALS]
        ///
        ///     [CODE]
        /// } [META DATA]
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            string code = FormatCode();
            var body = $"{FormatHeader()}{code}{DecompileMeta()}";
            // Write a declaration only if code is empty.
            return string.IsNullOrEmpty(code)
                ? $"{body};"
                : body;
        }

        private string FormatFlags()
        {
            var output = string.Empty;
            var isNormalFunction = true;

            if (HasFunctionFlag(FunctionFlag.Private))
            {
                output += "private ";
            }
            else if (HasFunctionFlag(FunctionFlag.Protected))
            {
                output += "protected ";
            }

            if (Package.Version >= (uint)PackageObjectLegacyVersion.AddedDLLBindFeature &&
                HasFunctionFlag(FunctionFlag.DLLImport))
            {
                output += "dllimport ";
            }

            if (Package.Version > 180 && HasFunctionFlag(FunctionFlag.Net))
            {
                if (HasFunctionFlag(FunctionFlag.NetReliable))
                {
                    output += "reliable ";
                }
                else
                {
                    output += "unreliable ";
                }

                if (HasFunctionFlag(FunctionFlag.NetClient))
                {
                    output += "client ";
                }

                if (HasFunctionFlag(FunctionFlag.NetServer))
                {
                    output += "server ";
                }
            }

            if (HasFunctionFlag(FunctionFlag.Native))
            {
                output += NativeToken > 0 ? $"{FormatNative()}({NativeToken}) " : $"{FormatNative()} ";
            }

            if (HasFunctionFlag(FunctionFlag.Static))
            {
                output += "static ";
            }

            if (HasFunctionFlag(FunctionFlag.Final))
            {
                output += "final ";
            }
#if VENGEANCE
            if (Package.Build == BuildGeneration.Vengeance)
            {
                if (HasAnyFunctionFlags((ulong)Flags.FunctionFlags.VG_Overloaded))
                {
                    output += "overloaded ";
                }
            }
#endif
            // NoExport is no longer available in UE3+ builds,
            // - instead it is replaced with (FunctionFlags.OptionalParameters)
            // - as an indicator that the function has optional parameters.
            if (HasFunctionFlag(FunctionFlag.NoExport) && Package.Version <= 220)
            {
                output += "noexport ";
            }

#if AHIT
            if (Package.Build == UnrealPackage.GameBuild.BuildName.AHIT)
            {
                if (HasAnyFunctionFlags((ulong)Flags.FunctionFlags.AHIT_Optional))
                {
                    output += "optional "; // optional interface functions use this.
                }

                if (HasAnyFunctionFlags((ulong)Flags.FunctionFlags.AHIT_Multicast))
                {
                    output += "multicast ";
                }

                if (HasAnyFunctionFlags((ulong)Flags.FunctionFlags.AHIT_NoOwnerRepl))
                {
                    output += "NoOwnerReplication ";
                }
            }
#endif

            // FIXME: Version, added with one of the later UDK builds.
            if (Package.Version >= 500
#if AHIT
                // For AHIT, don't write these K2 specifiers, since they overlap with its custom flags.
                && Package.Build != UnrealPackage.GameBuild.BuildName.AHIT
#endif
               )
            {
                if (HasFunctionFlag(FunctionFlag.K2Call))
                {
                    output += "k2call ";
                }

                if (HasFunctionFlag(FunctionFlag.K2Override))
                {
                    output += "k2override ";
                }

                if (HasFunctionFlag(FunctionFlag.K2Pure))
                {
                    output += "k2pure ";
                }
            }
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (HasAnyFunctionFlags(0x20000000))
                {
                    output += "devexec ";
                }

                if (HasAnyFunctionFlags(0x4000000))
                {
                    output += "animevent ";
                }

                if (HasAnyFunctionFlags(0x1000000))
                {
                    output += "cached ";
                }

                if (HasAnyFunctionFlags(0x2000000))
                {
                    output += "encrypted ";
                }

                // Only if non-static?
                if (HasAnyFunctionFlags(0x800000))
                {
                    // Along with an implicit "native"
                    output += "indexed ";
                }
            }
#endif
            if (HasFunctionFlag(FunctionFlag.Invariant))
            {
                output += "invariant ";
            }

            if (HasFunctionFlag(FunctionFlag.Iterator))
            {
                output += "iterator ";
            }

            if (HasFunctionFlag(FunctionFlag.Latent))
            {
                output += "latent ";
            }

            if (HasFunctionFlag(FunctionFlag.Singular))
            {
                output += "singular ";
            }

            if (HasFunctionFlag(FunctionFlag.Simulated))
            {
                output += "simulated ";
            }

            if (HasFunctionFlag(FunctionFlag.Exec))
            {
                output += "exec ";
            }

            if (HasFunctionFlag(FunctionFlag.Event))
            {
                output += "event ";
                isNormalFunction = false;
            }

            if (IsDelegate())
            {
                output += "delegate ";
                isNormalFunction = false;
            }

            if (IsOperator())
            {
                if (IsPre())
                {
                    output += "preoperator ";
                }
                else if (IsPost())
                {
                    output += "postoperator ";
                }
                else
                {
                    output += $"operator({OperPrecedence}) ";
                }

                isNormalFunction = false;
            }

            // Don't add function if it's an operator or event or delegate function type!
            if (isNormalFunction)
            {
                output += "function ";
            }

#if AHIT
            // Needs to be after function/event/operator/etc.
            if (Package.Build == UnrealPackage.GameBuild.BuildName.AHIT &&
                HasAnyFunctionFlags((ulong)Flags.FunctionFlags.AHIT_EditorOnly))
            {
                output += "editoronly ";
            }
#endif

            return output;
        }

        public override string FormatHeader()
        {
            var output = string.Empty;
            // static function (string?:) Name(Parms)...
            if (FunctionFlags.HasFlag(FunctionFlag.Native))
            {
                // Output native declaration.
                output = $"// Export U{Outer.Name}::exec{Name}(FFrame&, void* const)\r\n{UDecompilingState.Tabs}";
            }

            string comment = FormatTooltipMetaData();
            string returnCode = ReturnProperty != null
                ? ReturnProperty.GetFriendlyType() + " "
                : string.Empty;

            output += comment +
                      FormatFlags() + returnCode + FriendlyName + FormatParms();
            if (FunctionFlags.HasFlag(FunctionFlag.Const))
            {
                output += " const";
            }

            return output;
        }

        private string FormatParms()
        {
            var parms = EnumerateFields<UProperty>()
                        .Where(field => field.IsParm() && !field.PropertyFlags.HasFlag(PropertyFlag.ReturnParm))
                        .ToList();

            if (parms.Count == 0)
            {
                return "()";
            }

            UByteCodeDecompiler? decompiler = null;
            bool hasOptionalData = HasOptionalParamData();
            if (hasOptionalData)
            {
                decompiler = new UByteCodeDecompiler(this);
                decompiler.Deserialize();
                decompiler.InitDecompile();

                decompiler.PushContext(new UByteCodeDecompiler.DecompilationContext(decompiler.Context, OuterMost<UClass>()));
            }

            var output = string.Empty;
            foreach (var parm in parms)
            {
                string parmCode = parm.Decompile();
                if (hasOptionalData && parm.PropertyFlags.HasFlag(PropertyFlag.OptionalParm))
                {
                    // Look for an assignment.
                    var defaultToken = decompiler.NextToken;
                    if (defaultToken is UByteCodeDecompiler.DefaultParameterToken)
                    {
                        var paramContext = new UByteCodeDecompiler.DecompilationContext(decompiler.Context, parm);

                        string defaultExpr = defaultToken.Decompile(decompiler, paramContext);
                        parmCode += $" = {defaultExpr}";
                    }
                }

                if (parm != parms.Last())
                {
                    output += $"{parmCode}, ";
                    continue;
                }

                output += parmCode;
            }

            return $"({output})";
        }

        private string FormatCode()
        {
            UDecompilingState.AddTabs(1);
            string locals = FormatLocals();
            if (locals != string.Empty)
            {
                locals += "\r\n";
            }

            string code;
            try
            {
                code = DecompileScript();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception thrown: {e} in {nameof(FormatCode)}");
                code = $"/*ERROR: {e}*/";
            }
            finally
            {
                UDecompilingState.RemoveTabs(1);
            }

            // Empty function!
            if (string.IsNullOrEmpty(locals) && string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            return UnrealConfig.PrintBeginBracket() + "\r\n" +
                   locals +
                   code +
                   UnrealConfig.PrintEndBracket();
        }
    }
}
#endif
