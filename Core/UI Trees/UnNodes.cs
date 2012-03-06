using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;

namespace UELib.Core
{
	[Serializable]
	[System.Runtime.InteropServices.ComVisible( false )]
	public class ObjectNode : TreeNode, IDecompileableObjectNode
	{
		protected IUnrealDecompileable _Object;
		public virtual IUnrealDecompileable Object
		{
			get{ return _Object; }
			set{ _Object = value; }
		}

		public virtual bool AllowDecompile
		{
			get{ return true; }
		}

		public virtual bool CanViewBuffer
		{
			get{ return true; }
		}

		public ObjectNode( IUnrealDecompileable objectRef )
		{
			_Object = objectRef;
		}

		protected ObjectNode(SerializationInfo info, StreamingContext context) : base(info, context)
		{}

		public virtual string Decompile()
		{
			try
			{
				UDecompiler.ResetTabs();
				return _Object.Decompile();
			}
			catch( Exception e )
			{
				return "An " + e.GetType().Name + " occurred in " + Text + " while decompiling.\r\nDetails:\r\n" + e.ToString();
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

		public DefaultObjectNode( IUnrealDecompileable objectRef ) : base( objectRef )
		{
			ImageKey = typeof(UProperty).Name;
			SelectedImageKey = ImageKey;
		}

		protected DefaultObjectNode(SerializationInfo info, StreamingContext context) : base(info, context)
		{}
	}

	[Serializable]
	[System.Runtime.InteropServices.ComVisible( false )]
	public class ObjectListNode : TreeNode, IDecompileableNode
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

		protected ObjectListNode(SerializationInfo info, StreamingContext context) : base(info, context)
		{}

		public virtual string Decompile()
		{
			string FullView = "";
			foreach( IDecompileableNode Node in Nodes.OfType<IDecompileableNode>() )
			{
				if( Node.AllowDecompile )
				{
					FullView += Node.Decompile() + "\r\n";
				}
			}
			return FullView;
		}
	}
}
