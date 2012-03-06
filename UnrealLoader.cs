using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib.Core;

namespace UELib
{
	/// <summary>
	/// Provides static methods for loading unreal packages.
	/// </summary>
	public static class UnrealLoader
	{
		/// <summary>
		/// Stored packages that were imported by certain objects. Kept here that in case re-use is necessary, that it will be loaded faster.
		/// The packages and the list is closed and cleared by the main package that loaded them with ImportObjects(). 
		/// In any other case the list needs to be cleared manually.
		/// <summary>
		private static List<UnrealPackage> CachedPackages = new List<UnrealPackage>();

		/// <summary>
		/// Loads the given file specified by PackagePath and
		/// returns the serialized UnrealPackage.
		/// </summary>
		public static UnrealPackage LoadPackage( string packagePath, System.IO.FileAccess fileAccess = System.IO.FileAccess.Read )
		{
			return UnrealPackage.DeserializePackage( packagePath, fileAccess );
		}

		/// <summary>
		/// Looks if the package is already loaded before by looking into the CachedPackages list first. 
		/// If it is not found then it loads the given file specified by PackagePath and returns the serialized UnrealPackage.
		/// </summary>
		public static UnrealPackage LoadCachedPackage( string packagePath, System.IO.FileAccess fileAccess = System.IO.FileAccess.Read )
		{
			UnrealPackage UPkg = CachedPackages.Find( pkg => pkg.PackageName == System.IO.Path.GetFileNameWithoutExtension( packagePath ) );
			if( UPkg == null )
			{
				UPkg = LoadPackage( packagePath, fileAccess );
				if( UPkg != null )
				{
					CachedPackages.Add( UPkg );
				}
			}
			return UPkg;
		}

		/// <summary>
		/// Loads the given file specified by PackagePath and
		/// returns the serialized UnrealPackage with serialized objects.
		/// </summary>
		public static UnrealPackage LoadFullPackage( string packagePath, System.IO.FileAccess fileAccess = System.IO.FileAccess.Read )
		{
			UnrealPackage UPkg = LoadPackage( packagePath, fileAccess );
			if( UPkg != null )
			{
				UPkg.InitializePackage();
			}
			return UPkg;
		}
	}
}
