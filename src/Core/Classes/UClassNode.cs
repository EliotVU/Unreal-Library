#if Forms
using System.Linq;
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UClass
    {
        protected override void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UClass));
            AddSimpleObjectNode(_ParentNode, ClassWithin, "Within", ClassWithin != null ? ClassWithin.GetImageName() : "");

            var classFlagsNode = AddTextNode(_ParentNode, $"Class Flags:{(ulong)ClassFlags:X8}");
            classFlagsNode.ToolTipText = ClassFlags.ToString(Package.Branch.EnumFlagsMap[typeof(ClassFlag)]);

            base.InitNodes(_ParentNode);
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
