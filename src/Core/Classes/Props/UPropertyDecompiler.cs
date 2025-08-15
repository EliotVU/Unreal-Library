using System;
using System.Globalization;
using System.Linq;
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

            if (HasPropertyFlag(PropertyFlag.CommentString))
            {
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.CommentString);
            }

            if (HasPropertyFlag(PropertyFlag.Net))
            {
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Net);
            }

            if (HasPropertyFlag(PropertyFlag.DuplicateTransient))
            {
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.DuplicateTransient);
            }

            // Decompiling of this flag is put elsewhere.
            if (HasPropertyFlag(PropertyFlag.Editable))
            {
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Editable);
            }

            if (HasPropertyFlag(PropertyFlag.Component))
            {
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Component);
            }

            if (HasPropertyFlag(PropertyFlag.AlwaysInit))
            {
                output += "init ";
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.AlwaysInit);
            }

            if (HasPropertyFlag(PropertyFlag.CtorLink))
            {
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.CtorLink);
            }

            /** Flags that are valid as parameters only */
            if (Outer is UFunction && HasPropertyFlag(PropertyFlag.Parm))
            {
                copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Parm);
                // Oldest attestation for R6 v241
                if (Package.Version > (uint)PackageObjectLegacyVersion.UE3)
                {
                    if (HasPropertyFlag(PropertyFlag.Const))
                    {
                        output += "const ";
                        copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Const);
                    }
                }

                if (HasPropertyFlag(PropertyFlag.CoerceParm))
                {
                    output += "coerce ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.CoerceParm);
                }

                if (HasPropertyFlag(PropertyFlag.OptionalParm))
                {
                    output += "optional ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.OptionalParm);
                }

                if (HasPropertyFlag(PropertyFlag.OutParm))
                {
                    output += "out ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.OutParm);
                }

                if (HasPropertyFlag(PropertyFlag.SkipParm))
                {
                    output += "skip ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.SkipParm);
                }

                if (HasPropertyFlag(PropertyFlag.ReturnParm))
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.ReturnParm);
                }

                // Remove implied flags from GUIComponents
                if (HasPropertyFlag(PropertyFlag.ExportObject))
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.ExportObject);
                }

                if (HasPropertyFlag(PropertyFlag.EditInline))
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EditInline);
                }
            }
            else /** Not a function param. */
            {
                output += FormatAccess();


                if (HasPropertyFlag(PropertyFlag.PrivateWrite))
                {
                    output += "privatewrite ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.PrivateWrite);
                }

                if (HasPropertyFlag(PropertyFlag.ProtectedWrite))
                {
                    output += "protectedwrite ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.ProtectedWrite);
                }

                if (HasPropertyFlag(PropertyFlag.RepNotify))
                {
                    output += "repnotify ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.RepNotify);
                }

                if (HasPropertyFlag(PropertyFlag.NoClear))
                {
                    output += "noclear ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.NoClear);
                }

                if (HasPropertyFlag(PropertyFlag.NoImport))
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.NoImport);
                    output += "noimport ";
                }

                if (HasPropertyFlag(PropertyFlag.DataBinding))
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.DataBinding);
                    output += "databinding ";
                }

                if (HasPropertyFlag(PropertyFlag.EditHide))
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EditHide);
                    output += "edithide ";
                }

                if (HasPropertyFlag(PropertyFlag.EditTextBox))
                {
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EditTextBox);
                    output += "edittextbox ";
                }

                if (HasPropertyFlag(PropertyFlag.Interp))
                {
                    output += "interp ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Interp);
                }

                if (HasPropertyFlag(PropertyFlag.NonTransactional))
                {
                    output += "nontransactional ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.NonTransactional);
                }

                if (HasPropertyFlag(PropertyFlag.DuplicateTransient))
                {
                    output += "duplicatetransient ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.DuplicateTransient);

                    // Implies: Export, EditInline
                }

                if (HasPropertyFlag(PropertyFlag.EditorOnly))
                {
                    output += "editoronly ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EditorOnly);
                }

                if (HasPropertyFlag(PropertyFlag.CrossLevelPassive))
                {
                    output += "crosslevelpassive ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.CrossLevelPassive);
                }

                if (HasPropertyFlag(PropertyFlag.CrossLevelActive))
                {
                    output += "crosslevelactive ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.CrossLevelActive);
                }

                if (HasPropertyFlag(PropertyFlag.Archetype))
                {
                    output += "archetype ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Archetype);
                }

                if (HasPropertyFlag(PropertyFlag.NotForConsole))
                {
                    output += "notforconsole ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.NotForConsole);
                }

                if (HasPropertyFlag(PropertyFlag.RepRetry))
                {
                    output += "repretry ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.RepRetry);
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
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.SerializeText);
                }
