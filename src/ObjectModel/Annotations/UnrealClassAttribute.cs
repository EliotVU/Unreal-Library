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
    /// The object should not be pre-loaded, instead a manual call to <see cref="UObject.Load"/> is required.
    /// </summary>
    LazyLoad = 0,

    /// <summary>
    /// The object should be pre-loaded using an ordinary call to <see cref="UObject.Load"/> as soon as the object is constructed from an export.
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
    /// An intrinsic class, objects of this class are crucial to the inner-workings of objects, and must be pre-loaded.
    /// </summary>
    Intrinsic = Preload,
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
    public readonly UName SuperName;
    public readonly UName PackageName;
    public readonly InternalClassFlags InternalClassFlags;
    public readonly ClassFlag[] ClassFlags;

    // FIXME: Convenient, but this data is not preserved when retrying an attribute by using the GetCustomAttribute method.
    private UClass _StaticClass;

    public UnrealClassAttribute()
    {
        InternalClassFlags = InternalClassFlags.Default;
        ClassFlags = [ClassFlag.Intrinsic];
    }

    public UnrealClassAttribute(
        string className,
        string packageName,
        InternalClassFlags internalClassFlags = InternalClassFlags.Default,
        params ClassFlag[] classFlags)
    {
        Debug.Assert(!string.IsNullOrEmpty(className));
        Debug.Assert(!string.IsNullOrEmpty(packageName));

        ClassName = new UName(className);
        SuperName = UnrealName.None;
        PackageName = new UName(packageName);
        InternalClassFlags = internalClassFlags;
        ClassFlags = [ClassFlag.Intrinsic, .. classFlags];
    }

    public UnrealClassAttribute(
        string className,
        string superName,
        string packageName,
        InternalClassFlags internalClassFlags = InternalClassFlags.Default,
        params ClassFlag[] classFlags)
    {
        Debug.Assert(!string.IsNullOrEmpty(className));
        Debug.Assert(!string.IsNullOrEmpty(superName));
        Debug.Assert(!string.IsNullOrEmpty(packageName));

        ClassName = new UName(className);
        SuperName = new UName(superName);
        PackageName = new UName(packageName);
        InternalClassFlags = internalClassFlags;
        ClassFlags = [ClassFlag.Intrinsic, .. classFlags];
    }

    public UClass GetStaticClass()
    {
        Debug.Assert(_StaticClass != null);
        return _StaticClass;
    }

    public UClass CreateStaticClass(Type internalClassType, UName className)
    {
        Debug.Assert(_StaticClass == null);
        var staticClass = new UClass
        {
            _InternalType = internalClassType,
            Name = className,
            InternalFlags = InternalClassFlags,
        };

        _StaticClass = staticClass;
        return staticClass;
    }
}
