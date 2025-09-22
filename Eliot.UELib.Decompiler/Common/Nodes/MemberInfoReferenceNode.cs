using System.Diagnostics;
using System.Reflection;
using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record MemberInfoReferenceNode : Node
{
    public MemberInfo MemberInfo;

    public MemberInfoReferenceNode(MemberInfo memberInfo)
    {
        MemberInfo = memberInfo;
        Debug.Assert(MemberInfo != null);
    }

    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}