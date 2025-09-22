using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public record StructLiteralNode<T> : LiteralNode<T>
    where T : struct
{
    public StructLiteralNode(ref T value) : base(ref value)
    {
    }

    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}
