using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UActorComponent/Engine.ActorComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UActorComponent : UComponent
    {
    }

    /// <summary>
    ///     Implements UExponentialHeightFogComponent/Engine.ExponentialHeightFogComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UExponentialHeightFogComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements UHeadTrackingComponent/Engine.HeadTrackingComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UHeadTrackingComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements UHeightFogComponent/Engine.HeightFogComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UHeightFogComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements UImageReflectionComponent/Engine.ImageReflectionComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UImageReflectionComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements URadialBlurComponent/Engine.RadialBlurComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class URadialBlurComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements UWindDirectionalSourceComponent/Engine.WindDirectionalSourceComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UWindDirectionalSourceComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements UWindPointSourceComponent/Engine.WindPointSourceComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UWindPointSourceComponent : UWindDirectionalSourceComponent
    {
    }
}
