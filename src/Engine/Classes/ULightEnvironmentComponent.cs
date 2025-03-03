using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements ULightEnvironmentComponent/Engine.LightEnvironmentComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULightEnvironmentComponent : UActorComponent
    {
    }

    /// <summary>
    ///     Implements UDynamicLightEnvironmentComponent/Engine.DynamicLightEnvironmentComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDynamicLightEnvironmentComponent : ULightEnvironmentComponent
    {
    }

    /// <summary>
    ///     Implements UParticleLightEnvironmentComponent/Engine.ParticleLightEnvironmentComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UParticleLightEnvironmentComponent : UDynamicLightEnvironmentComponent
    {
    }
}