#if GIGANTIC
                if (Package.Build == UnrealPackage.GameBuild.BuildName.Gigantic)
                {
                    if ((PropertyFlags & (ulong)Branch.UE3.GIGANTIC.EngineBranchGigantic.PropertyFlags.JsonTransient) != 0)
                    {
                        // jsonserialize?
                        output += "jsontransient ";
                        copyFlags &= ~(ulong)Branch.UE3.GIGANTIC.EngineBranchGigantic.PropertyFlags.JsonTransient;
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
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Native);
                }

                if (HasPropertyFlag(PropertyFlag.Const))
                {
                    output += "const ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Const);
                }

                if (HasPropertyFlag(PropertyFlag.EditFixedSize))
                {
                    output += "editfixedsize ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EditFixedSize);
                }

                if (HasPropertyFlag(PropertyFlag.EditConstArray))
                {
                    output += "editconstarray ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EditConstArray);
                }

                if (HasPropertyFlag(PropertyFlag.EditConst))
                {
                    output += "editconst ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.EditConst);
                }

#if UE2 && UT
                // Properties flagged with automated, automatically get those flags added by the compiler.
                if (Package.Build == BuildGeneration.UE2_5 && (PropertyFlags & (ulong)PropertyFlagsLO.Automated) != 0)
                {
                    output += "automated ";
                    copyFlags &= ~((ulong)PropertyFlagsLO.Automated
                                   | (ulong)PropertyFlagsLO.EditInlineUse
                                   | (ulong)PropertyFlagsLO.EditInlineNotify
                                   | (ulong)PropertyFlagsLO.EditInline
                                   | (ulong)PropertyFlagsLO.NoExport
                                   | (ulong)PropertyFlagsLO.ExportObject);
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
                        copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.NoExport);
                    }
                    else if (HasPropertyFlag(PropertyFlag.ExportObject))
                    {
                        if (!HasPropertyFlag(PropertyFlag.DuplicateTransient))
                        {
                            output += "export ";
                        }

                        copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.ExportObject);
                    }

                    ulong editInline = (ulong)PropertyFlagsLO.EditInline;
                    ulong editInlineUse = (ulong)PropertyFlagsLO.EditInlineUse;
                    ulong editInlineNotify = (ulong)PropertyFlagsLO.EditInlineNotify;

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
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Deprecated);
                }

                // It is important to check for global before checking config! first
                if (HasPropertyFlag(PropertyFlag.GlobalConfig))
                {
                    output += "globalconfig ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.GlobalConfig);
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Config);
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
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Config);
                }

                if (HasPropertyFlag(PropertyFlag.Localized))
                {
                    output += "localized ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Localized);
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
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Transient);
                }

                if (HasPropertyFlag(PropertyFlag.Travel))
                {
                    output += "travel ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Travel);
                }

                if (HasPropertyFlag(PropertyFlag.Input))
                {
                    output += "input ";
                    copyFlags &= ~PropertyFlags.GetFlag(PropertyFlag.Input);
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

            // Local's may never output any of their implied flags!
            if (!IsParm() && Super != null
                          && string.Compare(Super.GetClassName(), "Function", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return string.Empty;
            }

            // alright...
            //return "/*" + UnrealMethods.FlagToString( PropertyFlags ) + "*/ " + output;
            return copyFlags != 0 ? "/*" + UnrealMethods.FlagToString(copyFlags) + "*/ " + output : output;
        }
#else
        public string FormatFlags() => string.Empty;
#endif
    }
}
