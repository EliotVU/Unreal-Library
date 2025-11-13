using System.IO;

namespace UELib;

public class UnrealPackageStream(UnrealPackageArchive baseArchive, Stream baseStream)
    : UnrealPackagePipedStream(baseArchive, baseStream)
{
    internal void SwapReaderBaseStream(Stream newBaseStream)
    {
        if (CanRead)
        {
            Reader = new UnrealPackageReader(BaseArchive, CreateBinaryReader(newBaseStream));
        }
    }

    internal void SwapWriterBaseStream(Stream newBaseStream)
    {
        if (CanWrite)
        {
            Writer = new UnrealPackageWriter(BaseArchive, CreateBinaryWriter(newBaseStream));
        }
    }
}
