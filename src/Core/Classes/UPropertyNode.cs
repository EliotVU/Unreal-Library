﻿#if Forms
using System.Windows.Forms;
using UELib.Flags;

namespace UELib.Core
{
    public partial class UProperty
    {
        protected override void InitNodes(TreeNode node)
        {
            _ParentNode = AddSectionNode(node, nameof(UProperty));
            var propertyFlagsNode = AddTextNode(_ParentNode,
                $"Property Flags:{(ulong)PropertyFlags:X8}"
            );
            propertyFlagsNode.ToolTipText = PropertyFlags.ToString();

            if (RepOffset > 0)
            {
                AddTextNode(_ParentNode, $"Replication Offset:{RepOffset}");
            }

            base.InitNodes(_ParentNode);
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
