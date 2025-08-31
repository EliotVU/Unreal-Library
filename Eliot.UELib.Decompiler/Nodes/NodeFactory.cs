using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UELib.Core;
using UELib.Decompiler.Common.Nodes;

namespace UELib.Decompiler.Nodes;

public static class NodeFactory
{
    public static MemberInfoReferenceNode Create(MemberInfo memberInfo)
    {
        return new MemberInfoReferenceNode(memberInfo);
    }

    public static StringLiteralNode Create(string value)
    {
        return new StringLiteralNode(value);
    }

    public static NameLiteralNode Create(UName value)
    {
        return new NameLiteralNode(value);
    }

    public static ObjectLiteralNode Create(UObject? value)
    {
        return new ObjectLiteralNode(value);
    }

    public static StructLiteralNode<T> Create<T>(ref T value)
        where T : struct
    {
        return new StructLiteralNode<T>(ref value);
    }

    public static Node Create(object? value)
    {
        if (value is IList list)
        {
            var enumerable = list.Cast<object>();
            return new ArrayLiteralNode(enumerable.Select(Create).ToList());
        }

        switch (value)
        {
            case byte v:
                return new NumberLiteralNode(v);

            case short v:
                return new NumberLiteralNode(v);

            case ushort v:
                return new NumberLiteralNode(v);

            case int v:
                return new NumberLiteralNode(v);

            case uint v:
                return new NumberLiteralNode(v);

            case float v:
                return new NumberLiteralNode(v);

            case string v:
                return Create(v);

            case UName v:
                return Create(v);

            case UColor v:
                return Create(ref v);

            case UVector v:
                return Create(ref v);

            case UObject v:
                return Create(v);

            case null:
                return Create((UObject?)null);
        }

        throw new NotSupportedException($"Couldn't create a matching node for type '{value.GetType()}'");
    }

    public static Node Create(object target, FieldInfo info)
    {
        object? value = info.GetValue(target);
        return Create(value);
    }

    public static Node Create(object target, PropertyInfo info)
    {
        object? value = info.GetValue(target);
        return Create(value);
    }

    public static Node Create(object target, MemberInfo info)
    {
        object? value;

        switch (info.MemberType & (MemberTypes.Property | MemberTypes.Field))
        {
            case MemberTypes.Field:
                value = ((FieldInfo)info).GetValue(target);
                break;

            case MemberTypes.Property:
                value = ((PropertyInfo)info).GetValue(target);
                break;

            default:
                throw new NotSupportedException();
        }

        return Create(value);
    }
}
