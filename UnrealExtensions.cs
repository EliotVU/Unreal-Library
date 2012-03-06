using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UELib
{
	public static class UnrealExtensions
	{
		// .TFC - Texture File Cache

		public static readonly string UnrealCodeExt		= ".uc";
		public static readonly string UnrealFlagsExt	= ".UPKG";

		public static readonly string[] ScriptExt 		= new string[]{ ".u", ".t3u" };
		public static readonly string[] TextureExt		= new string[]{ ".utx" };
		public static readonly string[] SoundExt 		= new string[]{ ".uax", ".umx" };
		public static readonly string[] MeshExt 		= new string[]{ ".usx", ".upx", ".ugx" };
		public static readonly string[] AnimExt 		= new string[]{ ".ukx" };
		public static readonly string[] CacheExt 		= new string[]{ ".uxx" };
		// UT2004, UDK, Unreal, Red Orchestra Map
		public static readonly string[] MapExt 			= new string[]{ ".ut2", ".udk", ".unr", ".rom", ".un2", ".aao", ".run", ".sac", ".xcm", ".nrf", ".wot", ".scl", ".dvs", ".rsm", ".ut3" };
		public static readonly string[] SaveExt 		= new string[]{ ".uvx", ".md5", ".usa", ".ums", ".rsa", ".sav" };
		public static readonly string[] PackageExt 		= new string[]{ ".upk" };
		public static readonly string[] ModExt 			= new string[]{ ".umod", ".ut2mod", ".ut4mod" };

		public static bool IsUnrealExt( string fileExt )
		{
			return ScriptExt.Contains( fileExt ) ||
				TextureExt.Contains( fileExt ) ||
				SoundExt.Contains( fileExt ) ||
				MeshExt.Contains( fileExt ) ||
				AnimExt.Contains( fileExt ) ||
				CacheExt.Contains( fileExt ) ||
				MapExt.Contains( fileExt ) ||
				SaveExt.Contains( fileExt ) ||
				PackageExt.Contains( fileExt );
		}

		public static string FormatUnrealExtensionsAsFilter()
		{
			List<string> exts = FormatUnrealExtensionsAsList();

			string extensions = "";
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
			List<string> exts = new List<string>(
				ScriptExt.Length + 
				TextureExt.Length + 
				SoundExt.Length + 
				MeshExt.Length + 
				AnimExt.Length + 
				CacheExt.Length + 
				MapExt.Length + 
				SaveExt.Length +
				PackageExt.Length);

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
