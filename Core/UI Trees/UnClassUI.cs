using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UELib;
using UELib.Core;

namespace UELib.Core
{
	public partial class UClass : UState
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UClass).Name );

			TextNode CFlagsNode = AddTextNode( _ParentNode, "Class Flags:" + UnrealMethods.FlagToString( ClassFlags ) ); 
			CFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.ClassFlags), ClassFlags ) );

			#if DEBUG
				if( _UNKNOWNBYTE != 0 )
				{
					AddTextNode( _ParentNode, "_UNKNOWNBYTE:" + _UNKNOWNBYTE );
				}

				if( NativeClassName.Length != 0 )
				{
					AddTextNode( _ParentNode, "NativeClassName:" + NativeClassName );
				}

				AddTextNode( _ParentNode, "Within Index:" + _WithinIndex ); 
				AddTextNode( _ParentNode, "Config Index:" + _ConfigIndex );
				if( HideCategoriesList != null )
				{
					TextNode tn = AddTextNode( _ParentNode, "Hide Categories" );
					foreach( var i in HideCategoriesList )
					{
						AddTextNode( tn, "Index:" + i );
					}
				}

				if( AutoExpandCategoriesList != null )
				{
					TextNode tn = AddTextNode( _ParentNode, "Auto Expand Categories" );
					foreach( var i in AutoExpandCategoriesList )
					{
						AddTextNode( tn, "Index:" + i );
					}
				}

				if( ImplementedInterfacesList != null )
				{
					TextNode tn = AddTextNode( _ParentNode, "Implemented Interfaces" );
					foreach( var i in ImplementedInterfacesList )
					{
						AddTextNode( tn, "Index:" + i );
					}
				}
			#endif

			base.InitNodes( _ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );

			AddObjectListNode( node, "States", _ChildStates );
		}
	}
}
