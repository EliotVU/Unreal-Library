#if Forms
using System.Collections.Generic;
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UStruct
    {
        protected override void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UStruct));
            if (IsPureStruct())
            {
                var sFlagsNode = AddTextNode(_ParentNode, $"Struct Flags:{UnrealMethods.FlagToString(StructFlags)}");
                sFlagsNode.ToolTipText =
                    UnrealMethods.FlagsListToString(UnrealMethods.FlagsToList(typeof(Flags.StructFlags), StructFlags));
            }

            AddTextNode(_ParentNode, $"Script Size:{DataScriptSize}");
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

            var children = new List<UObject>();
            for (var child = Children; child != null; child = child.NextField)
            {
                children.Insert(0, child);
            }
            AddObjectListNode(node, "Children", children, nameof(UObject));
            AddObjectListNode(node, "Constants", Constants, nameof(UConst));
            AddObjectListNode(node, "Enumerations", Enums, nameof(UEnum));
            AddObjectListNode(node, "Structures", Structs, nameof(UStruct));
            // Not if the upper class is a function; UFunction adds locals and parameters instead
            if (GetType() != typeof(UFunction))
            {
                AddObjectListNode(node, "Variables", Variables, nameof(UProperty));
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