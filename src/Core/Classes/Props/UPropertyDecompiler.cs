using System.Globalization;
using UELib.Branch;
using UELib.Flags;
using UELib.UnrealScript;

namespace UELib.Core
{
    public partial class UProperty
    {
#if DECOMPILE
        // Called before the var () is printed.
        public virtual string PreDecompile()
        {
            return FormatTooltipMetaData();
        }

        public override string Decompile()
        {
            return FormatFlags() + GetFriendlyType()
                                 + " " + Name
                                 + FormatSize()
                                 + DecompileEditorData()
                                 + DecompileMeta();
        }

        // Post semicolon ";".
        public virtual string PostDecompile()
        {
            return default;
        }

        // FIXME: Rewrite without performing this many string copies, however this part of the decompilation process is not crucial.
        private string DecompileEditorData()
        {
            if (string.IsNullOrEmpty(EditorDataText))
                return string.Empty;

            string[] options = EditorDataText.TrimEnd('\n').Split('\n');
            string decodedOptions = string.Join(" ", options.Select(PropertyDisplay.FormatLiteral));
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                return " ?(" + decodedOptions + ")";
#endif
            return " " + decodedOptions;
        }

        private string FormatSize()
        {
            if (!IsArray)
            {
                return string.Empty;
            }

            string arraySizeDecl = ArrayEnum != null
                ? ArrayEnum.GetFriendlyType()
                : ArrayDim.ToString(CultureInfo.InvariantCulture);
            return $"[{arraySizeDecl}]";
        }

        private string FormatAccess()
        {
            var output = string.Empty;

            // none are true in StreamInteraction.uc???

            if (IsProtected())
            {
                output += "protected ";
            }
            else if (IsPrivate())
            {
                output += "private ";
            }

            return output;
        }

