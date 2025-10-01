using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture3D/Engine.Texture3D
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTexture3D : UTexture
    {
        #region Serialized Members

        [StreamRecord]
        public uint SizeX { get; set; }

        [StreamRecord]
        public uint SizeY { get; set; }

        [StreamRecord]
        public UArray<MipMap3D> Mips { get; set; } = [];

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if BATTLEBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn &&
                stream.LicenseeVersion >= 47)
            {
                DeserializeTextureBaseGbx(stream);

                return;
            }
#endif
            SizeX = stream.ReadUInt32();
            stream.Record(nameof(SizeX), SizeX);

            SizeY = stream.ReadUInt32();
            stream.Record(nameof(SizeY), SizeY);

            stream.Read(out int format);
            Format = (TextureFormat)format;
            stream.Record(nameof(Format), Format);

            Mips = stream.ReadArray<MipMap3D>();
            stream.Record(nameof(Mips), Mips);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if BATTLEBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn &&
                stream.LicenseeVersion >= 47)
            {
                SerializeTextureBaseGbx(stream);

                return;
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.DisplacedUTextureProperties)
            {
                stream.Write(SizeX);
                stream.Write(SizeY);

                stream.Write((byte)Format);

                stream.Write(Mips);
            }
        }

        public struct MipMap3D : IUnrealSerializableClass
        {
            public UBulkData<byte> Data;
            public uint SizeX;
            public uint SizeY;
            public uint SizeZ;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out Data);
                stream.Read(out SizeX);
                stream.Read(out SizeY);
                stream.Read(out SizeZ);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(ref Data);
                stream.Write(SizeX);
                stream.Write(SizeY);
                stream.Write(SizeZ);
            }
        }
    }
}
