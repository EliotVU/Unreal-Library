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

            var output = $"begin object name={Name} class={Class.Name}\r\n";
            UDecompilingState.AddTabs(1);
            try
            {
                output += DecompileProperties();
            }
            finally
            {
                UDecompilingState.RemoveTabs(1);
            }

            return $"{output}{UDecompilingState.Tabs}object end\r\n{UDecompilingState.Tabs}// Reference: {Class.Name}'{GetOuterGroup()}'";
        }

        protected virtual string FormatHeader()
        {
            // Note:Dangerous recursive call!
            return Decompile();
        }

        protected string DecompileProperties()
        {
            if (Properties == null || Properties.Count == 0)
                return UDecompilingState.Tabs + "// This object has no properties!\r\n";

            var output = string.Empty;
            for (var i = 0; i < Properties.Count; ++i)
            {
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