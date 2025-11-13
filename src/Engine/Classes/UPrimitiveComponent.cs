using System;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UPrimitiveComponent/Engine.PrimitiveComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UPrimitiveComponent : UActorComponent
    {
        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if R6
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6Vegas &&
                stream.LicenseeVersion >= 50)
            {
                stream.Read(out int v1B4);
                for (int i = 0; i < v1B4; i++)
                {
                    stream.ReadStruct(out UGuid v0); // LightGuid?
                    stream.ReadArray(out UArray<ushort> v24); // LightShadowMap?
                    stream.Read(out uint v20);
                }

                stream.Record(nameof(v1B4), v1B4);
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedComponentGuid &&
                stream.Version < (uint)PackageObjectLegacyVersion.ComponentGuidDeprecated)
            {
                stream.ReadStruct(out UGuid guid);
                stream.Record(nameof(guid), guid);
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if R6
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6Vegas &&
                stream.LicenseeVersion >= 50)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedComponentGuid &&
                stream.Version < (uint)PackageObjectLegacyVersion.ComponentGuidDeprecated)
            {
                var guid = (UGuid)Guid.NewGuid();
                stream.WriteStruct(ref guid);
            }
        }
    }

    /// <summary>
    ///     Implements USpriteComponent/Engine.SpriteComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USpriteComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UCylinderComponent/Engine.CylinderComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UCylinderComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UArrowComponent/Engine.ArrowComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UArrowComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UDrawSphereComponent/Engine.DrawSphereComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDrawSphereComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UDrawCylinderComponent/Engine.DrawCylinderComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDrawCylinderComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UDrawBoxComponent/Engine.DrawBoxComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDrawBoxComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UDrawCapsuleComponent/Engine.DrawCapsuleComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDrawCapsuleComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UDrawFrustumComponent/Engine.DrawFrustumComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDrawFrustumComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UDrawQuadComponent/Engine.DrawQuadComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDrawQuadComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UDrawSoundRadiusComponent/Engine.DrawSoundRadiusComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UDrawSoundRadiusComponent : UDrawSphereComponent;

    /// <summary>
    ///     Implements ULightRadiusComponent/Engine.LightRadiusComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULightRadiusComponent : UDrawSphereComponent;

    /// <summary>
    ///     Implements UCameraConeComponent/Engine.CameraConeComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UCameraConeComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UCollisionComponent/Engine.CollisionComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UCollisionComponent : UCylinderComponent;

    /// <summary>
    ///     Implements UModelComponent/Engine.LineBatchComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULineBatchComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UParticleSystemComponent/Engine.ParticleSystemComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UParticleSystemComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UCascadeParticleSystemComponent/Engine.CascadeParticleSystemComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UCascadeParticleSystemComponent : UParticleSystemComponent;

    /// <summary>
    ///     Implements UFluidInfluenceComponent/Engine.FluidInfluenceComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFluidInfluenceComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UFluidSurfaceComponent/Engine.FluidSurfaceComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UFluidSurfaceComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements UImageReflectionShadowPlaneComponent/Engine.ImageReflectionShadowPlaneComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UImageReflectionShadowPlaneComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements ULandscapeComponent/Engine.LandscapeComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULandscapeComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements ULandscapeHeightfieldCollisionComponent/Engine.LandscapeHeightfieldCollisionComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULandscapeHeightfieldCollisionComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements ULensFlareComponent/Engine.LensFlareComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class ULensFlareComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements USpeedTreeComponent/Engine.SpeedTreeComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USpeedTreeComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements USplineComponent/Engine.SplineComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USplineComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements USplineComponentSimplified/Engine.SplineComponentSimplified
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USplineComponentSimplified : USplineComponent;

    /// <summary>
    ///     Implements UTerrainComponent/Engine.TerrainComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTerrainComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements URB_ConstraintDrawComponent/Engine.RB_ConstraintDrawComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class URB_ConstraintDrawComponent : UPrimitiveComponent;

    /// <summary>
    ///     Implements URB_RadialImpulseComponent/Engine.RB_RadialImpulseComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class URB_RadialImpulseComponent : UPrimitiveComponent;
}
