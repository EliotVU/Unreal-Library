using System.IO;

namespace UELib;

public class UnrealPackageStream(UnrealPackageArchive baseArchive, Stream baseStream)
    : UnrealPackagePipedStream(baseArchive, baseStream)
{
    internal void SwapReaderBaseStream(Stream newBaseStream)
    {
        if (CanRead)
        {
            Reader = new UnrealReader(BaseArchive, CreateBinaryReader(newBaseStream));
        }
    }

    internal void SwapWriterBaseStream(Stream newBaseStream)
    {
        if (CanWrite)
        {
            Writer = new UnrealWriter(BaseArchive, CreateBinaryWriter(newBaseStream));
        }
    }
}