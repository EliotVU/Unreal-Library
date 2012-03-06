using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using UELib;
using UELib.Core;

namespace UELib.Core
{
	public partial class UFunction : UStruct
	{
		protected override void InitNodes( TreeNode node )
		{							
			node.ToolTipText = FormatHeader();			
			_ParentNode = AddSectionNode( node, typeof(UFunction).Name );
#if DEBUGX
			AddNN( LastNode, "iNative:" + iNative ); 
			AddNN( LastNode, "OperPrecedence:" + OperPrecedence );
#endif
			TextNode FFlagsNode = AddTextNode( _ParentNode, "FunctionFlags:" + UnrealMethods.FlagToString( FunctionFlags ) );
			FFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.FunctionFlags), FunctionFlags ) );

			if( RepOffset > 0 )
			{
				AddTextNode( _ParentNode, "Replication Offset:" + RepOffset );
			}
			base.InitNodes( _ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );

			AddObjectListNode( node, "Parameters", _ChildParams );
			AddObjectListNode( node, "Locals", _ChildLocals );
		}
	}
}
