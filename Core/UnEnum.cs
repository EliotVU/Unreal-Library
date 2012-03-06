using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UELib;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal enum. 
	/// </summary>
	public partial class UEnum : UField
	{
		#region Serialized Members
		/// <summary>
		/// List of name index's in the NameTableList
		/// </summary>
		private List<int> _NamesIndex = new List<int>();

		/// <summary>
		/// List of name index's in the NameTableList
		/// </summary>
		public List<int> NamesIndex
		{
			get{ return _NamesIndex; }
		}
		#endregion

		#region PostInitialized Members
		public List<string> Names = new List<string>();
		#endregion

		/// <summary>
		///	Creates a new instance of the UELib.Core.UEnum class. 
		/// </summary>
		public UEnum()
		{
		}

		protected override void Deserialize()
		{
			base.Deserialize();

			int NamesCount = _Buffer.ReadIndex();
			for( int i = 0; i < NamesCount; ++ i )
			{
		   		int NameIndex = _Buffer.ReadNameIndex();
				_NamesIndex.Add( NameIndex );
			}
		}

		public override void PostInitialize()
		{
			base.PostInitialize();
			foreach( int Index in NamesIndex )
			{
				Names.Add( Package.NameTableList[Index].Name );
			}
		}

		public override void InitializeImports()
		{
			base.InitializeImports();
			ImportObject();
		}

		private void ImportObject()
		{
			// Already imported...
			if( Names.Count > 0 )
			{
				return;
			}

			// Closed when Owner gets closed.
			UnrealPackage pkg = LoadImportPackage();
			if( pkg != null )
			{
				if( pkg.ObjectsList == null )
				{
					pkg.RegisterClass( "Enum", typeof(UEnum) );
					pkg.InitializeExportObjects();
				}
				UEnum E = (UEnum)pkg.FindObject( Name, typeof(UEnum) );
				if( E != null )
				{
					// The names we needed.
					Names = E.Names;
				}
			}
		}
	}
}
