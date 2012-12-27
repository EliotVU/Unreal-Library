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

			node.ImageKey = GetType().IsSubclassOf( typeof(UProperty) ) 
				? typeof(UProperty).Name : this is UScriptStruct 
					? "UStruct" : GetType().Name;
			node.SelectedImageKey = node.ImageKey;
			HasInitializedNodes = true;
		}

		protected virtual void InitNodes( TreeNode node )
		{			
			_ParentNode = AddSectionNode( node, typeof(UObject).Name );
			var flagNode = AddTextNode( _ParentNode, "ObjectFlags:" + UnrealMethods.FlagToString( ObjectFlags ) );
			flagNode.ToolTipText = UnrealMethods.FlagsListToString( 
				UnrealMethods.FlagsToList( typeof(Flags.ObjectFlagsLO), typeof(Flags.ObjectFlagsHO), ObjectFlags ) 
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
			var nn = new TreeNode( n ){ImageKey = typeof(UObject).Name};
			nn.SelectedImageKey = nn.ImageKey;
		   	p.Nodes.Add( nn );
			return nn;
		}

		protected static TreeNode AddTextNode( TreeNode p, string n )
		{
			var nn = new TreeNode( n ){ImageKey = "Unknown"};
			nn.SelectedImageKey = nn.ImageKey;
			p.Nodes.Add( nn );
			return nn;
		}

		protected static ObjectNode AddObjectNode( TreeNode parentNode, UObject unrealObject )
		{
			var objN = new ObjectNode( unrealObject ){Text = unrealObject.Name};
			unrealObject.InitializeNodes( objN );

			if( unrealObject.SerializationState.HasFlag( ObjectState.Errorlized ) )
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
}