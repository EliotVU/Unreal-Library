using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UProperty
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UProperty).Name );
			var propertyFlagsNode = AddTextNode( _ParentNode, 
				"Property Flags:" + UnrealMethods.FlagToString( PropertyFlags ) 
			);
			propertyFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( 
				typeof(Flags.PropertyFlagsLO), 
				typeof(Flags.PropertyFlagsHO), PropertyFlags ) 
			);
		 	
			if( RepOffset > 0 )
			{
				AddTextNode( _ParentNode, "Replication Offset:" + RepOffset );
			}
			base.InitNodes( _ParentNode );
		}
	}
}
