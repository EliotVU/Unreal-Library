using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UELib.Core;
using UELib.Decompiler.Builders;
using UELib.Decompiler.Common.Nodes;
using UELib.Decompiler.Nodes;
using UELib.Engine;
using UELib.ObjectModel.Annotations;

namespace UELib.Decompiler.Common;

public class ArchetypeNodeTreeBuilder : IVisitor<Node>
{
    // Unfortunately we cannot use the INodeBuilder interface as the value type :(
    // So as a workaround, let's resort to a delegate.
    /// <summary>
    /// TODO: Automate this using class attributes.
    /// </summary>
    private static readonly Dictionary<Type, Func<(object, IVisitor<Node>), Node>> s_nodeBuilders = new()
    {
        { typeof(UModel), (args) => new UModelArchetypeNodeBuilder().Build((UModel)args.Item1, args.Item2) },
        { typeof(UPolys), (args) => new UPolysArchetypeNodeBuilder().Build((UPolys)args.Item1, args.Item2) },
        { typeof(Poly), (args) => new PolyArchetypeNodeBuilder().Build((Poly)args.Item1, args.Item2) },
    };

    public Node Visit(IAcceptable visitable)
    {
        // Exact type match, maybe come up with something to make it derivable?
        if (s_nodeBuilders.TryGetValue(visitable.GetType(), out var action))
        {
            return action((visitable, this));
        }

        // otherwise fallback to the default UObject implementation...
        if (visitable is UObject obj)
        {
            return Visit(obj);
        }

        throw new NotImplementedException(visitable.GetType().ToString());
    }

    public Node Visit(UStruct.UByteCodeDecompiler.Token token)
    {
        throw new NotImplementedException();
    }

    public Node Visit(UObject obj)
    {
        var memberNodes = ArchetypeNodeFactory
            .Create(obj)
            .ToList();

        var paramNodes = memberNodes
            .OfType<ArchetypeParameterAssignmentNode>();

        var propertyNodes = memberNodes
            .OfType<ArchetypePropertyAssignmentNode>();

        var outputAttr = obj.GetType().GetCustomAttribute<OutputAttribute>();
        string name = outputAttr?.Identifier ?? "Object";

        return new ArchetypeConstructionNode(obj, new UName(name), paramNodes, propertyNodes);
    }
}