#if Forms
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UProperty
    {
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

    public partial class UArrayProperty
    {
        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);

            if (InnerProperty != null) AddObjectNodes(node, [InnerProperty]);
        }
    }

    public partial class UFixedArrayProperty
    {
        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);

            if (InnerProperty != null) AddObjectNodes(node, [InnerProperty]);
        }
    }

    public partial class UMapProperty
    {
        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);

            if (KeyProperty != null) AddObjectNodes(node, [KeyProperty]);
            if (ValueProperty != null) AddObjectNodes(node, [ValueProperty]);
        }
    }
}
#endif
