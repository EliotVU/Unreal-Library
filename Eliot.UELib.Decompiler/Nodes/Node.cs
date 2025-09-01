using System.Collections.Generic;

namespace UELib.Decompiler.Nodes;

public abstract record class Node : INode<Node>
{
    public Node? Child { get; set; }
    public Node? Sibling { get; set; }

    public IEnumerable<Node> EnumerateChildren()
    {
        for (var child = Child; child != null; child = child.Sibling)
        {
            yield return child;
        }
    }

    public IEnumerable<T> EnumerateChildren<T>()
        where T : Node
    {
        for (var child = Child; child != null; child = child.Sibling)
        {
            if (child is T == false) continue;

            yield return (T)child;
        }
    }

    public IEnumerable<Node> EnumerateSiblings()
    {
        for (var sibling = Sibling; sibling != null; sibling = sibling.Sibling)
        {
            yield return sibling;
        }
    }

    public IEnumerable<T> EnumerateSiblings<T>()
        where T : Node
    {
        for (var sibling = Sibling; sibling != null; sibling = sibling.Sibling)
        {
            if (sibling is T == false) continue;

            yield return (T)sibling;
        }
    }

    public abstract void Accept(INodeVisitor visitor);
    public abstract TResult Accept<TResult>(INodeVisitor<TResult> visitor);
}
