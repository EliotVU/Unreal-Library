#if Forms
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UProperty
    {
        protected override void InitNodes(TreeNode node)
        {
            base.InitNodes(node);

            if (PropertyFlags != 0)
            {
                var propertyFlagsNode = AddTextNode(node,
                    $"Property Flags:{(ulong)PropertyFlags:X8}"
                );
                propertyFlagsNode.ToolTipText = PropertyFlags.ToString(Package.Branch.EnumFlagsMap[typeof(PropertyFlag)]);
            }
        }

        public override string GetImageName()
        {
            if (HasPropertyFlag(PropertyFlag.ReturnParm))
            {
                return "ReturnValue";
            }

            string which = base.GetImageName();
            if (IsProtected())
            {
                return $"{which}-Protected";
            }
            if (IsPrivate())
            {
                return $"{which}-Private";
            }
            return which;
        }
    }
}
#endif
