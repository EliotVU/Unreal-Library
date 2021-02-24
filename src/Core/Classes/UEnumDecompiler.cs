﻿#if DECOMPILE
using System;

namespace UELib.JsonDecompiler.Core
{
    public partial class UEnum
    {
        /// <summary>
        /// Decompiles this object into a text format of:
        ///
        /// enum NAME
        /// {
        ///     [ELEMENTS]
        /// };
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            return UDecompilingState.Tabs + FormatHeader() +
                UnrealConfig.PrintBeginBracket() +
                FormatNames() +
                UnrealConfig.PrintEndBracket()  + ";";
        }

        protected override string FormatHeader()
        {
            return "enum " + Name + DecompileMeta();
        }

        private string FormatNames()
        {
            string output = String.Empty;
            UDecompilingState.AddTabs( 1 );
            for( int index = 0; index < Names.Count; index++ )
            {
                var enumName = Names[index];
                output += "\r\n" + UDecompilingState.Tabs + enumName;
                if( index != Names.Count - 1 )
                {
                    output += ",";
                }
            }
            UDecompilingState.RemoveTabs( 1 );
            return output;
        }
    }
}
#endif