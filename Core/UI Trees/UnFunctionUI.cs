using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UFunction
	{
		protected override void InitNodes( TreeNode node )
		{							
			node.ToolTipText = FormatHeader();			
			ParentNode = AddSectionNode( node, typeof(UFunction).Name );

			var funcFlagsNode = AddTextNode( ParentNode, "FunctionFlags:" + UnrealMethods.FlagToString( FunctionFlags ) );
			funcFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.FunctionFlags), FunctionFlags ) );

			if( RepOffset > 0 )
			{
				AddTextNode( ParentNode, "Replication Offset:" + RepOffset );
			}
			base.InitNodes( ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );
			AddObjectListNode( node, "Parameters", Params );
			AddObjectListNode( node, "Locals", Locals );
		}
	}
}
