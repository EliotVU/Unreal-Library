#if Forms
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

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

            node.ToolTipText = FormatHeader();
            InitNodes(node);
            AddChildren(node);
            PostAddChildren(node);

            node.ImageKey = GetImageName();
            node.SelectedImageKey = node.ImageKey;
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
            var flagNode = AddTextNode(_ParentNode, $"ObjectFlags:{UnrealMethods.FlagToString(_ObjectFlags)}");
            flagNode.ToolTipText = UnrealMethods.FlagsListToString(
                UnrealMethods.FlagsToList(typeof(Flags.ObjectFlagsLO), typeof(Flags.ObjectFlagsHO), _ObjectFlags)
            );

            AddTextNode(_ParentNode, $"Size:{ExportTable.SerialSize}");
            AddTextNode(_ParentNode, $"Offset:{ExportTable.SerialOffset}");
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
            IList<T> objects,
            string imageName = "TreeView"
        ) where T : UObject
        {
            if (objects == null || !objects.Any())
                return null;

            var listNode = new ObjectListNode(imageName) { Text = title };
            foreach (var obj in objects)
            {
                AddObjectNode(listNode, obj, obj.GetType().Name);
            }

            parentNode.Nodes.Add(listNode);
            return listNode;
        }
    }
}
#endif