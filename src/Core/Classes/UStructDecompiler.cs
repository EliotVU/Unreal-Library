#if DECOMPILE
using UELib.Branch;
using UELib.Decompiler;

namespace UELib.Core
{
    public partial class UStruct
    {
        /// <summary>
        /// Decompiles this object into a text format of:
        ///
        /// struct [FLAGS] NAME [extends NAME]
        /// {
        ///     [STRUCTCPPTEXT]
        ///
        ///     [CONSTS]
        ///
        ///     [ENUMS]
        ///
        ///     [STRUCTS]
        ///
        ///     [VARIABLES]
        ///
        ///     [STRUCTDEFAULTPROPERTIES]
        /// };
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            string content = UDecompilingState.Tabs + FormatHeader() +
                             UnrealConfig.PrintBeginBracket();
            UDecompilingState.AddTabs(1);
            string cpptext = FormatCPPText();
            string props = FormatProperties();

            string defProps = FormatDefaultProperties();
            if (defProps.Length != 0)
            {
                defProps += "\r\n";
            }

            UDecompilingState.RemoveTabs(1);
            content += cpptext + props + defProps;
            if (content.EndsWith("\r\n"))
            {
                content = content.TrimEnd('\r', '\n');
            }

            return content + UnrealConfig.PrintEndBracket() + ";";
        }

        public override string FormatHeader()
        {
            var output =
                $"struct {FormatFlags()}{Name}{(Super != null ? $" {FormatExtends()} {Super.Name}" : string.Empty)}";
            string metaData = DecompileMeta();
            if (metaData != string.Empty)
            {
                output = $"{metaData}" +
                         $"\r\n{UDecompilingState.Tabs}{output}";
            }

            return output;
        }

        private string FormatFlags()
        {
            ulong copyFlags = StructFlags;
            var output = string.Empty;
            if (StructFlags == 0)
            {
                return string.Empty;
            }

            if (HasStructFlag(Flags.StructFlag.Native))
            {
                output += "native ";
            }

            if (HasStructFlag(Flags.StructFlag.Export))
            {
                output += "export ";
            }

            if (HasStructFlag(Flags.StructFlag.Long))
            {
                output += "long ";
            }

            if (HasStructFlag(Flags.StructFlag.Init))
            {
                output += "init ";
            }

            if (HasStructFlag(Flags.StructFlag.Transient))
            {
                output += "transient ";
            }

            if (HasStructFlag(Flags.StructFlag.Atomic))
            {
                output += "atomic ";
            }

            if (HasStructFlag(Flags.StructFlag.AtomicWhenCooked))
            {
                output += "atomicwhencooked ";
            }

            if (HasStructFlag(Flags.StructFlag.Immutable))
            {
                output += "immutable ";
            }

            if (HasStructFlag(Flags.StructFlag.ImmutableWhenCooked))
            {
                output += "immutablewhencooked ";
            }

            if (HasStructFlag(Flags.StructFlag.StrictConfig))
            {
                output += "strictconfig ";
            }

            if (!UnrealConfig.SuppressComments && TryGetUnknownFlags(copyFlags, StructFlags, out string undescribedFlags))
            {
                output += undescribedFlags;
            }

            return output;
        }

        protected virtual string CPPTextKeyword =>
            Package.Version < (uint)PackageObjectLegacyVersion.AddedCppTextToUStruct ? "cppstruct" : "structcpptext";

        protected string FormatCPPText()
        {
            if (CppText == null)
            {
                return string.Empty;
            }

            string output = $"\r\n{UDecompilingState.Tabs}{CPPTextKeyword}" +
                            UnrealConfig.PrintBeginBracket() + "\r\n";

            try
            {
                output += CppText.Decompile();
            }
            catch (Exception e) // may occur when the CppText content are corrupted.
            {
                output += $"{UDecompilingState.Tabs}/* {e} */ // occurred while decompiling CppText!" +
                          "\r\n";
            }
            finally
            {
                output += UnrealConfig.PrintEndBracket() + "\r\n";
            }

            return output;
        }

        protected string FormatConstants()
        {
            var output = string.Empty;
            foreach (var scriptConstant in EnumerateFields<UConst>().Reverse())
            {
                output += $"\r\n{UDecompilingState.Tabs}{scriptConstant.Decompile()}";
            }

            return output != string.Empty
                ? output + "\r\n"
                : string.Empty;
        }

