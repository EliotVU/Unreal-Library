using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UFunction
	{
		protected override void InitNodes( TreeNode node )
		{							
			node.ToolTipText = FormatHeader();			
			ParentNode = AddSectionNode( node, typeof(UFunction).Name );
#if DEBUGX
			AddNN( LastNode, "iNative:" + iNative ); 
			AddNN( LastNode, "OperPrecedence:" + OperPrecedence );
#endif
			TextNode funcFlagsNode = AddTextNode( ParentNode, "FunctionFlags:" + UnrealMethods.FlagToString( FunctionFlags ) );
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

			AddObjectListNode( node, "Parameters", _ChildParams );
			AddObjectListNode( node, "Locals", _ChildLocals );
		}
	}
}
