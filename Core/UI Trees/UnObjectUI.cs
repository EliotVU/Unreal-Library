using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UObject
	{
		protected TreeNode _ParentNode;
		public bool HasInitializedNodes;

		public void InitializeNodes( TreeNode node )
		{
			if( HasInitializedNodes )
				return;

			node.ToolTipText = FormatHeader();	
			InitNodes( node );
			AddChildren( node );
			PostAddChildren( node );

			node.ImageKey = GetImageName();
			node.SelectedImageKey = node.ImageKey;
			HasInitializedNodes = true;
		}

        public virtual string GetImageName()
        {
            return GetType().IsSubclassOf( typeof(UProperty) ) 
				? typeof(UProperty).Name : this is UScriptStruct 
					? "UStruct" : GetType().Name;
        }

		protected virtual void InitNodes( TreeNode node )
		{			
			_ParentNode = AddSectionNode( node, typeof(UObject).Name );
			var flagNode = AddTextNode( _ParentNode, "ObjectFlags:" + UnrealMethods.FlagToString( _ObjectFlags ) );
			flagNode.ToolTipText = UnrealMethods.FlagsListToString( 
				UnrealMethods.FlagsToList( typeof(Flags.ObjectFlagsLO), typeof(Flags.ObjectFlagsHO), _ObjectFlags ) 
			);

			AddTextNode( _ParentNode, "Size:" + ExportTable.SerialSize );
			AddTextNode( _ParentNode, "Offset:" + ExportTable.SerialOffset );
		}

		protected virtual void AddChildren( TreeNode node )
		{
		}

		protected virtual void PostAddChildren( TreeNode node )
		{
		}

		protected static TreeNode AddSectionNode( TreeNode p, string n )
		{
			var nn = new TreeNode( n ){ImageKey = "Extend"};
			nn.SelectedImageKey = nn.ImageKey;
		   	p.Nodes.Add( nn );
			return nn;
		}

		protected static TreeNode AddTextNode( TreeNode p, string n )
		{
			var nn = new TreeNode( n ){ImageKey = "Info"};
			nn.SelectedImageKey = nn.ImageKey;
			p.Nodes.Add( nn );
			return nn;
		}

		protected static ObjectNode AddObjectNode( TreeNode parentNode, UObject unrealObject )
		{
			var objN = new ObjectNode( unrealObject ){Text = unrealObject.Name};
			unrealObject.InitializeNodes( objN );

			if( unrealObject.DeserializationState.HasFlag( ObjectState.Errorlized ) )
			{
				objN.ForeColor = System.Drawing.Color.Red;
			}

			parentNode.Nodes.Add( objN );
			return objN;
		}

		protected static ObjectListNode AddObjectListNode
		( 
			TreeNode parentNode, 
			string title,
			IEnumerable<UObject> objects 
		)
		{
			if( objects == null )
				return null;

			var uObjects = objects as List<UObject> ?? objects.ToList();
			if( uObjects.Any() )
			{	
				var listNode = new ObjectListNode{Text = title};
				foreach( var obj in uObjects )
				{
					AddObjectNode( listNode, obj );
				}
				parentNode.Nodes.Add( listNode );
				return listNode;
			}
			return null;
		}
	}

    public partial class UPackage
    {
        public override string GetImageName()
        {
            return "Library";
        }
    }
}