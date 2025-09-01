using System.Linq;
using UELib.Core;
using UELib.Decompiler.Common.Nodes;
using UELib.Decompiler.Nodes;
using UELib.Engine;

namespace UELib.Decompiler.Builders;

public class ArchetypeBuilderUPolys : INodeBuilder<Node, UPolys>
{
    public Node Build(UPolys obj, IVisitor<Node> visitor)
    {
        var childNodes = obj.Element
            .Select(poly => poly.Accept(visitor))
            .ToArray();

        return new ArchetypeConstructionNode(obj,
            new UName("PolyList"),
            null,
            childNodes);
    }
}
