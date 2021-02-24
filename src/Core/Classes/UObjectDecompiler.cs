using System;

namespace UELib.JsonDecompiler.Core
{
    public partial class UObject : IUnrealDecompilable
    {
        /// <summary>
        /// Decompiles this Object into human-readable code
        /// </summary>
        public virtual string Decompile()
        {
            if( ShouldDeserializeOnDemand )
            {
                BeginDeserializing();
            }

            string output = $"{{\r\n\"Name\":\"{Name}\",\r\n\"Class\":\"{Class.Name}\",\r\n";
            UDecompilingState.AddTabs( 1 );
            try
            {
                output += DecompileProperties();
            }
            finally
            {
                UDecompilingState.RemoveTabs( 1 );
            }
            return $"{output}{UDecompilingState.Tabs}\r\n}}\r\n{UDecompilingState.Tabs}";
        }

        // Ment to be overriden!
        protected virtual string FormatHeader()
        {
            // Note:Dangerous recursive call!
            return Decompile();
        }

        protected string DecompileProperties()
        {
            if( Properties == null || Properties.Count == 0 )
                return UDecompilingState.Tabs + "null\r\n";

            string output = String.Empty;

            #if DEBUG
            //output += UDecompilingState.Tabs + "// Object Offset:" + UnrealMethods.FlagToString( (uint)ExportTable.SerialOffset ) + "\r\n";
            #endif

            for( int i = 0; i < Properties.Count; ++ i )
            {
                string propOutput = $"{Properties[i].Decompile()}{(i == Properties.Count -1 ? string.Empty : ",")}";

                // This is the first element of a static array
                if( i+1 < Properties.Count
                    && Properties[i+1].Name == Properties[i].Name
                    && Properties[i].ArrayIndex <= 0
                    && Properties[i+1].ArrayIndex > 0 )
                {
                    propOutput = propOutput.Insert( Properties[i].Name.Length + 3, "[\r\n" );
                }
                
                // This is the last element of a static array
                if( i > 0
                    && i+1 < Properties.Count
                    && Properties[i+1].Name != Properties[i].Name
                    && Properties[i].ArrayIndex > 0)
                {
                    if(i+1 < Properties.Count)
                    {
                        propOutput = propOutput.TrimEnd(',') + "],\r\n";
                    }
                    else
                    {
                        propOutput = propOutput.TrimEnd(',') + "]\r\n";
                    }
                }

                // FORMAT: 'DEBUG[TAB /* 0xPOSITION */] TABS propertyOutput + NEWLINE
                output += UDecompilingState.Tabs +
#if DEBUG_POSITIONS
            "/*" + UnrealMethods.FlagToString( (uint)Properties[i]._BeginOffset ) + "*/\t" +
#endif
                            propOutput + "\r\n";
            }
            return output;
        }
    }
}