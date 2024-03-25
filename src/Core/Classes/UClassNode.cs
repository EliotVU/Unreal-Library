#if Forms
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UClass
    {
        protected override void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UClass));
            AddSimpleObjectNode(_ParentNode, Within, "Within", Within != null ? Within.GetImageName() : "");

            var classFlagsNode = AddTextNode(_ParentNode, $"Class Flags:{UnrealMethods.FlagToString(ClassFlags)}");
            classFlagsNode.ToolTipText = UnrealMethods.FlagsListToString(
                UnrealMethods.FlagsToList(typeof(Flags.ClassFlags), ClassFlags)
            );

            base.InitNodes(_ParentNode);
        }

        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);
            AddObjectListNode(node, "States", States, nameof(UState));
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