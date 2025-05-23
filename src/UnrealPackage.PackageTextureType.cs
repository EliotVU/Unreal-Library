using UELib.Core;

namespace UELib;

public sealed partial class UnrealPackage
{
    public struct PackageTextureType : IUnrealSerializableClass
    {
        public int Width;
        public int Height;
        public int MipCount;
        public uint Format;
        public uint CreateFlags;
        public UArray<int> ExportIndices;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out Width);
            stream.Read(out Height);
            stream.Read(out MipCount);
            stream.Read(out Format);
            stream.Read(out CreateFlags);
            stream.ReadArray(out ExportIndices);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(Width);
            stream.Write(Height);
            stream.Write(MipCount);
            stream.Write(Format);
            stream.Write(CreateFlags);
            stream.WriteArray(ExportIndices);
        }
    }
}