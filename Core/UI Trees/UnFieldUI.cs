using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UField
	{
		protected override void InitNodes( TreeNode node )
		{
			ParentNode = AddSectionNode( node, typeof(UField).Name );
			AddTextNode( ParentNode, "SuperField:" + Super ); 
			AddTextNode( ParentNode, "NextField:" + NextField ); 
			base.InitNodes( ParentNode );
		}
	}
}
