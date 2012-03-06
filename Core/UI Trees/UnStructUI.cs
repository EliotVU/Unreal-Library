using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UStruct : UField
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UStruct).Name );
			if( GetType() == typeof(UStruct) )
			{
				var SFlagsNode = AddTextNode( _ParentNode, "Struct Flags:" + UnrealMethods.FlagToString( StructFlags ) );
				SFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.StructFlags), StructFlags ) );
			}

			if( ScriptBuffer != null )
			{
				var ObjN = new ObjectNode( ScriptBuffer ) {Text = ScriptBuffer.Name};
				node.Nodes.Add( ObjN );
			}

			#if DEBUG	
				AddTextNode( _ParentNode, "Script Index:" + ScriptText ); 
				AddTextNode( _ParentNode, "Children Index:" + Children );
				AddTextNode( _ParentNode, "CppText Index:" + CppText );
				AddTextNode( _ParentNode, "FriendlyName Index:" + _FriendlyNameIndex );
				AddTextNode( _ParentNode, "Line:" + this.Line );
				AddTextNode( _ParentNode, "TextPos:" + this.TextPos );
				AddTextNode( _ParentNode, "_MinAlignment:" + this._MinAlignment );		
				AddTextNode( _ParentNode, "Script Size:" + _ScriptSize );
			#endif
			base.InitNodes( _ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			AddObjectListNode( node, "Constants", _ChildConstants );
			AddObjectListNode( node, "Enumerations", _ChildEnums );
			AddObjectListNode( node, "Structures", _ChildStructs );

			// Not if the upper class is a function; UFunction adds locals and parameters instead
			if( GetType() != typeof(UFunction) )
			{
				AddObjectListNode( node, "Properties", _ChildProperties );
			}
		}

		protected override void PostAddChildren( TreeNode node )
		{
			if( _Properties != null && _Properties.Count > 0 )
			{
				ObjectListNode DefNode = new ObjectListNode();
				DefNode.Text = "Default Values";
				node.Nodes.Add( DefNode );
				foreach( UDefaultProperty Def in _Properties )
				{	
					DefaultObjectNode ObjN = new DefaultObjectNode( Def );
					ObjN.Text = Def.Tag.Name;
					DefNode.Nodes.Add( ObjN );
				}
			}
		}
	}
}
