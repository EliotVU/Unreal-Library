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
