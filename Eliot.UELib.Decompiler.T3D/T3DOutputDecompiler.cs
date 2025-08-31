using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using UELib.Core;
using UELib.Decompiler.Common;
using UELib.Decompiler.Common.Nodes;
using UELib.Decompiler.Nodes;
using UELib.ObjectModel.Annotations;
using UELib.UnrealScript;

namespace UELib.Decompiler.T3D;

/// <summary>
///     Decompiles objects to a Text 3D format.
///     The T3D format is used extensively by the Unreal Engine to manage the copying and pasting of objects between the
///     editor and the user.
/// </summary>
public class T3DOutputDecompiler(TextOutputStream outputStream, IVisitor<Node>[] transformers)
    : IOutputDecompiler<IAcceptable>, INodeVisitor
{
    private CancellationToken _CancellationToken = CancellationToken.None;

    public T3DOutputDecompiler(TextOutputStream outputStream) : this(outputStream, [new ArchetypeNodeTreeBuilder()])
    {
    }

    public void Visit(Node node) => Visit((dynamic)node);

    public void Visit(MultiNode node)
    {
        for (int i = 0; i < node.Nodes.Count(); i++)
        {
            if (_CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var subNode = node.Nodes.ElementAt(i);
            subNode.Accept(this);
            if (i + 1 < node.Nodes.Count())
            {
                outputStream.WriteSpace();
            }
        }
    }

    public void Visit(LineSeparatorNode node) => outputStream.WriteLine();

    public void Visit(NumberLiteralNode node) => outputStream.Write(PropertyDisplay.FormatLiteral(node.Value));

    public void Visit(StringLiteralNode node)
    {
        outputStream.WriteDoubleQuote();
        outputStream.WriteEscaped(node.Value);
        outputStream.WriteDoubleQuote();
    }

    public void Visit(NameLiteralNode node) => outputStream.Write(node.Value);

    public void Visit<T>(StructLiteralNode<T> node) where T : struct => throw new NotImplementedException();

    public void Visit(StructLiteralNode<UVector> node) =>
        outputStream.Write(PropertyDisplay.FormatExport(ref node.Value));

    public void Visit(ObjectLiteralNode node)
    {
        if (node.Value == null)
        {
            outputStream.WriteKeyword("none");
            return;
        }

        if (node.Value.Class != null)
        {
            outputStream.WriteReference(node.Value.Class, node.Value.Class.Name, node);
        }
        else if (node.Value.ImportTable != null)
        {
            outputStream.WriteToken(node.Value.ImportTable.ClassName);
        }

        outputStream.WriteSingleQuote();
        node.Value.GetPath(out var chain);
        for (int i = chain.Count - 1; i >= 1; i--)
        {
            outputStream.WriteReference(chain[i], chain[i].Name, null);
            outputStream.WriteDot();
        }

        outputStream.WriteReference(node.Value, node.Value.Name, null);
        outputStream.WriteSingleQuote();
    }

    public void Visit(ArrayLiteralNode node) => throw new NotImplementedException();

    public void Visit(IdentifierNode node) => outputStream.WriteToken(node.Identifier);

    public void Visit(ModifierNode node) => throw new NotImplementedException();

    public void Visit(MemberInfoReferenceNode node)
    {
        var outputIdentifier = node.MemberInfo.GetCustomAttribute<OutputAttribute>();
        outputStream.WriteToken(outputIdentifier?.Identifier ?? node.MemberInfo.Name);
    }

    public void Visit<T>(ObjectDeclarationNode<T> node) where T : UObject => throw new NotImplementedException();

    public void Visit(ObjectDeclarationNode<UEnum> node) => throw new NotImplementedException();

    public void Visit(ObjectDeclarationNode<UState> node) => throw new NotImplementedException();

    public void Visit(ObjectDeclarationNode<UStruct> node) => throw new NotImplementedException();
    public void Visit(ObjectDeclarationNode<UProperty> node) => throw new NotImplementedException();

    public void Visit(ObjectDeclarationNode<UClass> node) => throw new NotImplementedException();
    public void Visit(ObjectDeclarationNode<UFunction> node) => throw new NotImplementedException();

    public void Visit(ObjectDeclarationNode<UConst> node) => throw new NotImplementedException();

    public void Visit(ArchetypeConstructionNode node)
    {
        outputStream.WriteKeyword("Begin");
        outputStream.WriteSpace();
        outputStream.WriteKeyword(node.ClassKeyword);

        // Intrinsic Parameters
        if (node.Parameters != null)
        {
            foreach (var param in node.Parameters)
            {
                outputStream.WriteSpace();
                param.Accept(this);
            }
        }

        switch (node.Archetype)
        {
            case UTextBuffer uTextBuffer:
                outputStream.WriteIndented(() =>
                {
                    outputStream.WriteLine();
                    outputStream.Write(uTextBuffer.ScriptText);
                });

                break;
        }

        // Script Properties
        if (node.Archetype is UObject { Properties.Count: > 0 } uObject)
        {
            outputStream.WriteIndented(() =>
            {
                foreach (var scriptProperty in uObject.Properties)
                {
                    if (_CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    outputStream.WriteLine();

                    // FIXME: Legacy approach
                    outputStream.WriteReference(scriptProperty, scriptProperty.Name, null);
                    outputStream.WriteAssignment();
                    outputStream.Write(scriptProperty.Value);
                }
            });
        }

        // Intrinsic Properties
        if (node.Children != null)
        {
            outputStream.WriteIndented(() =>
            {
                foreach (var child in node.Children)
                {
                    if (_CancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    outputStream.WriteLine();
                    child.Accept(this);
                }
            });
        }

        outputStream.WriteLine();
        outputStream.WriteKeyword("End");
        outputStream.WriteSpace();
        outputStream.WriteKeyword(node.ClassKeyword);
    }

    public void Visit(ArchetypeParameterAssignmentNode node)
    {
        if (node.RValue == null)
        {
            return;
        }

        node.LValue.Accept(this);
        outputStream.WriteAssignment();
        node.RValue.Accept(this);
    }

    public void Visit(ArchetypePropertyAssignmentNode node)
    {
        if (node.RValue == null)
        {
            return;
        }

        if (node.RValue is ArrayLiteralNode multiNode)
        {
            foreach (var subNode in multiNode.Value)
            {
                if (_CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                node.LValue.Accept(this);
                outputStream.WriteAssignment();
                subNode.Accept(this);
                outputStream.WriteLine();
            }

            return;
        }

        node.LValue.Accept(this);
        outputStream.WriteAssignment();
        node.RValue.Accept(this);
    }

    public void Visit(ArchetypeShorthandAssignmentNode node)
    {
        if (node.RValue == null)
        {
            return;
        }

        if (node.RValue is ArrayLiteralNode multiNode)
        {
            foreach (var subNode in multiNode.Value)
            {
                if (_CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                node.LValue.Accept(this);
                subNode.Accept(this);
                outputStream.WriteLine();
            }

            return;
        }

        node.LValue.Accept(this);
        // TODO: Properly indent based on the previous nodes output!
        outputStream.WriteSpace();
        node.RValue.Accept(this);
    }

    public bool CanDecompile(UObject? visitable) => visitable != null;

    public void Decompile(UObject visitable, CancellationToken cancellationToken)
    {
        Contract.Assert(CanDecompile(visitable), "Cannot decompile visitable");

        _CancellationToken = cancellationToken;
        Transform(visitable);
    }

    // Convert the visitable to a representative node.
    private void Transform(UObject visitable)
    {
        foreach (var transformer in transformers)
        {
            if (_CancellationToken.IsCancellationRequested)
            {
                break;
            }

            var node = transformer.Visit(visitable);
            node?.Accept(this);
        }
    }

    public bool CanDecompile(IAcceptable? visitable) => CanDecompile(visitable as UObject);

    public void Decompile(IAcceptable visitable, CancellationToken cancellationToken) =>
        Decompile((UObject)visitable, cancellationToken);
}