        public string FormatFlags()
        {
            // TODO: Just enumerate the flags and return them as a string.
            ulong copyFlags = PropertyFlags;
            var output = string.Empty;

            if (PropertyFlags == 0)
            {
                return FormatAccess();
            }

            if (HasPropertyFlag(PropertyFlag.AlwaysInit))
            {
                output += "init ";
            }

            /** Flags that are valid as parameters only */
            if (Outer is UFunction && HasPropertyFlag(PropertyFlag.Parm))
            {
                // Oldest attestation for R6 v241
                if (Package.Version > (uint)PackageObjectLegacyVersion.UE3)
                {
                    if (HasPropertyFlag(PropertyFlag.Const))
                    {
                        output += "const ";
                    }
                }

                if (HasPropertyFlag(PropertyFlag.CoerceParm))
                {
                    output += "coerce ";
                }

                if (HasPropertyFlag(PropertyFlag.OptionalParm))
                {
                    output += "optional ";
                }

                if (HasPropertyFlag(PropertyFlag.OutParm))
                {
                    output += "out ";
                }

                if (HasPropertyFlag(PropertyFlag.SkipParm))
                {
                    output += "skip ";
                }
            }
            else /** Not a function param. */
            {
                output += FormatAccess();


                if (HasPropertyFlag(PropertyFlag.PrivateWrite))
                {
                    output += "privatewrite ";
                }

                if (HasPropertyFlag(PropertyFlag.ProtectedWrite))
                {
                    output += "protectedwrite ";
                }

                if (HasPropertyFlag(PropertyFlag.RepNotify))
                {
                    output += "repnotify ";

                    // (pseudo syntax) For BattleBorn, and possible other builds?
                    if (RepNotifyFuncName.IsNone() == false)
                    {
                        output += $"({RepNotifyFuncName}) ";
                    }
                }

                if (HasPropertyFlag(PropertyFlag.NoClear))
                {
                    output += "noclear ";
                }

                if (HasPropertyFlag(PropertyFlag.NoImport))
                {
                    output += "noimport ";
                }

                if (HasPropertyFlag(PropertyFlag.DataBinding))
                {
                    output += "databinding ";
                }

                if (HasPropertyFlag(PropertyFlag.EditHide))
                {
                    output += "edithide ";
                }

                if (HasPropertyFlag(PropertyFlag.EditTextBox))
                {
                    output += "edittextbox ";
                }

                if (HasPropertyFlag(PropertyFlag.Interp))
                {
                    output += "interp ";
                }

                if (HasPropertyFlag(PropertyFlag.NonTransactional))
                {
                    output += "nontransactional ";
                }

                if (HasPropertyFlag(PropertyFlag.DuplicateTransient))
                {
                    output += "duplicatetransient ";
                    // Implies: Export, EditInline
                }

                if (HasPropertyFlag(PropertyFlag.EditorOnly))
                {
                    output += "editoronly ";
                }

                if (HasPropertyFlag(PropertyFlag.CrossLevelPassive))
                {
                    output += "crosslevelpassive ";
                }

                if (HasPropertyFlag(PropertyFlag.CrossLevelActive))
                {
                    output += "crosslevelactive ";
                }

                if (HasPropertyFlag(PropertyFlag.Archetype))
                {
                    output += "archetype ";
                }

                if (HasPropertyFlag(PropertyFlag.NotForConsole))
                {
                    output += "notforconsole ";
                }

                if (HasPropertyFlag(PropertyFlag.NotForFinalRelease))
                {
                    output += "notforfinalrelease ";
                }

                if (HasPropertyFlag(PropertyFlag.RepRetry))
                {
                    output += "repretry ";
                }

                // Instanced is only an alias for Export and EditInline.
                /*if( HasPropertyFlag( Flags.PropertyFlagsLO.Instanced ) )
                {
                    output += "instanced ";
                    copyFlags &= ~(ulong)Flags.PropertyFlagsLO.Instanced;

                    // Implies: Export, EditInline
                }*/

                if (HasPropertyFlag(PropertyFlag.SerializeText))
                {
                    output += "serializetext ";
                }
#if GIGANTIC
                if (Package.Build == UnrealPackage.GameBuild.BuildName.Gigantic)
                {
                    if ((PropertyFlags & (ulong)Branch.UE3.GIGANTIC.EngineBranchGigantic.PropertyFlags.JsonTransient) != 0)
                    {
                        // jsonserialize?
                        output += "jsontransient ";
                    }
                }
#endif
#if AHIT
                if (Package.Build == UnrealPackage.GameBuild.BuildName.AHIT)
                {
                    if (HasAnyPropertyFlags(0x4000UL)) // Serialize
                    {
                        output += "serialize ";
                        copyFlags &= ~0x4000UL;
                    }

                    if (HasAnyPropertyFlags(0x08000000U)) // Bitwise
                    {
                        output += "bitwise ";
                        copyFlags &= ~0x08000000U;
                    }
                }
#endif
                if (HasPropertyFlag(PropertyFlag.Native))
                {
                    output += FormatNative() + " ";
                }

                if (HasPropertyFlag(PropertyFlag.Const))
                {
                    output += "const ";
                }

                if (HasPropertyFlag(PropertyFlag.EditFixedSize))
                {
                    output += "editfixedsize ";
                }

                if (HasPropertyFlag(PropertyFlag.EditConstArray))
                {
                    output += "editconstarray ";
                }

                if (HasPropertyFlag(PropertyFlag.EditConst))
                {
                    output += "editconst ";
                }

#if UE2 && UT
                // Properties flagged with automated, automatically get those flags added by the compiler.
                if (Package.Build == BuildGeneration.UE2_5 && (PropertyFlags & (ulong)PropertyFlagsLO.Automated) != 0)
                {
                    output += "automated ";
                }
                else // Not Automated
#endif
                {
                    if (HasPropertyFlag(PropertyFlag.NoExport)
#if DNF
                        // 0x00800000 is CPF_Comment in DNF
                        && Package.Build != UnrealPackage.GameBuild.BuildName.DNF
#endif
                       )
                    {
                        output += "noexport ";
                    }
                    else if (HasPropertyFlag(PropertyFlag.ExportObject))
                    {
                        if (!HasPropertyFlag(PropertyFlag.DuplicateTransient))
                        {
                            output += "export ";
                        }
                    }

                    ulong editInline = PropertyFlags.GetFlag(PropertyFlag.EditInline);
                    ulong editInlineUse = PropertyFlags.GetFlag(PropertyFlag.EditInlineUse);
                    ulong editInlineNotify = PropertyFlags.GetFlag(PropertyFlag.EditInlineNotify);

#if DNF
                    if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                    {
                        editInline = 0x10000000;
                        editInlineUse = 0x40000000;
                        editInlineNotify = 0x80000000;
                    }
#endif

                    if ((PropertyFlags & editInline) != 0)
                    {
                        if ((PropertyFlags & editInlineUse) != 0)
                        {
                            copyFlags &= ~editInlineUse;
                            output += "editinlineuse ";
                        }
                        else if ((PropertyFlags & editInlineNotify) != 0)
                        {
                            copyFlags &= ~editInlineNotify;
                            output += "editinlinenotify ";
                        }
                        else if (!HasPropertyFlag(PropertyFlag.DuplicateTransient))
                        {
                            output += "editinline ";
                        }

                        copyFlags &= ~editInline;
                    }
                }

                if (HasPropertyFlag(PropertyFlag.EdFindable)
#if AHIT
                    && Package.Build != UnrealPackage.GameBuild.BuildName.AHIT
#endif
#if DNF
                    && Package.Build != UnrealPackage.GameBuild.BuildName.DNF
#endif
                   )
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EdFindable);
                    output += "edfindable ";
                }

