#if Forms
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UStruct
    {
        protected override void AddChildren(TreeNode node)
        {
            base.AddChildren(node);

            if (this is not UClass)
            {
                AddObjectNodes(node, EnumerateFields().Reverse().ToList());
            }
        }
    }
}
#endif
