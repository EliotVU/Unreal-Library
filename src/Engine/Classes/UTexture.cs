using UELib.Annotations;
using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture/Engine.Texture
    /// </summary>
    [UnrealRegisterClass]
    public class UTexture : UBitmapMaterial
    {
        public bool HasComp;

        [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE2_5)] [CanBeNull]
        public UArray<LegacyMipMap> Mips;

        protected override void Deserialize()
        {
            base.Deserialize();

            if (_Buffer.Version > 160)
            {
                throw new NotSupportedException("UTexture is not supported for this build");
            }

            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.CompMipsDeprecated)
            {
                var bHasCompProperty = Properties.Find("bHasComp");
                if (bHasCompProperty != null)
                {
                    HasComp = bool.Parse(bHasCompProperty.Value);
                    if (HasComp)
                    {
                        _Buffer.ReadArray(out UArray<LegacyMipMap> oldMips);
                        Record(nameof(oldMips), oldMips);
                    }
                }
            }

            _Buffer.ReadArray(out Mips);
            Record(nameof(Mips), Mips);
        }

        public struct LegacyMipMap : IUnrealSerializableClass
        {
            public UBulkData<byte> Data;
            public int USize;
            public int VSize;
            public byte UBits;
            public byte VBits;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out Data);
                stream.Read(out USize);
                stream.Read(out VSize);
                stream.Read(out UBits);
                stream.Read(out VBits);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(ref Data);
                stream.Write(USize);
                stream.Write(VSize);
                stream.Write(UBits);
                stream.Write(VBits);
            }
        }
    }
}
