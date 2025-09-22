using UELib.Core;
using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record IdentifierNode(in UName Identifier) : Node
{
    public readonly UName Identifier = Identifier;

    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}
