#if Forms
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UFunction
    {
        protected override void InitNodes(TreeNode node)
        {
            base.InitNodes(node);

            node.ToolTipText = FormatHeader();

            if (FunctionFlags != 0)
            {
                var funcFlagsNode = AddTextNode(node, $"FunctionFlags:{(ulong)FunctionFlags:X8}");
                funcFlagsNode.ToolTipText = FunctionFlags.ToString(Package.Branch.EnumFlagsMap[typeof(FunctionFlag)]);
            }
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
