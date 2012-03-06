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
	public partial class UState : UStruct
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UState).Name );

			if( GetType() == typeof(UState) )
			{
				TextNode SFlagsNode = AddTextNode( _ParentNode, "State Flags:" + UnrealMethods.FlagToString( StateFlags ) );
				SFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.StateFlags), StateFlags ) );
			}

			#if DEBUG
				AddTextNode( _ParentNode, "Label Table offset:" + _LabelTableOffset );
				AddTextNode( _ParentNode, "Ignore Mask:" + String.Format( "0x{0:x8}", _IgnoreMask ) );
				AddTextNode( _ParentNode, "Probe Mask:" + String.Format( "0x{0:x8}", _ProbeMask ) );
			#endif

			base.InitNodes( _ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );

			AddObjectListNode( node, "Functions", _ChildFunctions );
		}
	}
}
