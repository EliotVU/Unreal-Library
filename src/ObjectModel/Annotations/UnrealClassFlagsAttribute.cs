using System;
using UELib.Core;
using UELib.Flags;

namespace UELib.ObjectModel.Annotations;

/// <summary>
/// Specifies class flags for internal use to determine how a class and its objects should be handled.
/// The attributed flags are inherited by derived classes unless overridden.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnrealClassFlagsAttribute : Attribute
{
    public readonly InternalClassFlags InternalClassFlags;
    public readonly ClassFlag[] ClassFlags;

    public UnrealClassFlagsAttribute(
        InternalClassFlags internalClassFlags = InternalClassFlags.Default)
    {
        InternalClassFlags = internalClassFlags;
        ClassFlags = [];
    }

    public UnrealClassFlagsAttribute(
        InternalClassFlags internalClassFlags = InternalClassFlags.Default,
        params ClassFlag[] classFlags)
    {
        InternalClassFlags = internalClassFlags;
        ClassFlags = classFlags;
    }
}

[Flags]
public enum InternalClassFlags
{
    Default = LazyLoad | Intrinsic,

    /// <summary>
    /// The object should not be preloaded, instead a manual call to <see cref="UObject.Load"/> is required.
    /// </summary>
    LazyLoad = 0,

    /// <summary>
    /// The object should be preloaded using an ordinary call to <see cref="UObject.Load"/> as soon as the object is constructed from an export.
    /// </summary>
    Preload = 1 << 0,

    /// <summary>
    /// The object tagged properties should be linked to the corresponding struct/class properties i.e. a <see cref="UProperty"/>.
    /// </summary>
    LinkTaggedProperties = 1 << 1,

    /// <summary>
    /// The object tagged properties should be linked to the corresponding attributed properties with a <see cref="UnrealPropertyAttribute"/>.
    ///
    /// FIXME: NOT IMPLEMENTED IN THIS BRANCH YET.
    /// </summary>
    LinkAttributedProperties = 1 << 2,

    /// <summary>
    /// The object tagged properties should also preload their value; otherwise a call to <see cref="UDefaultProperty.DeserializeProperty "/> must be invoked manually.
    /// </summary>
    PreloadTaggedProperties = 1 << 3,

    /// <summary>
    /// An intrinsic class, objects of this class are crucial to the inner-workings of objects, and must be preloaded.
    /// </summary>
    Intrinsic = 1 << 4,
    
    Preloadable = Intrinsic | Preload,
    Inherit = Preload | LinkAttributedProperties | LinkTaggedProperties | PreloadTaggedProperties,
}
