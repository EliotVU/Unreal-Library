using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UStruct
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UStruct).Name );
			if( IsPureStruct() )
			{
				var sFlagsNode = AddTextNode( _ParentNode, "Struct Flags:" + UnrealMethods.FlagToString( StructFlags ) );
				sFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.StructFlags), StructFlags ) );
			}

			AddTextNode( _ParentNode, "Script Size:" + DataScriptSize );
			base.InitNodes( _ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
            if( ScriptText != null )
            {
                AddObjectNode( node, ScriptText, "UObject" );
            }

            if( CppText != null )
            {
                AddObjectNode( node, CppText, "UObject" );
            }

            if( ProcessedText != null )
            {
                AddObjectNode( node, ProcessedText, "UObject" );
            }

			AddObjectListNode( node, "Constants", Constants, "UConst" );
			AddObjectListNode( node, "Enumerations", Enums, "UEnum" );
			AddObjectListNode( node, "Structures", Structs, "UStruct" );
            // Not if the upper class is a function; UFunction adds locals and parameters instead
			if( GetType() != typeof(UFunction) )
			{
				AddObjectListNode( node, "Variables", Variables, "UProperty" );
			}
		}

		protected override void PostAddChildren( TreeNode node )
		{
			if( Properties == null || Properties.Count <= 0 )
				return;

			var defNode = new ObjectListNode
            {
                Text = "Default Values", 
                ImageKey = "UDefaultProperty", 
                SelectedImageKey = "UDefaultProperty"
            };
			node.Nodes.Add( defNode );
			foreach( var def in Properties )
			{	
				var objN = new DefaultObjectNode( def ){Text = def.Name};
				defNode.Nodes.Add( objN );
			}
		}
	}
}
