namespace UELib.Decompiler.Nodes;

public interface INode<TNode>
{
    TNode Child { get; set; }
    TNode Sibling { get; set; }
}
