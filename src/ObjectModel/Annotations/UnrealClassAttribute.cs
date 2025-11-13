using System;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Core;
using UELib.Flags;

namespace UELib.ObjectModel.Annotations;

[Flags]
public enum InternalClassFlags
{
    Default = LazyLoad,

    /// <summary>
    /// The object should not be preloaded, instead a manual call to <see cref="UObject.Load"/> is required.
    /// </summary>
    LazyLoad = 0,

    /// <summary>
    /// The object should be preloaded using an ordinary call to <see cref="UObject.Load"/> as soon as the object is constructed from an export.
    /// </summary>
    Preload = 1 << 0,

    /// <summary>
    /// The object tagged properties should be linked <see cref="UDefaultProperty.Property"/> to the equivalent class <see cref="UProperty"/>.
    /// </summary>
    LinkTaggedProperties = 1 << 1,

    /// <summary>
    /// The object tagged properties should be linked <see cref="UDefaultProperty._InternalValuePtr"/> to the equivalent attributed property using <see cref="UnrealPropertyAttribute"/>.
    ///
    /// TODO: NOT IMPLEMENTED; Merge code from new property binding branch.
    /// </summary>
    LinkAttributedProperties = 1 << 2,

    /// <summary>
    /// The object tagged properties should also preload their value; otherwise a call to <see cref="UDefaultProperty.DeserializeProperty "/> must be invoked manually.
    /// </summary>
    PreloadTaggedProperties = 1 << 3,

    /// <summary>
    /// An intrinsic class, objects of this class are crucial to the inner-workings of objects, and must be preloaded.
    /// </summary>
    Intrinsic = Preload,

    Inherit = LinkAttributedProperties | LinkTaggedProperties | PreloadTaggedProperties,
}

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
            InternalFlags = InternalClassFlags,
            InternalType = internalClassType,
        };

        return staticClass;
    }
}
