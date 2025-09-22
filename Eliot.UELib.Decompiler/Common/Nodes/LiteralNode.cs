using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public abstract record LiteralNode<T>(T Value) : Node
{
    public T Value = Value;

    protected LiteralNode(ref T value) : this(value)
    {
    }
}