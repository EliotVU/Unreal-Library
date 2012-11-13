using System;
using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UState
	{
		protected override void InitNodes( TreeNode node )
		{
			ParentNode = AddSectionNode( node, typeof(UState).Name );

			if( GetType() == typeof(UState) )
			{
				TextNode stateFlagsNode = AddTextNode( ParentNode, "State Flags:" + UnrealMethods.FlagToString( StateFlags ) );
				stateFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.StateFlags), StateFlags ) );
			}

			#if DEBUG
				AddTextNode( ParentNode, "Label Table offset:" + _LabelTableOffset );
				AddTextNode( ParentNode, "Ignore Mask:" + String.Format( "0x{0:x8}", _IgnoreMask ) );
				AddTextNode( ParentNode, "Probe Mask:" + String.Format( "0x{0:x8}", _ProbeMask ) );
			#endif

			base.InitNodes( ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );

			AddObjectListNode( node, "Functions", _ChildFunctions );
		}
	}
}
