#if Forms
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UObject
    {
        protected TreeNode _ParentNode;
        public bool HasInitializedNodes;

        public void InitializeNodes(TreeNode node)
        {
            if (HasInitializedNodes)
                return;

            node.ImageKey = GetImageName();
            node.SelectedImageKey = node.ImageKey;
            node.ToolTipText = FormatHeader();

            InitNodes(node);
            AddChildren(node);
            PostAddChildren(node);

            HasInitializedNodes = true;
        }

        public virtual string GetImageName()
        {
            return GetType().IsSubclassOf(typeof(UProperty))
                ? nameof(UProperty)
                : this is UScriptStruct
                    ? nameof(UStruct)
                    : GetType().Name;
        }

        protected virtual void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UObject));
            var flagNode = AddTextNode(_ParentNode, $"ObjectFlags:{(ulong)ObjectFlags:X8}");
            flagNode.ToolTipText = ObjectFlags.ToString(Package.Branch.EnumFlagsMap[typeof(ObjectFlag)]);

            if (ExportResource != null)
            {
                AddTextNode(_ParentNode, $"Size:{ExportResource.SerialSize}");
                AddTextNode(_ParentNode, $"Offset:{ExportResource.SerialOffset}");
            }
        }

        protected virtual void AddChildren(TreeNode node)
        {
        }

        protected virtual void PostAddChildren(TreeNode node)
        {
        }

        protected static TreeNode AddSectionNode(TreeNode p, string n)
        {
            var nn = new TreeNode(n) { ImageKey = "Extend" };
            nn.SelectedImageKey = nn.ImageKey;
            p.Nodes.Add(nn);
            return nn;
        }

        protected static TreeNode AddTextNode(TreeNode p, string n)
        {
            var nn = new TreeNode(n) { ImageKey = "Info" };
            nn.SelectedImageKey = nn.ImageKey;
            p.Nodes.Add(nn);
            return nn;
        }

        protected static ObjectNode AddObjectNode(TreeNode parentNode, UObject unrealObject, string imageName = "")
        {
            if (unrealObject == null)
                return null;

            var objN = new ObjectNode(unrealObject) { Text = unrealObject.Name };
            unrealObject.InitializeNodes(objN);
            if (imageName != string.Empty)
            {
                objN.ImageKey = imageName;
                objN.SelectedImageKey = imageName;
            }

            if (unrealObject.DeserializationState.HasFlag(ObjectState.Errorlized))
            {
                objN.ForeColor = System.Drawing.Color.Red;
            }

            parentNode.Nodes.Add(objN);
            return objN;
        }

        protected static ObjectNode AddSimpleObjectNode(TreeNode parentNode, UObject unrealObject, string text,
            string imageName = "")
        {
            if (unrealObject == null)
                return null;

            var objN = new ObjectNode(unrealObject) { Text = $"{text}:{unrealObject.Name}" };
            if (imageName != string.Empty)
            {
                objN.ImageKey = imageName;
                objN.SelectedImageKey = imageName;
            }

            parentNode.Nodes.Add(objN);
            return objN;
        }

        protected static ObjectListNode AddObjectListNode<T>
        (
            TreeNode parentNode,
            string title,
            IEnumerable<T> objects,
            string imageName = "TreeView"
        ) where T : UObject
        {
            var children = objects.ToList();
            if (!children.Any())
                return null;

            var listNode = new ObjectListNode(imageName) { Text = title };
            foreach (var child in children)
            {
                AddObjectNode(listNode, child, child.GetImageName());
            }

            parentNode.Nodes.Add(listNode);
            return listNode;
        }
    }
}
#endif
