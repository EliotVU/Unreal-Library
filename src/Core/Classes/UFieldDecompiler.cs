#if DECOMPILE
using System.Diagnostics.Contracts;
using System;

namespace UELib.Core
{
    public partial class UField
    {
        protected string DecompileMeta()
        {
            return Meta != null ? Meta.Decompile() : String.Empty;
        }

        [Pure]protected string FetchToolTipAsComment()
        {
            var tag = Meta != null ? Meta.GetMetaTag( "ToolTip" ) : null;
            if( tag == null ) 
                return String.Empty;

            var comment = UDecompilingState.Tabs + "/** ";
            // Multiline comment?
            if( tag.TagValue.IndexOf( '\n' ) != -1 )
            {
                comment += " \r\n" + UDecompilingState.Tabs + " *" 
                           + tag.TagValue.Replace( "\n", "\n" + UDecompilingState.Tabs + " *" ) 
                           + "\r\n" + UDecompilingState.Tabs;
            }
            else
            {
                comment += tag.TagValue;
            }
            return comment + " */\r\n";
        }

        // Introduction of the change from intrinsic to native.
        private const uint NativeVersion = 69;
        // Introduction of the change from expands to extends.
        private const uint ExtendsVersion = 69;
        protected const uint PlaceableVersion = 69;

        protected string FormatNative()
        {
            return Package.Version >= NativeVersion ? "native" : "intrinsic";
        }

        protected string FormatExtends()
        {
            return Package.Version >= ExtendsVersion ? "extends" : "expands";
        }
    }
}
#endif