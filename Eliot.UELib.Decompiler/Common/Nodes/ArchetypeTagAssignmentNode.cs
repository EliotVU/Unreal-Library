using UELib.Decompiler.Nodes;

namespace UELib.Decompiler.Common.Nodes;

public abstract record ArchetypeTagAssignmentNode : Node
{
    public Node LValue;
    public Node? RValue;
}