using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using UELib.Branch;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FUntypedBulkData (UE3) and TLazyArray (UE3 or older)
    /// </summary>
    /// <typeparam name="TElement">The primitive element type of the bulk data.</typeparam>
    public struct UBulkData<TElement> : IUnrealSerializableClass, IDisposable
        where TElement : unmanaged
    {
        public BulkDataFlags Flags;

        public long StorageSize;
        public long StorageOffset;

        public int ElementCount;
        [CanBeNull] public byte[] ElementData;

        // TODO: multi-byte based data.
        public UBulkData(BulkDataFlags flags, byte[] rawData)
        {
            Flags = flags;
            StorageSize = -1;
            StorageOffset = -1;

            ElementCount = rawData.Length;
            ElementData = rawData;
        }

        public void Deserialize(IUnrealStream stream)
        {
            if (stream.Version < (uint)PackageObjectLegacyVersion.LazyArrayReplacedWithBulkData)
            {
                DeserializeLegacyLazyArray(stream);
                return;
            }

            stream.Read(out uint flags);
            Flags = (BulkDataFlags)flags;

            stream.Read(out ElementCount);

            StorageSize = stream.ReadInt32();
            StorageOffset = stream.ReadInt32();

            if (Flags.HasFlag(BulkDataFlags.StoreInSeparateFile))
            {
                return;
            }

            Debug.Assert(stream.AbsolutePosition == StorageOffset);
            // Skip the ElementData
            stream.AbsolutePosition = StorageOffset + StorageSize;
        }

        // Deserializes the TLazyArray format
        private void DeserializeLegacyLazyArray(IUnrealStream stream)
        {
            int elementSize = Unsafe.SizeOf<TElement>();

            if (stream.Version < (uint)PackageObjectLegacyVersion.LazyArraySkipCountChangedToSkipOffset)
            {
                ElementCount = stream.ReadIndex();

                StorageSize = ElementCount * elementSize;
                StorageOffset = stream.AbsolutePosition;

                stream.AbsolutePosition += StorageSize;
                return;
            }

            // Absolute position in stream
            long skipOffset = stream.ReadInt32();

            // We still need these checks for Rainbow Six: Vegas 2 (V241)
            if (stream.Version >= (uint)PackageObjectLegacyVersion.StorageSizeAddedToLazyArray)
            {
                StorageSize = stream.ReadInt32();
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LazyLoaderFlagsAddedToLazyArray)
            {
                stream.Read(out uint flags);
                Flags = (BulkDataFlags)flags;
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.L8AddedToLazyArray)
            {
                stream.Read(out UName l8);
            }

            ElementCount = stream.ReadLength();

            StorageSize = skipOffset - stream.AbsolutePosition;
            StorageOffset = stream.AbsolutePosition;

            stream.AbsolutePosition = skipOffset;
        }

        public void LoadData(IUnrealStream stream)
        {
            if (Flags.HasFlag(BulkDataFlags.Unused) || ElementCount == 0)
            {
                return;
            }

            int elementSize = Unsafe.SizeOf<TElement>();
            ElementData = new byte[ElementCount * elementSize];

            if (Flags.HasFlag(BulkDataFlags.StoreInSeparateFile))
            {
                throw new NotSupportedException("Cannot read bulk data from a separate file yet!");
            }

            if (Flags.HasFlag(BulkDataFlags.StoreOnlyPayload))
            {
                Debugger.Break();
            }

            Debug.Assert(StorageOffset != -1);
            Debug.Assert(StorageSize != -1);

            long returnPosition = stream.Position;
            stream.AbsolutePosition = StorageOffset;

            if (Flags.HasFlag(BulkDataFlags.Compressed))
            {
                stream.ReadStruct(out CompressedChunkHeader header);

                Debugger.Break();

                int offset = 0;
                var decompressFlags = Flags.ToCompressionFlags();
                foreach (var chunk in header.Chunks)
                {
                    stream.Read(ElementData, offset, chunk.CompressedSize);
                    offset += chunk.Decompress(ElementData, offset, decompressFlags);
                }

                Debug.Assert(offset == ElementData.Length);
            }
            else
            {
                stream.Read(ElementData, 0, ElementData.Length);
            }

            stream.Position = returnPosition;
        }

        public void Serialize(IUnrealStream stream)
        {
            Debug.Assert(ElementData != null);

            if (stream.Version < (uint)PackageObjectLegacyVersion.LazyArrayReplacedWithBulkData)
            {
                SerializeLegacyLazyArray(stream);
                return;
            }

            stream.Write((uint)Flags);

            int elementCount = ElementData.Length / Unsafe.SizeOf<TElement>();
            Debug.Assert(ElementCount == elementCount, "ElementCount mismatch");
            stream.Write(ElementCount);

            long storageSizePosition = stream.Position;
            stream.Write((int)StorageSize);
            stream.Write((int)StorageOffset);

            if (Flags.HasFlag(BulkDataFlags.StoreInSeparateFile))
            {
                return;
            }

            long storageOffsetPosition = stream.AbsolutePosition;
            stream.Write(ElementData);

            StorageOffset = storageOffsetPosition;
            StorageSize = stream.AbsolutePosition - StorageOffset;

            // Go back and rewrite the actual values.
            stream.Position = storageSizePosition;
            stream.Write((int)StorageSize);
            stream.Write((int)StorageOffset);
            // Restore
            stream.Position = StorageOffset + StorageSize;
        }

        private void SerializeLegacyLazyArray(IUnrealStream stream)
        {
            int elementSize = Unsafe.SizeOf<TElement>();
            Debug.Assert(ElementCount == ElementData.Length / elementSize, "ElementCount mismatch");

            if (stream.Version < (uint)PackageObjectLegacyVersion.LazyArraySkipCountChangedToSkipOffset)
            {
                stream.WriteIndex(ElementCount);

                StorageOffset = stream.AbsolutePosition;
                stream.Write(ElementData, 0, ElementData.Length);
                StorageSize = stream.AbsolutePosition - StorageOffset;
                return;
            }

            long storageDataPosition = stream.Position;
            const int fakeSkipOffset = 0;
            stream.Write(fakeSkipOffset);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.StorageSizeAddedToLazyArray)
            {
                stream.Write((int)StorageSize);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LazyLoaderFlagsAddedToLazyArray)
            {
                stream.Write((uint)Flags);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.L8AddedToLazyArray)
            {
                var dummy = new UName("");
                stream.Write(dummy);
            }

            stream.WriteIndex(ElementCount);
            StorageOffset = stream.AbsolutePosition;
            if (Flags.HasFlag(BulkDataFlags.StoreInSeparateFile))
            {
                throw new NotSupportedException("Cannot write bulk data to a separate file yet!");
            }

            if (Flags.HasFlag(BulkDataFlags.Compressed))
            {
                throw new NotSupportedException("Compressing bulk data is not yet supported.");
            }

            stream.Write(ElementData, 0, ElementData.Length);

            StorageSize = stream.AbsolutePosition - StorageOffset;

            long realSkipOffset = stream.AbsolutePosition;
            stream.Position = storageDataPosition;
            stream.Write((int)realSkipOffset);
            stream.AbsolutePosition = realSkipOffset;
        }

        public void Dispose() => ElementData = null;
    }

    [Flags]
    public enum BulkDataFlags : uint
    {
        // Formerly PayloadInSeparateFile? Attested in the assembly "!(LazyLoaderFlags & LLF_PayloadInSeparateFile)"
        StoreInSeparateFile = 0b1,
        CompressedZLIB = 0b10,
        ForceSingleElementSerialization = 0b100,
        SingleUse = 0b1000,
        CompressedLZO = 0b10000,
        Unused = 0b100000,
        StoreOnlyPayload = 0b1000000,
        CompressedLZX = 0b10000000,
        Compressed = CompressedZLIB | CompressedLZO | CompressedLZX
    }

    public static class BulkDataFlagsExtensions
    {
        public static CompressionFlags ToCompressionFlags(this BulkDataFlags flags)
        {
            uint cFlags = (((uint)flags & (uint)BulkDataFlags.CompressedZLIB) >> 1)
                          | (((uint)flags & (uint)BulkDataFlags.CompressedLZO) >> 3)
                          | (((uint)flags & (uint)BulkDataFlags.CompressedLZX) >> 5);
            return (CompressionFlags)cFlags;
        }
    }
}
