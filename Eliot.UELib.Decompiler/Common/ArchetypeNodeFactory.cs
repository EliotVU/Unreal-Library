using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UELib.Decompiler.Common.Nodes;
using UELib.Decompiler.Nodes;
using UELib.ObjectModel.Annotations;

namespace UELib.Decompiler.Common;

public static class ArchetypeNodeFactory
{
    public static ArchetypeTagAssignmentNode? Create(object target, MemberInfo memberInfo)
    {
        var exportAttr = memberInfo.GetCustomAttribute<OutputAttribute>();
        if (exportAttr == null) return null;

        object? value;
        switch (memberInfo)
        {
            case FieldInfo field:
                value = field.GetValue(target);
                break;

            case PropertyInfo field:
                value = field.GetValue(target);
                break;

            default:
                throw new NotSupportedException();
        }

        // Skip over members with default values.
        var defaultAttr = memberInfo.GetCustomAttribute<DefaultValueAttribute>();
        if (defaultAttr != null)
        {
            if (value == defaultAttr.Value)
            {
                return null;
            }
        }
        else if (value == null)
        {
            return null;
        }

        switch (exportAttr.Slot)
        {
            case OutputSlot.Parameter:
                return new ArchetypeParameterAssignmentNode
                {
                    LValue = NodeFactory.Create(memberInfo), RValue = NodeFactory.Create(value)
                };

            case OutputSlot.Property:
                if (exportAttr.Flags.HasFlag(OutputFlags.ShorthandProperty))
                {
                    return new ArchetypeShorthandAssignmentNode
                    {
                        LValue = NodeFactory.Create(memberInfo), RValue = NodeFactory.Create(value)
                    };
                }

                return new ArchetypePropertyAssignmentNode
                {
                    LValue = NodeFactory.Create(memberInfo), RValue = NodeFactory.Create(value)
                };

            default:
                throw new NotSupportedException();
        }
    }

    public static IEnumerable<ArchetypeTagAssignmentNode> Create(object target)
    {
        var members = target
            .GetType()
            .FindMembers(
                MemberTypes.Field | MemberTypes.Property,
                BindingFlags.Public | BindingFlags.Instance,
                (info, criteria) => true, null
            );

        return members
            .Select(member => Create(target, member))
            .Where(node => node != null)!
            .ToList<ArchetypeTagAssignmentNode>();
    }
}
