using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UELib.Core;
using UELib.Decompiler.Common;
using UELib.Decompiler.Common.Nodes;
using UELib.Decompiler.Nodes;
using UELib.Engine;
using UELib.ObjectModel.Annotations;

namespace UELib.Decompiler.Builders;

public class UModelArchetypeNodeBuilder : INodeBuilder<Node, UModel>
{
    public Node Build(UModel obj, IVisitor<Node> visitor)
    {
        var outputAttr = obj.GetType().GetCustomAttribute<OutputAttribute>();
        string name = outputAttr?.Identifier ?? "Object";

        var node = new ArchetypeConstructionNode(obj, new UName(name),
            new List<Node> { ArchetypeNodeFactory.Create(obj, obj.GetType().GetMember("Name").First())! }
                .AsReadOnly());
        if (obj.Polys == null)
        {
            return node;
        }

        var modelNode = obj.Polys.Accept(visitor);
        modelNode.Sibling = node.Child;
        node.Child = modelNode;

        return node;
    }
}
