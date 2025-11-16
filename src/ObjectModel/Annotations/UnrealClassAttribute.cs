using System;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Branch;
using UELib.Core;
using UELib.Flags;

namespace UELib.ObjectModel.Annotations;

/// <summary>
/// Registers an internal class as an unreal intrinsic class.
///
/// The target class name must begin with the letter "U" and the class must be derived from <see cref="UObject"/>.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class UnrealClassAttribute : Attribute
{
    public readonly UName ClassName;
    public readonly UName ClassPackageName;
    public readonly UName SuperClassName;
    public readonly InternalClassFlags InternalClassFlags;
    public readonly ClassFlag[] ClassFlags;

    public UnrealClassAttribute()
    {
        InternalClassFlags = InternalClassFlags.Default;
        ClassFlags = [ClassFlag.Intrinsic];
    }

    public UnrealClassAttribute(
        InternalClassFlags internalClassFlags = InternalClassFlags.Default,
        params ClassFlag[] classFlags)
    {
        InternalClassFlags = internalClassFlags;
        ClassFlags = [ClassFlag.Intrinsic, .. classFlags];
    }

    public UnrealClassAttribute(
        string className,
        string classPackageName,
        InternalClassFlags internalClassFlags = InternalClassFlags.Default,
        params ClassFlag[] classFlags)
    {
        Debug.Assert(!string.IsNullOrEmpty(className));
        Debug.Assert(!string.IsNullOrEmpty(classPackageName));

        ClassName = new UName(className);
        ClassPackageName = new UName(classPackageName);
        InternalClassFlags = internalClassFlags;
        ClassFlags = [ClassFlag.Intrinsic, .. classFlags];
    }

    public UnrealClassAttribute(
        string className,
        string classPackageName,
        string superName,
        InternalClassFlags internalClassFlags = InternalClassFlags.Default,
        params ClassFlag[] classFlags)
    {
        Debug.Assert(!string.IsNullOrEmpty(className));
        Debug.Assert(!string.IsNullOrEmpty(superName));
        Debug.Assert(!string.IsNullOrEmpty(classPackageName));

        ClassName = new UName(className);
        ClassPackageName = new UName(classPackageName);
        SuperClassName = new UName(superName);
        InternalClassFlags = internalClassFlags;
        ClassFlags = [ClassFlag.Intrinsic, .. classFlags];
    }

    public UClass CreateStaticClass(Type internalClassType, UName className)
    {
        var staticClass = new UClass
        {
            Name = className,
            Package = UnrealPackage.TransientPackage,
            InternalFlags = InternalClassFlags,
            InternalType = internalClassType,
            ClassFlags = new UnrealFlags<ClassFlag>(new ulong[(int)ClassFlag.Max], ClassFlags),
        };

        return staticClass;
    }

    public UClass CreateStaticClass(
        Type internalClassType,
        UName className,
        InternalClassFlags internalClassFlags,
        params ClassFlag[] classFlags)
    {
        var staticClass = new UClass
        {
            Name = className,
            Package = UnrealPackage.TransientPackage,
            InternalFlags = InternalClassFlags | internalClassFlags,
            InternalType = internalClassType,
            ClassFlags = new UnrealFlags<ClassFlag>(new ulong[(int)ClassFlag.Max], [.. ClassFlags, .. classFlags])
        };

        return staticClass;
    }
}
