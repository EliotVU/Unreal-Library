using System;
using UELib;
using UELib.Annotations;
using UELib.ObjectModel;
using UELib.ObjectModel.Annotations;

namespace UELib;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public sealed class UnrealRegisterClassAttribute : UnrealClassAttribute;
