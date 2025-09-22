using UELib.Core;
using UELib.Decompiler.Common.Nodes;

namespace UELib.Decompiler.Nodes;

public interface INodeVisitor
{
    void Visit(Node node);
    void Visit(MultiNode node);
    void Visit(LineSeparatorNode node);
    void Visit(NumberLiteralNode node);
    void Visit(StringLiteralNode node);
    void Visit(NameLiteralNode node);
    void Visit<T>(StructLiteralNode<T> node) where T : struct;
    void Visit(StructLiteralNode<UVector> node);
    void Visit(ObjectLiteralNode node);
    void Visit(ArrayLiteralNode node);
    void Visit(IdentifierNode node);
    void Visit(ModifierNode node);
    void Visit(MemberInfoReferenceNode node);
    void Visit<T>(ObjectDeclarationNode<T> node) where T : UObject;
    void Visit(ObjectDeclarationNode<UConst> node);
    void Visit(ObjectDeclarationNode<UEnum> node);
    void Visit(ObjectDeclarationNode<UClass> node);
    void Visit(ObjectDeclarationNode<UFunction> node);
    void Visit(ObjectDeclarationNode<UState> node);
    void Visit(ObjectDeclarationNode<UStruct> node);
    void Visit(ObjectDeclarationNode<UProperty> node);
    void Visit(ArchetypeConstructionNode node);
    void Visit(ArchetypeParameterAssignmentNode node);
    void Visit(ArchetypePropertyAssignmentNode node);
    void Visit(ArchetypeShorthandAssignmentNode node);
}

public interface INodeVisitor<out TResult>
{
    TResult Visit(Node node);
    TResult Visit(MultiNode node);
    TResult Visit(LineSeparatorNode node);
    TResult Visit(NumberLiteralNode node);
    TResult Visit(StringLiteralNode node);
    TResult Visit(NameLiteralNode node);
    TResult Visit<T>(StructLiteralNode<T> node) where T : struct;
    TResult Visit(StructLiteralNode<UVector> node);
    TResult Visit(ObjectLiteralNode node);
    TResult Visit(ArrayLiteralNode node);
    TResult Visit(IdentifierNode node);
    TResult Visit(ModifierNode node);
    TResult Visit(MemberInfoReferenceNode node);
    TResult Visit<T>(ObjectDeclarationNode<T> node) where T : UObject;
    TResult Visit(ObjectDeclarationNode<UConst> node);
    TResult Visit(ObjectDeclarationNode<UEnum> node);
    TResult Visit(ObjectDeclarationNode<UClass> node);
    TResult Visit(ObjectDeclarationNode<UFunction> node);
    TResult Visit(ObjectDeclarationNode<UState> node);
    TResult Visit(ObjectDeclarationNode<UStruct> node);
    TResult Visit(ObjectDeclarationNode<UProperty> node);
    TResult Visit(ArchetypeConstructionNode node);
    TResult Visit(ArchetypeParameterAssignmentNode node);
    TResult Visit(ArchetypePropertyAssignmentNode node);
    TResult Visit(ArchetypeShorthandAssignmentNode node);
}