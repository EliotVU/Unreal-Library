using System.Collections.Generic;
using UELib.Core;
using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record ArchetypeConstructionNode(
    object Archetype,
    UName ClassKeyword,
    IEnumerable<Node>? Parameters = null,
    IEnumerable<Node>? Children = null)
    : Node
{
    public object Archetype = Archetype;
    public IEnumerable<Node>? Children = Children;
    public UName ClassKeyword = ClassKeyword;

    public IEnumerable<Node>? Parameters = Parameters;

    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}