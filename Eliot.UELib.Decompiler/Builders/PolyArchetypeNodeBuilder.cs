using System.Linq;
using UELib.Core;
using UELib.Decompiler.Common;
using UELib.Decompiler.Common.Nodes;
using UELib.Decompiler.Nodes;
using UELib.Engine;

namespace UELib.Decompiler.Builders;

public class PolyArchetypeNodeBuilder : INodeBuilder<Node, Poly>
{
    public Node Build(Poly poly, IVisitor<Node> visitor)
    {
        var memberNodes = ArchetypeNodeFactory
            .Create(poly)
            .ToList();

        var paramNodes = memberNodes
            .OfType<ArchetypeParameterAssignmentNode>()
            .ToArray();

        var propertyNodes = memberNodes
            .OfType<ArchetypePropertyAssignmentNode>()
            .ToArray();

        return new ArchetypeConstructionNode(poly, new UName("Polygon"), paramNodes, propertyNodes);

        return new ArchetypeConstructionNode(poly, new UName("Polygon"), new[]
        {
            new ArchetypeParameterAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("ItemName")),
                RValue = poly.ItemName != null && !poly.ItemName.IsNone()
                    ? new NameLiteralNode(poly.ItemName)
                    : null
            },
            new ArchetypeParameterAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("Material")),
                RValue = poly.Material != null
                    ? new ObjectLiteralNode(poly.Material)
                    : null
            },
            new ArchetypeParameterAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("PolyFlags")),
                RValue = poly.PolyFlags != 0
                    ? new NumberLiteralNode(poly.PolyFlags)
                    : null
            },
            new ArchetypeParameterAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("Link")),
                RValue = poly.Link != -1
                    ? new NumberLiteralNode(poly.Link)
                    : null
            }
        }, new Node[]
        {
            new ArchetypeShorthandAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("Base")),
                RValue = new StructLiteralNode<UVector>(ref poly.Base)
            },
            new ArchetypeShorthandAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("Normal")),
                RValue = new StructLiteralNode<UVector>(ref poly.Normal)
            },
            new ArchetypeShorthandAssignmentNode
            {
                LValue = new IdentifierNode(new UName("Pan")),
                RValue = poly.PanU != 0 || poly.PanV != 0
                    ? new MultiNode(new Node[]
                    {
                        new ArchetypeParameterAssignmentNode
                        {
                            LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("PanU")),
                            RValue = new NumberLiteralNode(poly.PanU)
                        },
                        new ArchetypeParameterAssignmentNode
                        {
                            LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("PanV")),
                            RValue = new NumberLiteralNode(poly.PanV)
                        },
                    })
                    : null
            },
            new ArchetypeShorthandAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("TextureV")),
                RValue = new StructLiteralNode<UVector>(ref poly.TextureV)
            },
            new ArchetypeShorthandAssignmentNode
            {
                LValue = new MemberInfoReferenceNode(typeof(Poly).GetField("TextureV")),
                RValue = new StructLiteralNode<UVector>(ref poly.TextureV)
            },
            //new LineSeparatorNode(),
            //for (var i = 0; i < poly.NumVertices; i++)
            //{
            //    var vertex = poly.Vertex[i];
            //    _Output.WriteLine($"Vertex   {PropertyDisplay.FormatExport(vertex)}");
            //}
        });
    }
}