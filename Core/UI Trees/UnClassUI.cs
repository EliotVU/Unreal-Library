using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UClass
	{
		protected override void InitNodes( TreeNode node )
		{
			ParentNode = AddSectionNode( node, typeof(UClass).Name );

			TextNode classFlagsNode = AddTextNode( ParentNode, "Class Flags:" + UnrealMethods.FlagToString( ClassFlags ) ); 
			classFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.ClassFlags), ClassFlags ) );

			#if DEBUG
				if( _UNKNOWNBYTE != 0 )
				{
					AddTextNode( ParentNode, "_UNKNOWNBYTE:" + _UNKNOWNBYTE );
				}

				if( NativeClassName.Length != 0 )
				{
					AddTextNode( ParentNode, "NativeClassName:" + NativeClassName );
				}

				AddTextNode( ParentNode, "Within Index:" + _WithinIndex ); 
				AddTextNode( ParentNode, "Config Index:" + _ConfigIndex );
				if( HideCategoriesList != null )
				{
					TextNode tn = AddTextNode( ParentNode, "Hide Categories" );
					foreach( var i in HideCategoriesList )
					{
						AddTextNode( tn, "Index:" + i );
					}
				}

				if( AutoExpandCategoriesList != null )
				{
					TextNode tn = AddTextNode( ParentNode, "Auto Expand Categories" );
					foreach( var i in AutoExpandCategoriesList )
					{
						AddTextNode( tn, "Index:" + i );
					}
				}

				if( ImplementedInterfacesList != null )
				{
					TextNode tn = AddTextNode( ParentNode, "Implemented Interfaces" );
					foreach( var i in ImplementedInterfacesList )
					{
						AddTextNode( tn, "Index:" + i );
					}
				}
			#endif

			base.InitNodes( ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );

			AddObjectListNode( node, "States", _ChildStates );
		}
	}
}
