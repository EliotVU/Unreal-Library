#if Forms
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UState
    {
        protected override void InitNodes(TreeNode node)
        {
            base.InitNodes(node);

            if (StateFlags != 0)
            {
                var stateFlagsNode = AddTextNode(node, $"State Flags:{(ulong)StateFlags:X8}");
                stateFlagsNode.ToolTipText = StateFlags.ToString(Package.Branch.EnumFlagsMap[typeof(StateFlag)]);
            }
        }

        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);
            AddObjectListNode(node, "Functions", EnumerateFields<UFunction>().Reverse(), nameof(UFunction));
        }
    }
}
#endif
