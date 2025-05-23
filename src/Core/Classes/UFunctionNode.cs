#if Forms
using System.Linq;
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UFunction
    {
        protected override void InitNodes(TreeNode node)
        {
            node.ToolTipText = FormatHeader();
            _ParentNode = AddSectionNode(node, nameof(UFunction));

            var funcFlagsNode = AddTextNode(_ParentNode, $"FunctionFlags:{(ulong)FunctionFlags:X8}");
            funcFlagsNode.ToolTipText = FunctionFlags.ToString();

            if (RepOffset > 0)
            {
                AddTextNode(_ParentNode, $"Replication Offset:{RepOffset}");
            }

            base.InitNodes(_ParentNode);
        }

        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);
            AddObjectListNode(node, "Parameters", EnumerateFields<UProperty>().Where(field => field.IsParm()), nameof(UProperty));
            AddObjectListNode(node, "Locals", EnumerateFields<UProperty>().Where(field => !field.IsParm()), nameof(UProperty));
        }

        public override string GetImageName()
        {
            var name = string.Empty;
            if (HasFunctionFlag(FunctionFlag.Event))
            {
                name = "Event";
            }
            else if (IsDelegate())
            {
                name = "Delegate";
            }
            else if (HasFunctionFlag(FunctionFlag.Operator))
            {
                name = "Operator";
            }

            if (name != string.Empty)
            {
                if (HasFunctionFlag(FunctionFlag.Protected))
                {
                    return $"{name}-Protected";
                }
                if (HasFunctionFlag(FunctionFlag.Private))
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
