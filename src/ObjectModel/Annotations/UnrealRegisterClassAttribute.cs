using System;
using UELib.Annotations;
using UELib.Flags;

namespace UELib.ObjectModel.Annotations;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnrealRegisterClassAttribute : UnrealClassAttribute
{
    public UnrealRegisterClassAttribute()
    { }
    
    public UnrealRegisterClassAttribute(
        InternalClassFlags internalClassFlags = InternalClassFlags.Default,
        params ClassFlag[] classFlags) : base(internalClassFlags, classFlags)
    { }
}
