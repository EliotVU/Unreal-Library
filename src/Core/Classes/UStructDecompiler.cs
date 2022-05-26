#if DECOMPILE
using System;
using System.Linq;

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

        protected override string FormatHeader()
        {
            var output = $"struct {FormatFlags()}{Name}{(Super != null ? $" {FormatExtends()} {Super.Name}" : string.Empty)}";
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
            var output = string.Empty;
            if (StructFlags == 0)
            {
                return string.Empty;
            }

            if ((StructFlags & (uint)Flags.StructFlags.Native) != 0)
            {
                output += "native ";
            }

            if ((StructFlags & (uint)Flags.StructFlags.Export) != 0)
            {
                output += "export ";
            }

            if (Package.Version <= 128)
            {
                if ((StructFlags & (uint)Flags.StructFlags.Long) != 0)
                {
                    output += "long ";
                }
            }

            if ((StructFlags & (uint)Flags.StructFlags.Init) != 0 && Package.Version < 222)
            {
                output += "init ";
            }
            else if (HasStructFlag(Flags.StructFlags.Transient))
            {
                output += "transient ";
            }

            if (HasStructFlag(Flags.StructFlags.Atomic))
            {
                output += "atomic ";
            }

            if (HasStructFlag(Flags.StructFlags.AtomicWhenCooked))
            {
                output += "atomicwhencooked ";
            }

            if (HasStructFlag(Flags.StructFlags.Immutable))
            {
                output += "immutable ";
            }

            if (HasStructFlag(Flags.StructFlags.ImmutableWhenCooked))
            {
                output += "immutablewhencooked ";
            }

            if (HasStructFlag(Flags.StructFlags.StrictConfig))
            {
                output += "strictconfig ";
            }

            return output;
        }

        protected virtual string CPPTextKeyword => Package.Version < VCppText ? "cppstruct" : "structcpptext";

        protected string FormatCPPText()
        {
            if (CppText == null)
            {
                return string.Empty;
            }

            string output = $"\r\n{UDecompilingState.Tabs}{CPPTextKeyword}" +
                            UnrealConfig.PrintBeginBracket() + "\r\n" +
                            CppText.Decompile() +
                            UnrealConfig.PrintEndBracket() + "\r\n";
            return output;
        }

        protected string FormatConstants()
        {
            if (Constants == null || !Constants.Any())
                return string.Empty;

            var output = string.Empty;
            foreach (var scriptConstant in Constants)
            {
                output += $"\r\n{UDecompilingState.Tabs}{scriptConstant.Decompile()}";
            }

            return output +
                   "\r\n";
        }

        protected string FormatEnums()
        {
            if (Enums == null || !Enums.Any())
                return string.Empty;

            var output = string.Empty;
            foreach (var scriptEnum in Enums)
            {
                // And add a empty line between all enums!
                output += $"\r\n{scriptEnum.Decompile()}\r\n";
            }

            return output;
        }

        protected string FormatStructs()
        {
            if (Structs == null || !Structs.Any())
                return string.Empty;

            var output = string.Empty;
            foreach (var scriptStruct in Structs)
            {
                // And add a empty line between all structs!
                output += "\r\n"
                          + scriptStruct.Decompile()
                          + "\r\n";
            }

            return output;
        }

        protected string FormatProperties()
        {
            if (Variables == null || !Variables.Any())
                return string.Empty;

            var output = string.Empty;
            // Only for pure UStructs because UClass handles this on its own
            if (IsPureStruct())
            {
                output += FormatConstants() + FormatEnums() + FormatStructs();
            }

            // Don't use foreach, screws up order.
            foreach (var property in Variables)
            {
                // Fix for properties within structs
                output += "\r\n" +
                          property.PreDecompile() +
                          $"{UDecompilingState.Tabs}var";
                if (property.CategoryName != null && !property.CategoryName.IsNone())
                {
                    output += property.CategoryName == Name
                        ? "()"
                        : $"({property.CategoryName})";
                }
                output += $" {property.Decompile()};";
            }

            return output + "\r\n";
        }

        public string FormatDefaultProperties()
        {
            if (Default != null && Default != this)
            {
                Default.BeginDeserializing();
                Properties = Default.Properties;
            }

            if (Properties == null || !Properties.Any())
                return string.Empty;

            var output = string.Empty;
            string innerOutput;

            if (IsClassType("Class"))
            {
                output += "\r\n" +
                          "defaultproperties" +
                          "\r\n" +
                          "{" +
                          "\r\n";
            }
            else
            {
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
            catch (Exception e)
            {
                innerOutput = $"{UDecompilingState.Tabs}// {e.GetType().Name} occurred while decompiling properties!" +
                              "\r\n";
            }
            finally
            {
                UDecompilingState.RemoveTabs(1);
            }

            return $"{output}{innerOutput}{UDecompilingState.Tabs}}}";
        }

        protected string FormatLocals()
        {
            if (Locals == null || !Locals.Any())
                return string.Empty;

            var numParms = 0;
            var output = string.Empty;
            var lastType = string.Empty;
            for (var i = 0; i < Locals.Count; ++i)
            {
                string curType = Locals[i].GetFriendlyType();
                string nextType = ((i + 1) < Locals.Count
                    ? Locals[i + 1].GetFriendlyType()
                    : string.Empty);

                // If previous is the same as the one now then format the params as one line until another type is reached
                if (curType == lastType)
                {
                    output += Locals[i].Name +
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
                              + UDecompilingState.Tabs + "local " + Locals[i].Decompile() +
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
            return ByteCodeManager != null ? ByteCodeManager.Decompile() : string.Empty;
        }
    }
}
#endif