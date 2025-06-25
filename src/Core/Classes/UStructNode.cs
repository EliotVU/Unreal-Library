#if Forms
using System.Linq;
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UStruct
    {
        protected override void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UStruct));
            if (IsPureStruct())
            {
                var sFlagsNode = AddTextNode(_ParentNode, $"Struct Flags:{(ulong)StructFlags:X8}");
                sFlagsNode.ToolTipText = StructFlags.ToString(Package.Branch.EnumFlagsMap[typeof(StructFlag)]);
            }

            AddTextNode(_ParentNode, $"Script Size:{ScriptSize}");
            base.InitNodes(_ParentNode);
        }

        protected override void AddChildren(TreeNode node)
        {
            if (ScriptText != null)
            {
                AddObjectNode(node, ScriptText, nameof(UObject));
            }

            if (CppText != null)
            {
                AddObjectNode(node, CppText, nameof(UObject));
            }

            if (ProcessedText != null)
            {
                AddObjectNode(node, ProcessedText, nameof(UObject));
            }

            AddObjectListNode(node, "Children", EnumerateFields().Reverse(), nameof(UObject));
            AddObjectListNode(node, "Constants", EnumerateFields<UConst>().Reverse(), nameof(UConst));
            AddObjectListNode(node, "Enumerations", EnumerateFields<UEnum>().Reverse(), nameof(UEnum));
            AddObjectListNode(node, "Structures", EnumerateFields<UStruct>().Where(field => field.IsPureStruct()).Reverse(), nameof(UStruct));
            // Not if the upper class is a function; UFunction adds locals and parameters instead
            if (GetType() != typeof(UFunction))
            {
                AddObjectListNode(node, "Variables", EnumerateFields<UProperty>(), nameof(UProperty));
            }
        }

        protected override void PostAddChildren(TreeNode node)
        {
            if (Properties == null || Properties.Count <= 0)
                return;

            var defNode = new ObjectListNode
            {
                Text = "Default Values",
                ImageKey = "UDefaultProperty",
                SelectedImageKey = "UDefaultProperty"
            };
            node.Nodes.Add(defNode);
            foreach (var def in Properties)
            {
                var objN = new DefaultObjectNode(def) { Text = def.Name };
                defNode.Nodes.Add(objN);
            }
        }
    }
}
#endif
