using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements ULightComponent/Engine.LightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULightComponent : UActorComponent
    {
        public UArray<UConvexVolume> InclusionConvexVolumes;
        public UArray<UConvexVolume> ExclusionConvexVolumes;

        protected override void Deserialize()
        {
            base.Deserialize();

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedConvexVolumes)
            {
                _Buffer.ReadArray(out InclusionConvexVolumes);
                Record(nameof(InclusionConvexVolumes), InclusionConvexVolumes);

                _Buffer.ReadArray(out ExclusionConvexVolumes);
                Record(nameof(ExclusionConvexVolumes), ExclusionConvexVolumes);
            }
        }
    }

    /// <summary>
    ///     Implements UDirectionalLightComponent/Engine.DirectionalLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDirectionalLightComponent : ULightComponent
    {
    }

    /// <summary>
    ///     Implements UDominantDirectionalLightComponent/Engine.DominantDirectionalLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDominantDirectionalLightComponent : UDirectionalLightComponent
    {
    }

    /// <summary>
    ///     Implements UDominantPointLightComponent/Engine.DominantPointLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDominantPointLightComponent : UPointLightComponent
    {
    }

    /// <summary>
    ///     Implements UDominantSpotLightComponent/Engine.DominantSpotLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDominantSpotLightComponent : UPointLightComponent
    {
    }

    /// <summary>
    ///     Implements UPointLightComponent/Engine.PointLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UPointLightComponent : ULightComponent
    {
    }

    /// <summary>
    ///     Implements USpotLightComponent/Engine.SpotLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USpotLightComponent : UPointLightComponent
    {
    }

    /// <summary>
    ///     Implements USkyLightComponent/Engine.SkyLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USkyLightComponent : ULightComponent
    {
    }

    /// <summary>
    ///     Implements USphericalHarmonicLightComponent/Engine.SphericalHarmonicLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USphericalHarmonicLightComponent : ULightComponent
    {
    }
}
