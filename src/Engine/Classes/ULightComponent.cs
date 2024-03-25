using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements ULightComponent/Engine.LightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULightComponent : UActorComponent
    {
        // TODO: InclusionConvexVolumes, ExclusionConvexVolumes
        protected override void Deserialize() => base.Deserialize();
    }
}