        protected string FormatEnums()
        {
            var output = string.Empty;
            foreach (var scriptEnum in EnumerateFields<UEnum>().Reverse())
            {
                // And add an empty line between all enums!
                output += $"\r\n{scriptEnum.Decompile()}\r\n";
            }

            return output;
        }

        protected string FormatStructs()
        {
            var output = string.Empty;
            foreach (var scriptStruct in EnumerateFields<UStruct>()
                                         .Where(field => field.IsPureStruct())
                                         .Reverse())
            {
                // And add an empty line between all structs!
                output += "\r\n"
                          + scriptStruct.Decompile()
                          + "\r\n";
            }

            return output;
        }

        protected string FormatProperties()
        {
            var output = string.Empty;

            // Only for pure UStructs because UClass handles this on its own
            if (IsPureStruct())
            {
                output += FormatConstants() + FormatEnums() + FormatStructs();
            }

            foreach (var property in EnumerateFields<UProperty>())
            {
                bool isCompilerGenerated = IsCompilerAutoGeneratedHelper.Visit((dynamic)property);

                // Fix for properties within structs
                output += "\r\n";
                // MetaData like comments.
                string preOutput = property.PreDecompile();
                output += preOutput;
                output += UDecompilingState.Tabs;
                if (isCompilerGenerated)
                {
                    output += "//";
                }

                output += "var";
                if (!property.CategoryName.IsNone())
                {
                    output += property.CategoryName == Name
                        ? "()"
                        : $"({property.CategoryName})";
                }

                output += $" {property.Decompile()};";

                string postOutput = property.PostDecompile();
                if (!string.IsNullOrEmpty(postOutput))
                {
                    output += postOutput;
                }
            }

            return output != string.Empty
                ? output + "\r\n"
                : string.Empty;
        }

        public string FormatDefaultProperties()
        {
            var output = string.Empty;
            string innerOutput;

            if (this is UClass)
            {
                output += "\r\n" +
                          "defaultproperties" +
                          "\r\n" +
                          "{" +
                          "\r\n";
            }
            else
            {
                // Output nothing for an empty 'structdefaultproperties' block.
                if (Properties.Count == 0)
                {
                    return string.Empty;
                }

                output += "\r\n" +
                          $"{UDecompilingState.Tabs}structdefaultproperties" +
                          "\r\n" +
                          $"{UDecompilingState.Tabs}{{" +
                          "\r\n";
            }

            UDecompilingState.AddTabs(1);
            try
            {
                innerOutput = DecompileProperties();
            }
            finally
            {
                UDecompilingState.RemoveTabs(1);
            }

            return $"{output}{innerOutput}{UDecompilingState.Tabs}}}";
        }

        protected string FormatLocals()
        {
            var locals = EnumerateFields<UProperty>()
                .Where(prop => !prop.IsParm())
                .ToList();

            if (!locals.Any())
                return string.Empty;

            var numParms = 0;
            var output = string.Empty;
            var lastType = string.Empty;
            for (var i = 0; i < locals.Count; ++i)
            {
                string curType = locals[i].GetFriendlyType();
                string nextType = (i + 1 < locals.Count
                    ? locals[i + 1].GetFriendlyType()
                    : string.Empty);

                // If previous is the same as the one now then format the params as one line until another type is reached
                if (curType == lastType)
                {
                    output += locals[i].Name +
                              (
                                  curType == nextType
                                      ? ((numParms >= 5 && numParms % 5 == 0)
                                          ? ",\r\n\t" + UDecompilingState.Tabs
                                          : ", "
                                      )
                                      : ";\r\n"
                              );
                    ++numParms;
                }
                else
                {
                    output += (numParms >= 5 ? "\r\n" : string.Empty)
                              + UDecompilingState.Tabs + "local " + locals[i].Decompile() +
                              (
                                  (nextType != curType || string.IsNullOrEmpty(nextType))
                                      ? ";\r\n"
                                      : ", "
                              );
                    numParms = 1;
                }

                lastType = curType;
            }

            return output;
        }

        protected string DecompileScript()
        {
            var decompiler = new UByteCodeDecompiler(this);
            return decompiler.Decompile();
        }
    }
}
#endif
