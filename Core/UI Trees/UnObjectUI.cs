using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UObject
	{
		// Cannot be viewed
		internal class TextNode:TreeNode{internal TextNode(string n):base(n){}}
		internal TextNode _ParentNode = null;
		public bool InitializedNodes = false;

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

			if( GetType().IsSubclassOf( typeof(UProperty) ) )
			{
				node.ImageKey = typeof(UProperty).Name;
			}
			else
			{
				node.ImageKey = GetType().Name;	
			}
			node.SelectedImageKey = node.ImageKey;

			InitializedNodes = true;
		}

		protected virtual void InitNodes( TreeNode node )
		{			
			_ParentNode = AddSectionNode( node, typeof(UObject).Name );
			foreach( System.Reflection.PropertyInfo PI in GetType().GetProperties() )
			{
				// Only properties that are from UObject i.e. ignore properties of children.
				// Flags
				if( PI.PropertyType == typeof(uint) )
				{
					AddTextNode( _ParentNode, PI.Name + ":" + String.Format( "0x{0:x4}", PI.GetValue( this, null ) ) );
				}
				/*else if( PI.PropertyType != null && !PI.PropertyType.IsArray )
				{
					AddTextNode( _ParentNode, PI.Name + ":" + PI.GetValue( this, null ).ToString() );
				}*/
			}

			TextNode flagNode = AddTextNode( _ParentNode, "ObjectFlags:" + UnrealMethods.FlagToString( ObjectFlags ) );
			flagNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.ObjectFlagsLO), typeof(Flags.ObjectFlagsHO), ObjectFlags ) );

			AddTextNode( _ParentNode, "Size:" + ExportTable.SerialSize );
			AddTextNode( _ParentNode, "Offset:" + ExportTable.SerialOffset );
		}

		protected virtual void AddChildren( TreeNode node )
		{
		}

		protected virtual void PostAddChildren( TreeNode node )
		{
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic" )]
		internal TextNode AddSectionNode( TreeNode p, string n )
		{
			var NN = new TextNode( n ){ImageKey = typeof (UObject).Name};
			NN.SelectedImageKey = NN.ImageKey;
		   	p.Nodes.Add( NN );
			return NN;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic" )]
		internal TextNode AddTextNode( TreeNode p, string n )
		{
			TextNode NN = new TextNode( n );
			NN.ImageKey = "Unknown";
			NN.SelectedImageKey = NN.ImageKey;
			p.Nodes.Add( NN );
			return NN;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage( "Microsoft.Performance", "CA1822:MarkMembersAsStatic" )]
		internal ObjectNode AddObjectNode( TreeNode parentNode, UObject unrealObject )
		{
			ObjectNode ObjN = new ObjectNode( unrealObject );
			ObjN.Text = unrealObject.Name;
			unrealObject.InitializeNodes( ObjN );

			if( unrealObject.SerializationState.HasFlag( ObjectState.Errorlized ) )
			{
				ObjN.ForeColor = System.Drawing.Color.Red;
			}

			parentNode.Nodes.Add( ObjN );
			return ObjN;
		}

		internal ObjectListNode AddObjectListNode( TreeNode parentNode, string title, IEnumerable<UObject> objects )
		{
			if( objects.Count() > 0 )
			{	
				ObjectListNode listNode = new ObjectListNode();
 				listNode.Text = title;
				foreach( UObject obj in objects )
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