using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace UELib
{
	using UELib.Core;

	public sealed class NativeTable
	{
		public string 	Name;
		public byte 	OperPrecedence;
		public byte 	Format;
		public int 		ByteToken;

		public void SetFormat( UFunction function )
		{
			if( function.IsOperator() )
			{
				Format = (byte)NativeType.Operator;
			}
			else if( function.IsPost() )
			{
				Format = (byte)NativeType.PostOperator;
			}
			else if( function.IsPre() )
			{
				Format = (byte)NativeType.PreOperator;
			} 
			else
			{
				Format = (byte)NativeType.Function;
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue" )]
	public enum NativeType : byte
	{
		Function		= 1,
		Operator		= 2,
		PreOperator		= 3,
		PostOperator	= 4
	}

	public sealed class NativesTablePackage
	{
		public const uint Signature = 0x2C8D14F1;
		public const string Extension = ".NTL";

		public List<NativeTable> NativesTableList;

		public NativesTablePackage()
		{
		}

		public void LoadPackage( string name )
		{
			using( FileStream stream = new FileStream( name + Extension, FileMode.Open, FileAccess.Read ) )
			{
				BinaryReader BinReader = new BinaryReader( stream );
				if( BinReader.ReadUInt32() != Signature )
				{
					throw new UnrealException( "File " + stream.Name + " is not a NTL file!" );
				}
				int Count = BinReader.ReadInt32();
				NativesTableList = new List<NativeTable>();
				for( int i = 0; i < Count; ++ i )
				{
					NativeTable NT = new NativeTable();

					// Name
					NT.Name = BinReader.ReadString();
					NT.OperPrecedence = BinReader.ReadByte();
					NT.Format = BinReader.ReadByte();
					NT.ByteToken = BinReader.ReadInt32();
					NativesTableList.Add( NT );
				}
				NativesTableList.Sort
				( 
					delegate( NativeTable nt1, NativeTable nt2 )
					{ 
						return nt1.ByteToken.CompareTo( nt2.ByteToken ); 
					} 
				);
			}
		}

		public NativeTable FindTable( int token )
		{
			int lownum = 0;
			int highnum = NativesTableList.Count - 1;
			while( lownum <= highnum )
			{
				int midnum = (lownum + highnum) / 2;
				if( token > NativesTableList[midnum].ByteToken )
				{
					lownum = midnum + 1;
				}
				else if( token < NativesTableList[midnum].ByteToken )
				{
					highnum = midnum - 1;
				}
				else
				{
					return NativesTableList[midnum];
				}
			}
			return null;
		}

		public void CreatePackage( string name )
		{
			using( FileStream stream = new FileStream( name + Extension, FileMode.Create, FileAccess.Write ) )
			{
				BinaryWriter BinWriter = new BinaryWriter( stream );
				BinWriter.Write( Signature ); 
				BinWriter.Write( NativesTableList.Count );
				foreach( NativeTable T in NativesTableList )
				{
					BinWriter.Write( T.Name );
					BinWriter.Write( T.OperPrecedence );
					BinWriter.Write( T.Format );
					BinWriter.Write( T.ByteToken );
				}
			}
		}
	}
}