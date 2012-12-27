using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UField
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UField).Name );
			AddTextNode( _ParentNode, "SuperField:" + Super ); 
			AddTextNode( _ParentNode, "NextField:" + NextField ); 
			base.InitNodes( _ParentNode );
		}
	}
}
