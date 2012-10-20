using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UELib.Core;

namespace UELib
{
	#region Exceptions
	[Serializable]
	public class UnrealException : Exception
	{
		public UnrealException(){}
		public UnrealException( string message ) : base( message )
		{
		}

		public UnrealException( string message, Exception innerException ) : base( message, innerException )
		{
		}
	}

	[Serializable]
	public class DeserializationException : UnrealException
	{
		[System.NonSerializedAttribute]
		public readonly string Output;

		public DeserializationException()
		{
			Output = "SerializationException";
		}

		public DeserializationException( string output ) : base( output )
		{
			Output = output;
		}
	}

	[Serializable]
	public class DecompilingCastException : DeserializationException
	{
	}

	[Serializable]
	public class DecompilingHeaderException : UnrealException
	{
		[System.NonSerializedAttribute]
		public readonly string Output;

		public DecompilingHeaderException()
		{
			Output = "DecompilingHeaderException";
		}

		public DecompilingHeaderException( string output )
		{
			Output = output;
		}
	}

	[Serializable]
	public class CookedPackageException : UnrealException
	{
		public CookedPackageException() : base( "The package is cooked" )
		{
		}			
	}

	[Serializable]
	public class DecompressPackageException : UnrealException
	{
		public DecompressPackageException() : base( "Failed to decompress this package" )
		{
		}			
	}

	[Serializable]
	public class OccurredWhileException : UnrealException
	{
		public OccurredWhileException( string endmsg ) : base( "An exception occurred while " + endmsg )
		{
		}			
	}

	[Serializable]
	public class SerializingObjectsException : OccurredWhileException
	{
		public SerializingObjectsException() : base( "serializing objects" )
		{
		}
	}

	[Serializable]
	public class ImportingObjectsException : OccurredWhileException
	{
		public ImportingObjectsException() : base( "importing objects" )
		{
		}
	}

	[Serializable]
	public class LinkingObjectsException : OccurredWhileException
	{
		public LinkingObjectsException() : base( "linking objects" )
		{
		}
	}
	#endregion

	#region Static Methods
	/// <summary>
	/// Provides static methods for formating flags.
	/// </summary>
	public static class UnrealMethods
	{
		public static string FlagsListToString( List<string> flagsList )
		{
			string output = "";
			foreach( string S in flagsList )
			{
				output += S + (S != flagsList.Last() ? "\n" : String.Empty);
			}
			return output;
		}

		public static List<string> FlagsToList( Type flagEnum, uint flagsDWORD )
		{
			List<string> FlagsList = new List<string>();
			Array FlagValues = Enum.GetValues( flagEnum );
			foreach( uint Flag in FlagValues )
			{
				if( (flagsDWORD & Flag) == Flag )
				{
					string eName = Enum.GetName( flagEnum, Flag );
					if( FlagsList.Contains( eName ) )
						continue;

					FlagsList.Add( eName );
				}
			}
			return FlagsList;
		}

		public static List<string> FlagsToList( Type flagEnum, ulong flagsDWORD )
		{
			List<string> FlagsList = new List<string>();
			Array FlagValues = Enum.GetValues( flagEnum );
			foreach( ulong Flag in FlagValues )
			{
				if( (flagsDWORD & Flag) == Flag )
				{
					string eName = Enum.GetName( flagEnum, Flag );
					if( FlagsList.Contains( eName ) )
						continue;

					FlagsList.Add( eName );
				}
			}
			return FlagsList;
		}

		public static List<string> FlagsToList( Type flagEnum, Type flagEnum2, ulong flagsQWORD )
		{
			var list = FlagsToList( flagEnum, flagsQWORD );
			list.AddRange( FlagsToList( flagEnum2, flagsQWORD >> 32 ) );
			return list; 
		}

		public static string FlagToString( uint flags )
		{
			return flags != 0 ? "0x" + String.Format( "{0:x4}", flags ).PadLeft( 8, '0' ).ToUpper() : String.Empty;
		}

		public static string FlagToString( ulong flags )
		{
			return FlagToString( (uint)(flags >> 32) ) + "-" + FlagToString( (uint)(flags) );
		}
	}
	#endregion

	#region Lists
	[System.Runtime.InteropServices.ComVisible( false )]
	public sealed class DefaultPropertiesCollection : List<UDefaultProperty>
	{
		public UDefaultProperty FindPropertyByName( string name )
		{
			return this.Find
			( 
				delegate( UDefaultProperty prop )
				{
					return prop.Tag.Name == name;
				}
			);
		}

		public UDefaultProperty FindPropertyByIndex( int index )
		{
			return this.Find
			( 
				delegate( UDefaultProperty prop )
				{
					return prop.Tag.NameIndex == index;
				}
			);
		}

		public bool ContainsIndex( int index )
		{
			return FindPropertyByIndex( index ) != null;
		}

		public bool ContainsIndex( int index, out UDefaultProperty prop )
		{
			prop = FindPropertyByIndex( index );
			return prop != null;
		}

		public bool ContainsName( string name )
		{
			return FindPropertyByName( name ) != null;
		}

		public bool ContainsName( string name, out UDefaultProperty prop )
		{
			prop = FindPropertyByName( name );
			return prop != null;
		}
	}
	#endregion
}
