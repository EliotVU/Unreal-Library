using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UFogVolumeDensityComponent/Engine.FogVolumeDensityComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFogVolumeDensityComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements UFogVolumeConeDensityComponent/Engine.FogVolumeConeDensityComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFogVolumeConeDensityComponent : UFogVolumeDensityComponent
    {
    }

    /// <summary>
    ///     Implements UFogVolumeConstantDensityComponent/Engine.FogVolumeConstantDensityComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFogVolumeConstantDensityComponent : UFogVolumeDensityComponent
    {
    }

    /// <summary>
    ///     Implements UFogVolumeLinearHalfspaceDensityComponent/Engine.FogVolumeLinearHalfspaceDensityComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFogVolumeLinearHalfspaceDensityComponent : UFogVolumeDensityComponent
    {
    }

    /// <summary>
    ///     Implements UFogVolumeSphericalDensityComponent/Engine.FogVolumeSphericalDensityComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFogVolumeSphericalDensityComponent : UFogVolumeDensityComponent
    {
    }
}
