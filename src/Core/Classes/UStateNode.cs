#if Forms
using System.Linq;
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UState
    {
        protected override void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UState));

            if (GetType() == typeof(UState))
            {
                var stateFlagsNode = AddTextNode(_ParentNode, $"State Flags:{(ulong)StateFlags:X8}");
                stateFlagsNode.ToolTipText = StateFlags.ToString();
            }

            base.InitNodes(_ParentNode);
        }

        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);
            AddObjectListNode(node, "Functions", EnumerateFields<UFunction>().Reverse(), nameof(UFunction));
        }
    }
}
#endif
