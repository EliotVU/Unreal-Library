using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UClass
	{
		protected override void InitNodes( TreeNode node )
		{
			ParentNode = AddSectionNode( node, typeof(UClass).Name );

			var classFlagsNode = AddTextNode( ParentNode, "Class Flags:" + UnrealMethods.FlagToString( ClassFlags ) ); 
			classFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( 
				UnrealMethods.FlagsToList( typeof(Flags.ClassFlags), ClassFlags ) 
			);

			base.InitNodes( ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );
			AddObjectListNode( node, "States", States );
		}
	}
}
