#if Forms
using System.Windows.Forms;

namespace UELib.Core
{
    public partial class UField
    {
        protected override void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UField));
            AddSimpleObjectNode(_ParentNode, Super, "SuperField", Super != null ? Super.GetImageName() : "");
            AddSimpleObjectNode(_ParentNode, NextField, "NextField", NextField != null ? NextField.GetImageName() : "");
            base.InitNodes(_ParentNode);
        }
    }
}
#endif