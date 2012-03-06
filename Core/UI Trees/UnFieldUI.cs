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
	public partial class UField : UObject
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UField).Name );
			AddTextNode( _ParentNode, "SuperField:" + (Super != null ? Super.Name : "None") + "(" + _SuperIndex + ")" ); 
			AddTextNode( _ParentNode, "NextField:" + (NextField != null ? NextField.Name : "None") + "(" + _NextIndex + ")" ); 
			base.InitNodes( _ParentNode );
		}
	}
}
