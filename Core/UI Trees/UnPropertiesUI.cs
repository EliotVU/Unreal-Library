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
	public partial class UProperty : UField
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UProperty).Name );

			TextNode SFlagsNode = AddTextNode( _ParentNode, "Property Flags:" + UnrealMethods.FlagToString( PropertyFlags ) );
			SFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.PropertyFlagsLO), typeof(Flags.PropertyFlagsHO), PropertyFlags ) );
		 	
			if( RepOffset > 0 )
			{
				AddTextNode( _ParentNode, "Replication Offset:" + RepOffset );
			}

			base.InitNodes( _ParentNode );
		}
	}
}
