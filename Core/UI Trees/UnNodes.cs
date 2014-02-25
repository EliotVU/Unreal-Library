using System;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.Serialization;

namespace UELib.Core
{
	[Serializable]
	[System.Runtime.InteropServices.ComVisible( false )]
	public class ObjectNode : TreeNode, IDecompilableObject
	{
		public IUnrealDecompilable Object{ get; private set; }

		public ObjectNode( IUnrealDecompilable objectRef )
		{
			Object = objectRef;
		}

		protected ObjectNode( SerializationInfo info, StreamingContext context ) : base( info, context )
		{
			info.AddValue( Text, Object );
		}

		public virtual string Decompile()
		{
			try
			{
				UDecompilingState.ResetTabs();
				return Object.Decompile();
			}
			catch( Exception e )
			{
				return String.Format
				( 
					"An exception of type \"{0}\" occurred while decompiling {1}.\r\nDetails:\r\n{2}", 
					e.GetType().Name, Text, e 
				);
			}
		}
	}

	[System.Runtime.InteropServices.ComVisible( false )]
	public class DefaultObjectNode : ObjectNode
	{
		public DefaultObjectNode( IUnrealDecompilable objectRef ) : base( objectRef )
		{
			ImageKey = typeof(UDefaultProperty).Name;
			SelectedImageKey = ImageKey;
		}
	}

	[System.Runtime.InteropServices.ComVisible( false )]
	public sealed class ObjectListNode : TreeNode, IUnrealDecompilable
	{
		public ObjectListNode()
		{
			ImageKey = "TreeView";
			SelectedImageKey = ImageKey;
		}

        public ObjectListNode( string imageName )
		{
			ImageKey = imageName;
			SelectedImageKey = imageName;
		}

		public string Decompile()
		{
			string fullView = String.Empty;
			foreach( var node in Nodes.OfType<IUnrealDecompilable>() )
			{
				fullView += node.Decompile() + UnrealSyntax.NewLine;
			}
			return fullView;
		}
	}
}
