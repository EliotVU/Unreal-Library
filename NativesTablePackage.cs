using System;
using System.Collections.Generic;
using System.IO;

namespace UELib
{
	using Core;

	public sealed class NativeTableItem
	{
		public string 			Name;
		public byte 			OperPrecedence;
		public FunctionType		Type;
		public int 				ByteToken;

		public NativeTableItem()
		{
		}

		public NativeTableItem( UFunction function )
		{
			if( function.IsOperator() )
			{
				Type = FunctionType.Operator;
			}
			else if( function.IsPost() )
			{
				Type = FunctionType.PostOperator;
			}
			else if( function.IsPre() )
			{
				Type = FunctionType.PreOperator;
			} 
			else
			{
				Type = FunctionType.Function;
			}

			OperPrecedence = function.OperPrecedence;
			ByteToken = function.NativeToken;
			Name = Type == FunctionType.Function 
				? function.Name 
				: function.FriendlyName;
		}
	}

	public enum FunctionType : byte
	{
		Function		= 1,
		Operator		= 2,
		PreOperator		= 3,
		PostOperator	= 4
	}

	public sealed class NativesTablePackage
	{
		private const uint Signature = 0x2C8D14F1;
		public const string Extension = ".NTL";

		public List<NativeTableItem> NativeTableList;

		public void LoadPackage( string name )
		{
			using( var stream = new FileStream( name + Extension, FileMode.Open, FileAccess.Read ) )
			{
				var binReader = new BinaryReader( stream );
				if( binReader.ReadUInt32() != Signature )
				{
					throw new UnrealException( String.Format( "File {0} is not a NTL file!", stream.Name ) );
				}
				int count = binReader.ReadInt32();
				NativeTableList = new List<NativeTableItem>();
				for( int i = 0; i < count; ++ i )
				{
					NativeTableList.Add
					( 
						new NativeTableItem
						{
							Name = binReader.ReadString(),
							OperPrecedence = binReader.ReadByte(),
							Type = (FunctionType)binReader.ReadByte(),
							ByteToken = binReader.ReadInt32()
						} 
					);
				}
				NativeTableList.Sort( (nt1, nt2) => nt1.ByteToken.CompareTo( nt2.ByteToken ) );
			}
		}

		public NativeTableItem FindTableItem( int nativeToken )
		{
			int lowNum = 0;
			int highNum = NativeTableList.Count - 1;
			while( lowNum <= highNum )
			{
				int midNum = (lowNum + highNum) / 2;
				if( nativeToken > NativeTableList[midNum].ByteToken )
				{
					lowNum = midNum + 1;
				}
				else if( nativeToken < NativeTableList[midNum].ByteToken )
				{
					highNum = midNum - 1;
				}
				else
				{
					return NativeTableList[midNum];
				}
			}
			return null;
		}

		public void CreatePackage( string name )
		{
			using( var stream = new FileStream( name + Extension, FileMode.Create, FileAccess.Write ) )
			{
				var binWriter = new BinaryWriter( stream );
				binWriter.Write( Signature ); 
				binWriter.Write( NativeTableList.Count );
				foreach( var item in NativeTableList )
				{
					binWriter.Write( item.Name );
					binWriter.Write( item.OperPrecedence );
					binWriter.Write( (byte)item.Type );
					binWriter.Write( item.ByteToken );
				}
			}
		}
	}
}