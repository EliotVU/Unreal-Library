#if Forms
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;

namespace UELib.Core
{
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    public class ObjectNode : TreeNode, IDecompilableObject
    {
        public IUnrealDecompilable Object { get; private set; }

        public ObjectNode(IUnrealDecompilable objectRef)
        {
            Object = objectRef;
        }

        protected ObjectNode(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            info.AddValue(Text, Object);
        }

        public virtual string Decompile()
        {
            try
            {
                UDecompilingState.ResetTabs();
                return Object.Decompile();
            }
            catch (Exception e)
            {
                return string.Format
                (
                    "An exception of type \"{0}\" occurred while decompiling {1}.\r\nDetails:\r\n{2}",
                    e.GetType().Name, Text, e
                );
            }
        }
    }

    [System.Runtime.InteropServices.ComVisible(false)]
    public class DefaultObjectNode : ObjectNode
    {
        public DefaultObjectNode(IUnrealDecompilable objectRef) : base(objectRef)
        {
            ImageKey = nameof(UDefaultProperty);
            SelectedImageKey = ImageKey;
        }
    }

    [System.Runtime.InteropServices.ComVisible(false)]
    public sealed class ObjectListNode : TreeNode, IUnrealDecompilable
    {
        public ObjectListNode()
        {
            ImageKey = "TreeView";
            SelectedImageKey = ImageKey;
        }

        public ObjectListNode(string imageName)
        {
            ImageKey = imageName;
            SelectedImageKey = imageName;
        }

        public string Decompile()
        {
            var output = new StringBuilder();
            foreach (var node in Nodes.OfType<IUnrealDecompilable>())
            {
                output.AppendLine(node.Decompile());
            }

            return output.ToString();
        }
    }
}
#endif