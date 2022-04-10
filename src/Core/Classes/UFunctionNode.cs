#if Forms
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UFunction
    {
        protected override void InitNodes(TreeNode node)
        {
            node.ToolTipText = FormatHeader();
            _ParentNode = AddSectionNode(node, nameof(UFunction));

            var funcFlagsNode = AddTextNode(_ParentNode, $"FunctionFlags:{UnrealMethods.FlagToString(FunctionFlags)}");
            funcFlagsNode.ToolTipText =
                UnrealMethods.FlagsListToString(UnrealMethods.FlagsToList(typeof(Flags.FunctionFlags), FunctionFlags));

            if (RepOffset > 0)
            {
                AddTextNode(_ParentNode, $"Replication Offset:{RepOffset}");
            }

            base.InitNodes(_ParentNode);
        }

        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);
            AddObjectListNode(node, "Parameters", Params, nameof(UProperty));
            AddObjectListNode(node, "Locals", Locals, nameof(UProperty));
        }

        public override string GetImageName()
        {
            var name = string.Empty;
            if (HasFunctionFlag(Flags.FunctionFlags.Event))
            {
                name = "Event";
            }
            else if (HasFunctionFlag(Flags.FunctionFlags.Delegate))
            {
                name = "Delegate";
            }
            else if (HasFunctionFlag(Flags.FunctionFlags.Operator))
            {
                name = "Operator";
            }

            if (name != string.Empty)
            {
                if (IsProtected())
                {
                    return $"{name}-Protected";
                }
                if (IsPrivate())
                {
                    return $"{name}-Private";
                }
                return name;
            }

            return base.GetImageName();
        }
    }
}
#endif