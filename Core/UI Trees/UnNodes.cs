using System;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.Serialization;

namespace UELib.Core
{
	[Serializable]
	[System.Runtime.InteropServices.ComVisible( false )]
	public class ObjectNode : TreeNode, IDecompilableObjectNode
	{
		public virtual IUnrealDecompilable Object
		{
			get;
			set;
		}

		public virtual bool AllowDecompile
		{
			get{ return true; }
		}

		public virtual bool CanViewBuffer
		{
			get{ return true; }
		}

		public ObjectNode( IUnrealDecompilable objectRef )
		{
			Object = objectRef;
		}

		protected ObjectNode(SerializationInfo info, StreamingContext context) : base(info, context){}

		public virtual string Decompile()
		{
			try
			{
				UDecompilingState.ResetTabs();
				return Object.Decompile();
			}
			catch( Exception e )
			{
				return "An " + e.GetType().Name + " occurred in " + Text 
					+ " while decompiling.\r\nDetails:\r\n" + e;
			}
		}
	}

	[Serializable]
	[System.Runtime.InteropServices.ComVisible( false )]
	public class DefaultObjectNode : ObjectNode
	{
		public override bool CanViewBuffer
		{
			get{ return false; }
		}

		public DefaultObjectNode( IUnrealDecompilable objectRef ) : base( objectRef )
		{
			ImageKey = typeof(UProperty).Name;
			SelectedImageKey = ImageKey;
		}

		protected DefaultObjectNode(SerializationInfo info, StreamingContext context) : base(info, context)
		{}
	}

	[Serializable]
	[System.Runtime.InteropServices.ComVisible( false )]
	public class ObjectListNode : TreeNode, IDecompilableNode
	{
		public virtual bool AllowDecompile
		{
			get{ return (Nodes.Count > 0); }
		}

		public ObjectListNode()
		{
			ImageKey = "List";
			SelectedImageKey = ImageKey;
		}

		protected ObjectListNode(SerializationInfo info, StreamingContext context) : base(info, context){}

		public virtual string Decompile()
		{
			string fullView = String.Empty;
			foreach( IDecompilableNode node in Nodes.OfType<IDecompilableNode>() )
			{
				if( node.AllowDecompile )
				{
					fullView += node.Decompile() + UnrealSyntax.NewLine;
				}
			}
			return fullView;
		}
	}
}
