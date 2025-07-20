using System;

namespace UELib.ObjectModel.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class UnrealPropertyAttribute : Attribute
{
    public readonly string Name;

    public UnrealPropertyAttribute()
    {
    }

    public UnrealPropertyAttribute(string name)
    {
        Name = name;
    }
}