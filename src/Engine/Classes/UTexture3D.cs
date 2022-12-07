using UELib.Branch;
using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UTexture3D/Engine.Texture3D
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UTexture3D : UTexture
    {
        public UArray<MipMap3D> Mips;
        public uint SizeX, SizeY;

        protected override void Deserialize()
        {
            base.Deserialize();

            _Buffer.Read(out SizeX);
            Record(nameof(SizeX), SizeX);
            
            _Buffer.Read(out SizeY);
            Record(nameof(SizeY), SizeY);
            
            _Buffer.Read(out int format);
            Format = (TextureFormat)format;
            Record(nameof(Format), Format);

            _Buffer.ReadArray(out Mips);
        }

        public struct MipMap3D : IUnrealDeserializableClass
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
