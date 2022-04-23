#if Forms
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
                var stateFlagsNode = AddTextNode(_ParentNode, $"State Flags:{UnrealMethods.FlagToString(_StateFlags)}");
                stateFlagsNode.ToolTipText =
                    UnrealMethods.FlagsListToString(UnrealMethods.FlagsToList(typeof(Flags.StateFlags), _StateFlags));
            }

            base.InitNodes(_ParentNode);
        }

        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);
            AddObjectListNode(node, "Functions", Functions, nameof(UFunction));
        }
    }
}
#endif