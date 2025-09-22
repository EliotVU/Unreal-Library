using System.Collections.Generic;
using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record MultiNode(IEnumerable<Node> Nodes) : Node
{
    public IEnumerable<Node> Nodes = Nodes;

    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}
