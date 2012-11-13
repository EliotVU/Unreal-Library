using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UObject
	{
		// Cannot be viewed
		internal class TextNode : TreeNode
		{
			internal TextNode( string node ) : base(node)
			{
			}
		}

		internal TextNode ParentNode;
		public bool InitializedNodes;

		public void InitializeNodes( TreeNode node )
		{
			if( InitializedNodes )
				return;

			try
			{
				node.ToolTipText = FormatHeader();	
			}
			catch
			{
				node.ToolTipText = "An error occurred!";
				//throw new DecompilingHeaderException();
			}

			InitNodes( node );
			AddChildren( node );
			PostAddChildren( node );

			node.ImageKey = GetType().IsSubclassOf( typeof(UProperty) ) ? typeof(UProperty).Name : GetType().Name;
			node.SelectedImageKey = node.ImageKey;
			InitializedNodes = true;
		}

		protected virtual void InitNodes( TreeNode node )
		{			
			ParentNode = AddSectionNode( node, typeof(UObject).Name );
			/*foreach( System.Reflection.PropertyInfo PI in GetType().GetProperties() )
			{
				// Only properties that are from UObject i.e. ignore properties of children.
				// Flags
				if( PI.PropertyType == typeof(uint) )
				{
					AddTextNode( _ParentNode, PI.Name + ":" + String.Format( "0x{0:x4}", PI.GetValue( this, null ) ) );
				}
				else if( PI.PropertyType != null && !PI.PropertyType.IsArray )
				{
					AddTextNode( _ParentNode, PI.Name + ":" + PI.GetValue( this, null ).ToString() );
				}
			}*/

			TextNode flagNode = AddTextNode( ParentNode, "ObjectFlags:" + UnrealMethods.FlagToString( ObjectFlags ) );
			flagNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.ObjectFlagsLO), typeof(Flags.ObjectFlagsHO), ObjectFlags ) );

			AddTextNode( ParentNode, "Size:" + ExportTable.SerialSize );
			AddTextNode( ParentNode, "Offset:" + ExportTable.SerialOffset );
		}

		protected virtual void AddChildren( TreeNode node )
		{
		}

		protected virtual void PostAddChildren( TreeNode node )
		{
		}

		internal static TextNode AddSectionNode( TreeNode p, string n )
		{
			var nn = new TextNode( n ){ImageKey = typeof (UObject).Name};
			nn.SelectedImageKey = nn.ImageKey;
		   	p.Nodes.Add( nn );
			return nn;
		}

		internal static TextNode AddTextNode( TreeNode p, string n )
		{
			var nn = new TextNode( n ){ImageKey = "Unknown"};
			nn.SelectedImageKey = nn.ImageKey;
			p.Nodes.Add( nn );
			return nn;
		}

		internal static ObjectNode AddObjectNode( TreeNode parentNode, UObject unrealObject )
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

		internal static ObjectListNode AddObjectListNode( TreeNode parentNode, string title, IEnumerable<UObject> objects )
		{
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