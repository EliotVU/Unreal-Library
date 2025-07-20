using System.Diagnostics.CodeAnalysis;
using UELib.Flags;

namespace UELib
{
    using Core;

    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
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

            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague
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
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague
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
            stream.Write(ref Summary);
            stream.Write(Chunks);
        }

        public void Deserialize(IUnrealStream stream)
        {
            Tag = stream.ReadUInt32();
            ChunkSize = stream.ReadInt32();
            if ((uint)ChunkSize == UnrealPackage.Signature)
            {
                ChunkSize = 0x20000;
            }
            stream.ReadStruct(out Summary);

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

        public int Decompress(byte[] compressedData, int index, CompressionFlags flags)
        {
            return UncompressedSize;
        }
    }
}
