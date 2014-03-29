using System;
using System.Collections.Generic;
using System.Linq;

namespace UELib
{
    public static class UnrealExtensions
    {
        // .TFC - Texture File Cache

        public const string UnrealCodeExt = ".uc";
        public const string UnrealFlagsExt = ".UPKG";

        public static readonly string[] ScriptExt 		= { ".u", ".t3u" };
        public static readonly string[] TextureExt		= { ".utx" };
        public static readonly string[] SoundExt 		= { ".uax", ".umx" };
        public static readonly string[] MeshExt 		= { ".usx", ".upx", ".ugx" };
        public static readonly string[] AnimExt 		= { ".ukx" };
        public static readonly string[] CacheExt 		= { ".uxx" };
        // UT2004, UDK, Unreal, Red Orchestra Map
        public static readonly string[] MapExt 			=
        { 
            ".ut2", ".udk", ".unr", ".rom", ".un2", ".aao",
            ".run", ".sac", ".xcm", ".nrf", ".wot", ".scl", 
            ".dvs", ".rsm", ".ut3", ".umap" 
        };
        public static readonly string[] SaveExt 		= { ".uvx", ".md5", ".usa", ".ums", ".rsa", ".sav" };
        public static readonly string[] PackageExt 		= { ".upk", ".uasset" };
        public static readonly string[] ModExt 			= { ".umod", ".ut2mod", ".ut4mod" };

        public static string FormatUnrealExtensionsAsFilter()
        {
            string extensions = String.Empty;
            var exts = FormatUnrealExtensionsAsList();
            foreach( string ext in exts )
            {
                extensions += "*" + ext;
                if( ext != exts.Last() )
                {
                    extensions += ";";
                }
            }
            return "All Unreal Files(" + extensions + ")|" + extensions;
        }

        public static List<string> FormatUnrealExtensionsAsList()
        {
            var exts = new List<string>
            (
                ScriptExt.Length + 
                TextureExt.Length + 
                SoundExt.Length + 
                MeshExt.Length + 
                AnimExt.Length + 
                CacheExt.Length + 
                MapExt.Length + 
                SaveExt.Length +
                PackageExt.Length
            );

            exts.AddRange( ScriptExt );
            exts.AddRange( TextureExt );
            exts.AddRange( SoundExt );
            exts.AddRange( MeshExt );
            exts.AddRange( AnimExt );
            exts.AddRange( CacheExt );
            exts.AddRange( MapExt );
            exts.AddRange( SaveExt );
            exts.AddRange( PackageExt );
            return exts;
        }
    }
}
