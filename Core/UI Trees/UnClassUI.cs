using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UClass
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UClass).Name );

			var classFlagsNode = AddTextNode( _ParentNode, "Class Flags:" + UnrealMethods.FlagToString( ClassFlags ) ); 
			classFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( 
				UnrealMethods.FlagsToList( typeof(Flags.ClassFlags), ClassFlags ) 
			);

			base.InitNodes( _ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			base.AddChildren( node );
			AddObjectListNode( node, "States", States );
		}

        public override string GetImageName()
        {
            if( IsClassInterface() )
            {
                return "Interface";
            }
            else if( IsClassWithin() )
            {
                return "UClass-Within";
            }
            return base.GetImageName();
        }
	}
}
