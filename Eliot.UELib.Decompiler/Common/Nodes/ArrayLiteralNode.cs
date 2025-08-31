using System.Collections.Generic;
using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record ArrayLiteralNode(IEnumerable<Node> Value) : LiteralNode<IEnumerable<Node>>(Value)
{
    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}