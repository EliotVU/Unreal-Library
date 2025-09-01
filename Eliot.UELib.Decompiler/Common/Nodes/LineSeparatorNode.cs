using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record LineSeparatorNode : Node
{
    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}