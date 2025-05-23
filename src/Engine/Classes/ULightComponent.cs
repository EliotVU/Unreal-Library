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

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedConvexVolumes &&
                _Buffer.Version < (uint)PackageObjectLegacyVersion.RemovedConvexVolumes)
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
        public UArray<ushort> DominantLightShadowMap;

        public override void Deserialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDominantLightShadowMapToDominantDirectionalLightComponent)
            {
                stream.Read(out DominantLightShadowMap);
                Record(nameof(DominantLightShadowMap), DominantLightShadowMap);
            }

            base.Deserialize(stream);
        }
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
        public UArray<ushort> DominantLightShadowMap;

        public override void Deserialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDominantLightShadowMapToUDominantSpotLightComponent)
            {
                stream.Read(out DominantLightShadowMap);
                Record(nameof(DominantLightShadowMap), DominantLightShadowMap);
            }

            base.Deserialize(stream);
        }
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
