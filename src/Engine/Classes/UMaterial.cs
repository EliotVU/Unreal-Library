using UELib.Branch;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UMaterial/Engine.Material
    /// </summary>
    [UnrealRegisterClass]
    public class UMaterial : UMaterialInterface
    {
        #region Serialized Members

        [StreamRecord, UnrealProperty]
        public byte MaterialType { get; set; }

        /// <summary>
        /// Implements `var const native duplicatetransient pointer MaterialResources[2]{FMaterialResource};`
        /// </summary>
        [StreamRecord, BuildGeneration(BuildGeneration.UE3)]
        public MaterialResource?[] MaterialResources { get; } = new MaterialResource?[2];

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if UNREAL2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                MaterialType = stream.ReadByte();
                stream.Record(nameof(MaterialType), MaterialType); // TextureType in Unreal2
            }
#endif
#if RM
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RM &&
                stream.LicenseeVersion >= 3)
            {
                MaterialType = stream.ReadByte(); // v34
                stream.Record(nameof(MaterialType), MaterialType);
            }
#endif
            // UE3's UTexture no longer inherits from UMaterial.
            if (this is not UTexture && stream.Version >= (uint)PackageObjectLegacyVersion.UE3)
            {
                uint materialMask = 0x01;
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedQualityMaskToUMaterial)
                {
                    materialMask = stream.ReadUInt32();
                    stream.Record(nameof(materialMask), materialMask);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties
                    && (this is not UDecalMaterial || stream.Version >= (uint)PackageObjectLegacyVersion.UpdatedDecalMaterial)
                    && !IsTemplate())
                {
                    for (int i = 0; i < MaterialResources.Length; i++)
                    {
                        if ((materialMask & (1 << i)) == 0)
                        {
                            continue;
                        }

                        MaterialResources[i] ??= new MaterialResource();
                        //MaterialResources[i].Deserialize(stream);
                    }
                }
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if UNREAL2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                stream.Write(MaterialType);
            }
#endif
#if RM
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RM &&
                stream.LicenseeVersion >= 3)
            {
                stream.Write(MaterialType);
            }
#endif
            // UE3's UTexture no longer inherits from UMaterial.
            if (this is not UTexture && stream.Version >= (uint)PackageObjectLegacyVersion.UE3)
            {
                uint materialMask = 0x01;
                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedQualityMaskToUMaterial)
                {
                    for (int i = 0; i < MaterialResources.Length; i++)
                    {
                        if (MaterialResources[i] != null)
                        {
                            materialMask |= 1u << i;
                        }
                    }

                    stream.Write(materialMask);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties
                    && (this is not UDecalMaterial || stream.Version >= (uint)PackageObjectLegacyVersion.UpdatedDecalMaterial)
                    && !IsTemplate())
                {
                    for (int i = 0; i < MaterialResources.Length; i++)
                    {
                        if ((materialMask & (1u << i)) == 0)
                        {
                            continue;
                        }

                        if (MaterialResources[i] != null)
                        {
                            //MaterialResources[i].Serialize(stream);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Implements FMaterial
        /// </summary>
        public class Material : IUnrealSerializableClass
        {
            public void Deserialize(IUnrealStream stream)
            {
                throw new System.NotImplementedException();
            }

            public void Serialize(IUnrealStream stream)
            {
                throw new System.NotImplementedException();
            }
        }

        /// <summary>
        ///     Implements FMaterialResource
        /// </summary>
        public class MaterialResource : Material
        {
            public void Deserialize(IUnrealStream stream)
            {
                throw new System.NotImplementedException();
            }

            public void Serialize(IUnrealStream stream)
            {
                throw new System.NotImplementedException();
            }
        }
    }

    [UnrealRegisterClass, BuildGeneration(BuildGeneration.UE3)]
    public class UDecalMaterial : UMaterial;
}
