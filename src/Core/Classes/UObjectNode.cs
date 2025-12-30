#if Forms
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UObject
    {
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
        }

        protected virtual void AddChildren(TreeNode node)
        {
        }

        private void PostAddChildren(TreeNode node)
        {
            if (Properties.Count == 0)
                return;

            var defaultsNode = new ObjectListNode
            {
                Text = "Default Values",
                ImageKey = "UDefaultProperty",
                SelectedImageKey = "UDefaultProperty"
            };

            var tagNodes = Properties.Select(tag => new DefaultObjectNode(tag) { Text = tag.Name });
            defaultsNode.Nodes.AddRange(tagNodes.ToArray<TreeNode>());
            node.Nodes.Add(defaultsNode);
        }

        private static TreeNode CreateObjectNode(UObject obj, string imageName = "")
        {
            var objectNode = new ObjectNode(obj) { Text = obj.Name };
            obj.InitializeNodes(objectNode);
            if (imageName != string.Empty)
            {
                objectNode.ImageKey = imageName;
                objectNode.SelectedImageKey = imageName;
            }

            if (obj.DeserializationState.HasFlag(ObjectState.Errorlized))
            {
                objectNode.ForeColor = System.Drawing.Color.Red;
            }

            return objectNode;
        }

        protected static void AddObjectListNode<T>(
            TreeNode parentNode,
            string text,
            IEnumerable<T> objects,
            string imageKey = "TreeView") where T : UObject
        {
            var children = objects.ToList();
            if (!children.Any()) return;

            var objectListNode = new ObjectListNode(imageKey) { Text = text };
            AddObjectNodes(objectListNode, children);
            parentNode.Nodes.Add(objectListNode);
        }

        protected static void AddObjectNodes<T>(
            TreeNode parentNode,
            List<T> objects
        ) where T : UObject
        {
            var nodes = objects.Select(child => CreateObjectNode(child, child.GetImageName()));
            parentNode.Nodes.AddRange(nodes.ToArray());
        }
    }
}
#endif
