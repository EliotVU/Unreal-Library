using System.Diagnostics.CodeAnalysis;
using UELib.Annotations;
using UELib.Core.Types;

namespace UELib
{
    using Core;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [PublicAPI]
    public class CompressedChunk : IUnrealSerializableClass
    {
        public int UncompressedOffset;
        public int UncompressedSize;
        public int CompressedOffset;
        public int CompressedSize;

        public void Serialize(IUnrealStream stream)
        {
#if ROCKETLEAGUE

            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague
                && stream.Package.LicenseeVersion >= 22)
            {
                stream.Write((long)UncompressedOffset);
                stream.Write((long)CompressedOffset);
                goto streamStandardSize;
            }
#endif
            stream.Write(UncompressedOffset);
            stream.Write(UncompressedSize);
        streamStandardSize:
            stream.Write(CompressedOffset);
            stream.Write(CompressedSize);
        }

        public void Deserialize(IUnrealStream stream)
        {
#if ROCKETLEAGUE
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague
                && stream.Package.LicenseeVersion >= 22)
            {
                UncompressedOffset = (int)stream.ReadInt64();
                CompressedOffset = (int)stream.ReadInt64();
                goto streamStandardSize;
            }
#endif
            UncompressedOffset = stream.ReadInt32();
            CompressedOffset = stream.ReadInt32();
        streamStandardSize:
            UncompressedSize = stream.ReadInt32();
            CompressedSize = stream.ReadInt32();
        }
    }

 	// TODO: Complete implementation
    // ReSharper disable once UnusedType.Global
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct CompressedChunkHeader : IUnrealSerializableClass
    {
        public uint Tag;
        public int ChunkSize;
        public CompressedChunkBlock Summary;
        public UArray<CompressedChunkBlock> Chunks;

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(Tag);
            stream.Write(ChunkSize);
            Summary.Serialize(stream);
            stream.Write(ref Chunks);
        }

        public void Deserialize(IUnrealStream stream)
        {
            Tag = stream.ReadUInt32();
            ChunkSize = stream.ReadInt32();
            if ((uint)ChunkSize == UnrealPackage.Signature)
            {
                ChunkSize = 0x20000;
            }
            Summary = new CompressedChunkBlock();
            Summary.Deserialize(stream);
            
            int chunksCount = (Summary.UncompressedSize + ChunkSize - 1) / ChunkSize;
            stream.ReadArray(out Chunks, chunksCount);
        }
    }

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public struct CompressedChunkBlock : IUnrealSerializableClass
    {
        public int CompressedSize;
        public int UncompressedSize;

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(CompressedSize);
            stream.Write(UncompressedSize);
        }

        public void Deserialize(IUnrealStream stream)
        {
            CompressedSize = stream.ReadInt32();
            UncompressedSize = stream.ReadInt32();
        }
    }
}