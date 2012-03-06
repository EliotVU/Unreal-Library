using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UELib.Cache
{
	[Serializable]
	public abstract class CacheException : Exception
	{
		/// <summary>
		/// Creates a new instance of the UELib.Cache.CacheException class. 
		/// </summary>
		public CacheException()
		{
		}
	}

	[Serializable]
	public sealed class CacheEmptyException : CacheException
	{
		/// <summary>
		/// Creates a new instance of the UELib.Cache.CacheEmptyException class. 
		/// </summary>
		public CacheEmptyException()
		{
		}
	}

	/// <summary>
	/// Represents a UnrealCache object with provided functionality for loading, extracting and saving.
	/// </summary>
	public sealed class UnrealCache
	{
		public const string CacheIniName = "\\Cache.ini";
		private string _CachePath = "";

		public struct CacheFileStruct
		{
			public string FileName;
			public string Extension;
			public string Guid;

			public string UnsplitCache()
			{
				return Guid + "=" + FileName + Extension;
			}
		}

		public List<CacheFileStruct> CacheEntries = null;

		/// <summary>
		/// Creates a new instance of the UELib.Cache.UnrealCache class with the specified cache path.
		/// </summary>
		/// <param name="cachePath">A full directory path to the cache folder of a specific game, not including the cache ini file.</param>
		public UnrealCache( string cachePath )
		{
			_CachePath = cachePath;
		}

		/// <summary>
		/// Load all cache entries from the cache.ini located in the set CachePath.
		/// </summary>
		/// <exception cref="UELib.Cache.CacheEmptyException">
		/// </exception>
		public void LoadCacheEntries()
		{
			string[] cacheinfo = null;
			try
			{
				cacheinfo = File.ReadAllLines( _CachePath + CacheIniName );
			}
			catch( IOException )
			{
				throw new CacheEmptyException();
			}

			if( cacheinfo.Length <= 1 )
			{
				throw new CacheEmptyException();
			}

			char[] sep = new char[1] { '=' };
			CacheEntries = new List<CacheFileStruct>();
			for( int i = 1; i < cacheinfo.Length; ++ i )
			{
				string[] line = cacheinfo[i].Split( sep, 2 );
				if( line == null || line.Length == 0 || line[0].Length <= 2 )
				{
					continue;
				}

				CacheFileStruct cfs;
				cfs.Guid = line[0];

				if( line.Length == 1 )
				{
					continue;
				}

				string fullfname = line[1];
				cfs.FileName = Path.GetFileNameWithoutExtension( fullfname );
				cfs.Extension = Path.GetExtension( fullfname );
				CacheEntries.Add( cfs );
			}
		}

		public bool ExtractCacheEntry( int index, string dirPath )
		{
			bool success = false;
			try
			{
				File.Move( Path.Combine( _CachePath, (CacheEntries[index].FileName + CacheEntries[index].Extension) ), 
							Path.Combine( dirPath, (CacheEntries[index].FileName + CacheEntries[index].Extension) ) );
				RemoveCacheEntry( index );
				success = true;
			}
			catch( Exception )
			{
			}
			return success;
		}

		public bool RemoveCacheEntry( int index )
		{
			bool success = false;
			try
			{
				CacheEntries.RemoveAt( index );
				success = true;
			}
			catch( ArgumentOutOfRangeException )
			{
			}
			return success;
		}

		/// <summary>
		/// Deletes the specified file of a cache entry by index. 
		/// </summary>
		/// <param name="index">The cache entry index that should be deleted.</param>
		public bool DeleteCacheEntry( int index )
		{
			bool success = false;
			try
			{
				File.Delete( Path.Combine( _CachePath, (CacheEntries[index].FileName + CacheEntries[index].Extension) ) );
				RemoveCacheEntry( index );
				success = true;
			}
			catch( Exception )
			{
			}
			return success;
		}

		public void ImportFileToCache( string filePath )
		{
			try
			{
				File.Move( filePath, _CachePath );
				CacheFileStruct cfs = new CacheFileStruct();
				cfs.FileName = Path.GetFileNameWithoutExtension( filePath );
				cfs.Extension = Path.GetExtension( filePath );
				cfs.Guid = cfs.FileName;	// TODO: Generate a guid.
				CacheEntries.Add( cfs );
			}
			catch( Exception )
			{
			}
		}

		public void SaveCacheEntries()
		{
			string[] Contents = new string[CacheEntries.Count + 1];
			Contents[0] = "[Cache]";
			for( int i = 0; i < CacheEntries.Count; ++ i )
			{
				Contents[i + 1] = CacheEntries[i].UnsplitCache();
			}
			File.WriteAllLines( _CachePath + CacheIniName, Contents );
		}
	}
}