#if Forms
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UField
    {
        protected override void InitNodes(TreeNode node)
        {
            base.InitNodes(node);

            if (Super != null)
            {
                AddSimpleObjectNode(node, Super, "SuperStruct", Super.GetImageName());
            }
            if (NextField != null)
            {
                AddSimpleObjectNode(node, NextField, "NextField", NextField.GetImageName());
            }
        }
    }
}
#endif
