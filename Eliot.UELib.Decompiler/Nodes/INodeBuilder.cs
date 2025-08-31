namespace UELib.Decompiler.Nodes;

public interface INodeBuilder<TNode, in TObject>
    where TNode : INode<TNode>
{
    TNode Build(TObject obj, IVisitor<TNode> visitor);
}
