#if Forms
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UClass
    {
        protected override void InitNodes(TreeNode node)
        {
            base.InitNodes(node);

            if (ClassFlags != 0)
            {
                var classFlagsNode = AddTextNode(node, $"Class Flags:{(ulong)ClassFlags:X8}");
                classFlagsNode.ToolTipText = ClassFlags.ToString(Package.Branch.EnumFlagsMap[typeof(ClassFlag)]);
            }

            if (ClassWithin != null)
            {
                AddSimpleObjectNode(node, ClassWithin, "Within", ClassWithin.GetImageName());
            }
        }

        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);
            AddObjectListNode(node, "States", EnumerateFields<UState>().Reverse(), nameof(UState));
        }

        public override string GetImageName()
        {
            if (IsClassInterface())
            {
                return "Interface";
            }
            if (IsClassWithin())
            {
                return "UClass-Within";
            }

            return base.GetImageName();
        }
    }
}
#endif
