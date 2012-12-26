using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UStruct
	{
		protected override void InitNodes( TreeNode node )
		{
			ParentNode = AddSectionNode( node, typeof(UStruct).Name );
			if( IsPureStruct() )
			{
				var sFlagsNode = AddTextNode( ParentNode, "Struct Flags:" + UnrealMethods.FlagToString( StructFlags ) );
				sFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.StructFlags), StructFlags ) );
			}

			if( ScriptBuffer != null )
			{
				var objN = new ObjectNode( ScriptBuffer ) {Text = ScriptBuffer.Name};
				node.Nodes.Add( objN );
			}

			AddTextNode( ParentNode, "Script Size:" + DataScriptSize );
			base.InitNodes( ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			AddObjectListNode( node, "Constants", Constants );
			AddObjectListNode( node, "Enumerations", Enums );
			AddObjectListNode( node, "Structures", Structs );

			// Not if the upper class is a function; UFunction adds locals and parameters instead
			if( GetType() != typeof(UFunction) )
			{
				AddObjectListNode( node, "Variables", Variables );
			}
		}

		protected override void PostAddChildren( TreeNode node )
		{
			if( Properties == null || Properties.Count <= 0 )
				return;

			var defNode = new ObjectListNode{Text = "Default Values"};
			node.Nodes.Add( defNode );
			foreach( var def in Properties )
			{	
				var objN = new DefaultObjectNode( def ){Text = def.Name};
				defNode.Nodes.Add( objN );
			}
		}
	}
}
