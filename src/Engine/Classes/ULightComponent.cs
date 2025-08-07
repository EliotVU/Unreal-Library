using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements ULightComponent/Engine.LightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULightComponent : UActorComponent
    {
        #region Serialized Members

        [StreamRecord]
        public UArray<UConvexVolume> InclusionConvexVolumes { get; set; }

        [StreamRecord]
        public UArray<UConvexVolume> ExclusionConvexVolumes { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedConvexVolumes &&
                stream.Version < (uint)PackageObjectLegacyVersion.RemovedConvexVolumes)
            {
                InclusionConvexVolumes = stream.ReadArray<UConvexVolume>();
                stream.Record(nameof(InclusionConvexVolumes), InclusionConvexVolumes);

                ExclusionConvexVolumes = stream.ReadArray<UConvexVolume>();
                stream.Record(nameof(ExclusionConvexVolumes), ExclusionConvexVolumes);
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedConvexVolumes &&
                stream.Version < (uint)PackageObjectLegacyVersion.RemovedConvexVolumes)
            {
                stream.WriteArray(InclusionConvexVolumes);
                stream.WriteArray(ExclusionConvexVolumes);
            }
        }
    }

    /// <summary>
    ///     Implements UDirectionalLightComponent/Engine.DirectionalLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDirectionalLightComponent : ULightComponent;

    /// <summary>
    ///     Implements UDominantDirectionalLightComponent/Engine.DominantDirectionalLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDominantDirectionalLightComponent : UDirectionalLightComponent
    {
        #region Serialized Members

        [StreamRecord]
        public UArray<ushort> DominantLightShadowMap { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDominantLightShadowMapToDominantDirectionalLightComponent
#if GOWUE
                || stream.Package.Build == UnrealPackage.GameBuild.BuildName.GoWUE
#endif
                )
            {
                DominantLightShadowMap = stream.ReadUShortArray();
                stream.Record(nameof(DominantLightShadowMap), DominantLightShadowMap);
            }

            base.Deserialize(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDominantLightShadowMapToDominantDirectionalLightComponent
#if GOWUE
                || stream.Package.Build == UnrealPackage.GameBuild.BuildName.GoWUE
#endif
                )
            {
                stream.Write(DominantLightShadowMap);
            }

            base.Serialize(stream);
        }
    }

    /// <summary>
    ///     Implements UDominantPointLightComponent/Engine.DominantPointLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDominantPointLightComponent : UPointLightComponent;

    /// <summary>
    ///     Implements UDominantSpotLightComponent/Engine.DominantSpotLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDominantSpotLightComponent : UPointLightComponent
    {
        #region Serialized Members

        [StreamRecord]
        public UArray<ushort> DominantLightShadowMap { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDominantLightShadowMapToUDominantSpotLightComponent)
            {
                DominantLightShadowMap = stream.ReadUShortArray();
                stream.Record(nameof(DominantLightShadowMap), DominantLightShadowMap);
            }

            base.Deserialize(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDominantLightShadowMapToUDominantSpotLightComponent)
            {
                stream.Record(nameof(DominantLightShadowMap), DominantLightShadowMap);
                stream.Write(DominantLightShadowMap);
            }

            base.Serialize(stream);
        }
    }

    /// <summary>
    ///     Implements UPointLightComponent/Engine.PointLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UPointLightComponent : ULightComponent;

    /// <summary>
    ///     Implements USpotLightComponent/Engine.SpotLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USpotLightComponent : UPointLightComponent;

    /// <summary>
    ///     Implements USkyLightComponent/Engine.SkyLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USkyLightComponent : ULightComponent;

    /// <summary>
    ///     Implements USphericalHarmonicLightComponent/Engine.SphericalHarmonicLightComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USphericalHarmonicLightComponent : ULightComponent;
}
