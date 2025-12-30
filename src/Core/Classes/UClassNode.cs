#if Forms
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UClass
    {
        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);

            AddObjectListNode(node, "Constants", EnumerateFields<UConst>().Reverse(), nameof(UConst));
            AddObjectListNode(node, "Enumerations", EnumerateFields<UEnum>().Reverse(), nameof(UEnum));
            AddObjectListNode(node, "Structures", EnumerateFields<UStruct>().Where(field => field.IsPureStruct()).Reverse(), nameof(UStruct));
            AddObjectListNode(node, "Variables", EnumerateFields<UProperty>(), nameof(UProperty));
            AddObjectListNode(node, "Functions", EnumerateFields<UFunction>().Reverse(), nameof(UFunction));
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
