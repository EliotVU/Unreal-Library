using UELib.Core;
using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public abstract record class ObjectDeclarationNode<T>(T Object) : IdentifierNode(new UName(Object.Name))
    where T : UObject
{
    public T Object = Object;

    public override void Accept(INodeVisitor visitor) => visitor.Visit(this);

    public override TResult Accept<TResult>(INodeVisitor<TResult> visitor) => visitor.Visit(this);
}