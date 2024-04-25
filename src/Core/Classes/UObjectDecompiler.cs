using System.Diagnostics;

namespace UELib.Core
{
    public partial class UObject : IUnrealDecompilable
    {
        /// <summary>
        /// Decompiles this Object into human-readable code
        /// </summary>
        public virtual string Decompile()
        {
            if (ShouldDeserializeOnDemand)
            {
                BeginDeserializing();
            }

            string output = $"// Reference: {GetReferencePath()}\r\n";

            if (ImportTable != null)
            {
                return output + $"\r\n{UDecompilingState.Tabs}// Cannot decompile an imported object";
            }

            output += UDecompilingState.Tabs;
            output += $"begin object name={Name}";
            // If null then we have a new sub-object (not an override)
            if (Archetype == null)
            {
                Debug.Assert(Class != null);
                output += $" class={Class.GetReferencePath()}";
            }
            else
            {
                // Commented out, too noisy but useful.
                //output += $" /*archetype={Archetype.GetReferencePath()}*/";
            }
            output += "\r\n";

            UDecompilingState.AddTabs(1);
            try
            {
                output += DecompileProperties();
            }
            finally
            {
                UDecompilingState.RemoveTabs(1);
            }

            return $"{output}" +
                   $"{UDecompilingState.Tabs}end object";
        }

        public virtual string FormatHeader()
        {
            return GetReferencePath();
        }

        protected string DecompileProperties()
        {
            if (Properties == null || Properties.Count == 0)
                return UDecompilingState.Tabs + "// This object has no properties!\r\n";

            var output = string.Empty;
            for (var i = 0; i < Properties.Count; ++i)
            {
                //output += $"{UDecompilingState.Tabs}// {Properties[i].Type}\r\n";
                string propOutput = Properties[i].Decompile();

                // This is the first element of a static array
                if (i + 1 < Properties.Count
                    && Properties[i + 1].Name == Properties[i].Name
                    && Properties[i].ArrayIndex <= 0
                    && Properties[i + 1].ArrayIndex > 0)
                {
                    propOutput = propOutput.Insert(Properties[i].Name.Length, "[0]");
                }

                // FORMAT: 'DEBUG[TAB /* 0xPOSITION */] TABS propertyOutput + NEWLINE
                output += UDecompilingState.Tabs + propOutput + "\r\n";
            }

            return output;
        }
    }
}