                if (HasPropertyFlag(PropertyFlag.Deprecated)
#if DNF
                    && Package.Build != UnrealPackage.GameBuild.BuildName.DNF
#endif
                   )
                {
                    output += "deprecated ";
                }

                // It is important to check for global before checking config! first
                if (HasPropertyFlag(PropertyFlag.GlobalConfig))
                {
                    output += "globalconfig ";
                }
                else if (HasPropertyFlag(PropertyFlag.Config))
                {
#if XCOM2
                    if (ConfigName != null && !ConfigName.Value.IsNone())
                    {
                        output += "config(" + ConfigName + ") ";
                    }
                    else
                    {
#endif
                        output += "config ";
#if XCOM2
                    }
#endif
                }

                if (HasPropertyFlag(PropertyFlag.Localized))
                {
                    output += "localized ";
                }

#if UE2 && UT
                // Assuming UE2.5 (introduced with UT2004 but is also used in SG1)
                if (Package.Build == BuildGeneration.UE2_5)
                {
                    if (HasAnyPropertyFlags((ulong)PropertyFlagsLO.Cache))
                    {
                        copyFlags &= ~(ulong)PropertyFlagsLO.Cache;
                        output += "cache ";
                    }
                }
#endif

                if (HasPropertyFlag(PropertyFlag.Transient))
                {
                    output += "transient ";
                }

                if (HasPropertyFlag(PropertyFlag.Travel))
                {
                    output += "travel ";
                }

                if (HasPropertyFlag(PropertyFlag.Input))
                {
                    output += "input ";
                }
#if DNF
                if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                {
                    // Always erase 'CommentString'
                    copyFlags &= ~(uint)0x00800000;

                    if (HasAnyPropertyFlags(0x20000000))
                    {
                        output += "edfindable ";
                        copyFlags &= ~(uint)0x20000000;
                    }

                    if (HasAnyPropertyFlags(0x1000000))
                    {
                        output += "nontrans ";
                        copyFlags &= ~(uint)0x1000000;
                    }

                    if (HasAnyPropertyFlags(0x8000000))
                    {
                        output += "nocompress ";
                        copyFlags &= ~(uint)0x8000000;
                    }

                    if (HasAnyPropertyFlags(0x2000000))
                    {
                        output += $"netupdate({RepNotifyFuncName}) ";
                        copyFlags &= ~(uint)0x2000000;
                    }

                    if (HasAnyPropertyFlags(0x4000000))
                    {
                        output += "state ";
                        copyFlags &= ~(uint)0x4000000;
                    }

                    if (HasAnyPropertyFlags(0x100000))
                    {
                        output += "anim ";
                        copyFlags &= ~(uint)0x100000;
                    }
                }
#endif
            }

            if (!UnrealConfig.SuppressComments && TryGetUnknownFlags(copyFlags, PropertyFlags, out string undescribedFlags))
            {
                // Bring the flags to the front.
                output = undescribedFlags + output;
            }

            return output;
        }
#else
        public string FormatFlags() => string.Empty;
#endif
    }
}
