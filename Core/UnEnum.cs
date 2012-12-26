using System.Collections.Generic;

namespace UELib.Core
{
	/// <summary>
	/// Represents a unreal enum. 
	/// </summary>
	[UnrealRegisterClass]
	public partial class UEnum : UField
	{
		#region Serialized Members
		/// <summary>
		/// List of name indexes in the UnrealPackage.Names list.
		/// </summary>
		public readonly IList<string> Names = new List<string>();
		#endregion

		#region Constructors
		protected override void Deserialize()
		{
			base.Deserialize();

			int namesCount = _Buffer.ReadIndex();
			for( int i = 0; i < namesCount; ++ i )
			{
		   		int nameIndex = _Buffer.ReadNameIndex();
				Names.Add( Package.Names[nameIndex].Name );
			}
		}

		//public override void InitializeImports()
		//{
		//    base.InitializeImports();
		//    ImportObject();
		//}

		private void ImportObject()
		{
			// Already imported...
			//if( Names.Count > 0 )
			//{
			//    return;
			//}

			//// Closed when Owner gets closed.
			//UnrealPackage pkg = LoadImportPackage();
			//if( pkg != null )
			//{
			//    if( pkg.Objects == null )
			//    {
			//        pkg.RegisterClass( "Enum", typeof(UEnum) );
			//        pkg.InitializeExportObjects();
			//    }
			//    UEnum E = (UEnum)pkg.FindObject( Name, typeof(UEnum) );
			//    if( E != null )
			//    {
			//        // The names we needed.
			//        Names = E.Names;
			//    }
			//}
		}
		#endregion
	}
}
