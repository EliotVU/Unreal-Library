using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UELib.Branch;
using UELib.Flags;
using UELib.IO;
using UELib.Services;

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

        /// <summary>
        /// The package name of the storage file.
        ///
        /// Serialized if version &gt;= <see cref="PackageObjectLegacyVersion.PackageNameAddedToLazyArray"/>
        /// and &lt;= <see cref="PackageObjectLegacyVersion.LazyArrayReplacedWithBulkData"/>
        /// </summary>
        private UName _StoragePackageName;

        public long StorageSize;
        public long StorageOffset;

        public int ElementCount;
        public byte[]? ElementData; // TElement (but, this requires Span<byte> usage when reading (N/A for .NET 4.8)

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
            InternalDeserialize(stream);

            if (Flags.HasFlag(BulkDataFlags.StoreInSeparateFile))
            {
                return;
            }

            if (Flags.HasFlag(BulkDataFlags.StoreOnlyPayload))
            {
                return;
            }

            try
            {
                if ((stream.Flags & UnrealArchiveFlags.LoadBulkData) != 0)
                {
                    LoadData(stream);
                }
            }
            catch (Exception exception)
            {
                LibServices.LogService.SilentException(exception);

                // Skip the bulk data that we have failed to load.
                stream.AbsolutePosition = StorageOffset + StorageSize;
            }
        }

        private void InternalDeserialize(IUnrealStream stream)
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

            // Skip the ElementData
            Debug.Assert(stream.AbsolutePosition == StorageOffset);
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

                stream.Position += StorageSize;

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

            if (stream.Version >= (uint)PackageObjectLegacyVersion.PackageNameAddedToLazyArray)
            {
                stream.Read(out _StoragePackageName);
            }

            ElementCount = stream.ReadLength(); // Use ReadLength for 'Vanguard'

            // Occurs with SCCT_Versus
            if (skipOffset == 0)
            {
                StorageSize = ElementCount * elementSize;
                StorageOffset = stream.AbsolutePosition;

                stream.Position += StorageSize;

                return;
            }

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
                return;
            }

            Debug.Assert(StorageOffset != -1);
            Debug.Assert(StorageSize != -1);

            long returnPosition = stream.Position;
            if (stream.AbsolutePosition != StorageOffset)
            {
                stream.AbsolutePosition = StorageOffset;
            }

            if (Flags.HasFlag(BulkDataFlags.Compressed))
            {
                stream.ReadStruct(out CompressedChunkHeader header);

                throw new NotSupportedException("Cannot read compressed bulk data yet!");

                int offset = 0;
                var decompressFlags = Flags.ToCompressionFlags();
                foreach (var chunk in header.Chunks)
                {
                    stream.Read(ElementData, offset, chunk.CompressedSize);
                    offset += chunk.Decompress(ElementData, offset, decompressFlags);
                }

                Debug.Assert(offset == ElementData.Length);

                if (ShouldReadSingleElement(stream))
                {
                    throw new NotSupportedException("Cannot read compressed bulk data by elements yet!");
                    //ReadElements(ElementData, ElementCount);
                }
            }
            else
            {
                // Read per element (Slow)
                if (ShouldReadSingleElement(stream))
                {
                    ReadElements(ElementData, ElementCount);
                }
                else // read in bulk
                {
                    stream.Read(ElementData, 0, ElementData.Length);
                }
            }

            stream.Position = returnPosition;
            return;

            // Beware, the sizeof must match the runtime memory size of an element in UE3
            // TODO: safe version? (Requires span)
            unsafe void ReadElements(byte[] elementData, int elementCount)
            {
                fixed (byte* ptr = elementData)
                {
                    for (int i = 0; i < elementCount; ++i)
                    {
                        byte* elementPtr = ptr + i * elementSize;
                        ReadElement(elementPtr);
                    }
                }
            }

            unsafe void ReadElement(byte* elementPtr)
            {
                // Yuck! Unsafe.ReadUnaligned<T>?
                if (typeof(TElement) == typeof(byte))
                {
                    *(byte*)elementPtr = stream.ReadByte();
                }
                else if (typeof(TElement) == typeof(short))
                {
                    *(short*)elementPtr = stream.ReadInt16();
                }
                else if (typeof(TElement) == typeof(ushort))
                {
                    *(ushort*)elementPtr = stream.ReadUInt16();
                }
                else if (typeof(TElement) == typeof(int))
                {
                    *(int*)elementPtr = stream.ReadInt32();
                }
                else if (typeof(TElement) == typeof(uint))
                {
                    *(uint*)elementPtr = stream.ReadUInt32();
                }
                else if (typeof(TElement) == typeof(float))
                {
                    *(float*)elementPtr = stream.ReadFloat();
                }
                else if (typeof(TElement) == typeof(long))
                {
                    *(long*)elementPtr = stream.ReadInt64();
                }
                else if (typeof(TElement) == typeof(ulong))
                {
                    *(ulong*)elementPtr = stream.ReadUInt64();
                }
                else
                {
                    throw new NotImplementedException(
                        $"Single element serialization for type {typeof(TElement)} is not supported.");
                }
            }
        }

        public void Serialize(IUnrealStream stream)
        {
            if (stream.Version < (uint)PackageObjectLegacyVersion.LazyArrayReplacedWithBulkData)
            {
                SerializeLegacyLazyArray(stream);

                return;
            }

            stream.Write((uint)Flags);

            if (ElementData != null)
            {
                int elementCount = ElementData.Length / Unsafe.SizeOf<TElement>();
                Debug.Assert(ElementCount == elementCount, "ElementCount mismatch");
            }

            stream.Write(ElementCount);

            long storageSizePosition = stream.Position;
            stream.Write((int)StorageSize);
            stream.Write((int)StorageOffset);

            if (Flags.HasFlag(BulkDataFlags.StoreInSeparateFile))
            {
                return;
            }

            if (Flags.HasFlag(BulkDataFlags.StoreOnlyPayload))
            {
                // TODO: Write to the end of file, or change the flags and write it right here.
                return;
            }

            long storageOffsetPosition = stream.AbsolutePosition;

            if (ElementCount > 0)
            {
                SaveData(stream);
            }

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
            Debug.Assert(ElementData == null || ElementCount == ElementData.Length / elementSize, "ElementCount mismatch");

            if (stream.Version < (uint)PackageObjectLegacyVersion.LazyArraySkipCountChangedToSkipOffset)
            {
                stream.WriteIndex(ElementCount);

                StorageOffset = stream.AbsolutePosition;
                if (ElementCount > 0)
                {
                    SaveData(stream);
                }

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

            if (stream.Version >= (uint)PackageObjectLegacyVersion.PackageNameAddedToLazyArray)
            {
                stream.Write(_StoragePackageName);
            }

            stream.WriteLength(ElementCount); // Use WriteLength for 'Vanguard'
            StorageOffset = stream.AbsolutePosition;

            if (Flags.HasFlag(BulkDataFlags.StoreInSeparateFile))
            {
                throw new NotSupportedException("Cannot write bulk data to a separate file yet!");
            }

            if (Flags.HasFlag(BulkDataFlags.Compressed))
            {
                throw new NotSupportedException("Compressing bulk data is not yet supported.");
            }

            if (ElementCount > 0)
            {
                SaveData(stream);
            }

            StorageSize = stream.AbsolutePosition - StorageOffset;

            long realSkipOffset = stream.AbsolutePosition;
            stream.Position = storageDataPosition;
            stream.Write((int)realSkipOffset);
            stream.AbsolutePosition = realSkipOffset;
        }

        /// <summary>
        /// Save the element data.
        /// </summary>
        private void SaveData(IUnrealStream stream)
        {
            Debug.Assert(ElementCount > 0);
            Contract.Assert(ElementData != null, "Cannot save bulk data that has not been loaded.");

            int elementSize = Unsafe.SizeOf<TElement>();

            // Need to merge from decompression-support branch.
            if (Flags.HasFlag(BulkDataFlags.Compressed))
            {
                int elementDataSize = ElementCount * elementSize;
                int segmentCount = (elementDataSize + 0x20000 - 1) / 0x20000 + 1;
                uint tag = stream.Package.Summary.Tag;

                var header = new CompressedChunkHeader
                {
                    Tag = tag,
                    ChunkSize = (int)tag,
                    Chunks = new UArray<CompressedChunkBlock>(segmentCount)
                };
                stream.WriteStruct(header);

                throw new NotSupportedException("Cannot save compressed bulk data yet!");
            }

            // Write per element (Slow)
            if (ShouldReadSingleElement(stream))
            {
                unsafe
                {
                    fixed (byte* ptr = ElementData)
                    {
                        for (int i = 0; i < ElementCount; ++i)
                        {
                            byte* elementPtr = ptr + i * elementSize;
                            WriteElement(elementPtr);
                        }
                    }
                }
            }
            else // write in bulk
            {
                stream.Write(ElementData, 0, ElementData.Length);
            }

            return;

            unsafe void WriteElement(byte* elementPtr)
            {
                // Yuck! Unsafe.ReadUnaligned<T>?
                if (typeof(TElement) == typeof(byte))
                {
                    stream.Write(*(byte*)elementPtr);
                }
                else if (typeof(TElement) == typeof(short))
                {
                    stream.Write(*(short*)elementPtr);
                }
                else if (typeof(TElement) == typeof(ushort))
                {
                    stream.Write(*(ushort*)elementPtr);
                }
                else if (typeof(TElement) == typeof(int))
                {
                    stream.Write(*(int*)elementPtr);
                }
                else if (typeof(TElement) == typeof(uint))
                {
                    stream.Write(*(uint*)elementPtr);
                }
                else if (typeof(TElement) == typeof(float))
                {
                    stream.Write(*(float*)elementPtr);
                }
                else if (typeof(TElement) == typeof(long))
                {
                    stream.Write(*(long*)elementPtr);
                }
                else if (typeof(TElement) == typeof(ulong))
                {
                    stream.Write(*(ulong*)elementPtr);
                }
                else
                {
                    throw new NotImplementedException(
                        $"Single element serialization for type {typeof(TElement)} is not supported.");
                }
            }
        }

        public void Dispose() => ElementData = null;

        /// <summary>
        /// Whether the bulk data should be read element per element.
        /// </summary>
        private bool ShouldReadSingleElement(IUnrealStream stream)
        {
            // Older TLazyArrayLike were serialized per element, but despite that,
            // we can still enable bulk reading if the archive is not big endian encoded.
            // However, let's not try this for 'encoded' archives in general. (decryption may be dependent on element-size)
            return (stream.Version < (uint)PackageObjectLegacyVersion.LazyArrayReplacedWithBulkData
                    && (stream.Flags & (UnrealArchiveFlags.BigEndian | UnrealArchiveFlags.Encoded)) != 0)
                   || Flags.HasFlag(BulkDataFlags.ForceSingleElementSerialization);
        }
    }

    [Flags]
    public enum BulkDataFlags : uint
    {
        // Formerly PayloadInSeparateFile? Attested in the assembly "!(LazyLoaderFlags & LLF_PayloadInSeparateFile)"
        StoreInSeparateFile = 0b1,
        CompressedZLIB = 0b10,
        // Apparently used for TLazyArrayLike bulk data.
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
