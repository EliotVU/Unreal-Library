using System.Diagnostics.CodeAnalysis;
using UELib.Annotations;

namespace UELib
{
    using Core;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [PublicAPI]
    public class CompressedChunk : IUnrealSerializableClass
    {
        private long _UncompressedOffset;
        private int _UncompressedSize;
        private long _CompressedOffset;
        private int _CompressedSize;

        public long UncompressedOffset
        {
            get => _UncompressedOffset;
            set => _UncompressedOffset = value;
        }

        public int UncompressedSize
        {
            get => _UncompressedSize;
            set => _UncompressedSize = value;
        }

        public long CompressedOffset
        {
            get => _CompressedOffset;
            set => _CompressedOffset = value;
        }

        public int CompressedSize
        {
            get => _CompressedSize;
            set => _CompressedSize = value;
        }

        public void Serialize(IUnrealStream stream)
        {
#if ROCKETLEAGUE

            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague
                && stream.LicenseeVersion >= 22)
            {
                stream.Write(_UncompressedOffset);
                stream.Write(_CompressedOffset);
                goto streamStandardSize;
            }
#endif
            stream.Write((int)_UncompressedOffset);
            stream.Write(_UncompressedSize);
        streamStandardSize:
            stream.Write((int)_CompressedOffset);
            stream.Write(_CompressedSize);
        }

        public void Deserialize(IUnrealStream stream)
        {
#if ROCKETLEAGUE
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague
                && stream.LicenseeVersion >= 22)
            {
                _UncompressedOffset = stream.ReadInt64();
                _CompressedOffset = stream.ReadInt64();
                goto streamStandardSize;
            }
#endif
            _UncompressedOffset = stream.ReadInt32();
            _CompressedOffset = stream.ReadInt32();
        streamStandardSize:
            _UncompressedSize = stream.ReadInt32();
            _CompressedSize = stream.ReadInt32();
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