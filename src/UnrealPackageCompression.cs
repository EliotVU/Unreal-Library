using System;
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
        public CompressedChunkHeader Header;

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

        public void Decompress(UPackageStream inStream, UPackageStream outStream)
        {
            inStream.Seek(CompressedOffset, System.IO.SeekOrigin.Begin);
            Header.Deserialize(inStream);

            outStream.Seek(UncompressedOffset, System.IO.SeekOrigin.Begin);
            foreach (byte[] buffer in Header.Blocks.Select(block => block.Decompress()))
            {
                outStream.Write(buffer, 0, buffer.Length);
            }
        }

        public struct CompressedChunkHeader : IUnrealSerializableClass
        {
            public uint Signature;
            public int BlockSize;
            public int CompressedSize;
            public int UncompressedSize;

            public UArray<CompressedChunkBlock> Blocks;

            public void Serialize(IUnrealStream stream)
            {
                // TODO: Implement code
            }

            public void Deserialize(IUnrealStream stream)
            {
                Signature = stream.ReadUInt32();
                if (Signature != UnrealPackage.Signature)
                {
                    throw new System.IO.FileLoadException("Unrecognized signature!");
                }

                BlockSize = stream.ReadInt32();
                CompressedSize = stream.ReadInt32();
                UncompressedSize = stream.ReadInt32();

                var blockCount = (int)Math.Ceiling(UncompressedSize / (float)BlockSize);
                stream.ReadArray(out Blocks, blockCount);
            }

            public struct CompressedChunkBlock : IUnrealSerializableClass
            {
                public int CompressedSize;
                public int UncompressedSize;
                public byte[] CompressedData;

                public void Serialize(IUnrealStream stream)
                {
                    // TODO: Implement code
                }

                public void Deserialize(IUnrealStream stream)
                {
                    CompressedSize = stream.ReadInt32();
                    UncompressedSize = stream.ReadInt32();

                    CompressedData = new byte[CompressedSize];
                    stream.Read(CompressedData, 0, CompressedSize);
                }

                public byte[] Decompress()
                {
                    var decompressedData = new byte[UncompressedSize];
                    //ManagedLZO.MiniLZO.Decompress( CompressedData, DecompressedData );
                    return decompressedData;
                }
            }
        }

        public bool IsChunked(long offset)
        {
            return offset >= UncompressedOffset && offset < UncompressedOffset + UncompressedSize;
        }
    }
}