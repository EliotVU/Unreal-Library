using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record ModifierNode(ModifierNode.ModifierKind Modifier) : Node
{
    public enum ModifierKind
    {
        Specifier,
        Group
    }

    public ModifierKind Modifier = Modifier;

    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}