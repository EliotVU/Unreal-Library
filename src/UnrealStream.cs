using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UELib.Annotations;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;
using UELib.Flags;

namespace UELib
{
    public interface IUnrealArchive
    {
        UnrealPackage Package { get; }
        uint Version { get; }
        uint LicenseeVersion { get; }
        uint UE4Version { get; }

        bool BigEndianCode { get; }
        long LastPosition { get; set; }
    }

    public interface IUnrealStream : IUnrealArchive, IDisposable
    {
        UnrealPackage Package { get; }
        UnrealReader UR { get; }
        UnrealWriter UW { get; }
        
        [CanBeNull] IBufferDecoder Decoder { get; set; }

        void SetBranch(EngineBranch packageEngineBranch);

        string ReadText();
        string ReadASCIIString();

        int ReadObjectIndex();
        UObject ReadObject();

        [PublicAPI("UE Explorer")]
        UObject ParseObject(int index);

        int ReadNameIndex();

        [Obsolete]
        int ReadNameIndex(out int num);

        [PublicAPI("UE Explorer")]
        string ParseName(int index);

        /// <summary>
        /// Reads the next 1-5 (compact) consecutive bytes as an index.
        /// </summary>
        int ReadIndex();

        float ReadFloat();

        byte ReadByte();

        short ReadInt16();
        ushort ReadUInt16();

        int ReadInt32();
        uint ReadUInt32();

        long ReadInt64();
        ulong ReadUInt64();

        void Skip(int bytes);

        // Stream
        long Length { get; }
        long Position { get; set; }
        long LastPosition { get; set; }
        int Read(byte[] buffer, int index, int count);
        long Seek(long offset, SeekOrigin origin);
    }

    public class UnrealWriter : BinaryWriter
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        private IUnrealStream _UnrealStream;

        private uint _Version => _UnrealStream.Version;

        public UnrealWriter(Stream stream) : base(stream)
        {
            _UnrealStream = stream as IUnrealStream;
        }

        public void WriteText(string s)
        {
            Write(s.Length);
            Write(s.ToCharArray(), 0, 0);
            Write('\0');
        }

        // TODO: Add support for Unicode, and Unreal Engine 1 & 2.
        public void WriteString(string s)
        {
            WriteIndex(s.Length + 1);
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            Write(bytes, 0, bytes.Count());
            Write((byte)0);
        }

        public void WriteIndex(int index)
        {
            if (_Version >= UnrealPackage.VINDEXDEPRECATED)
                Write(index);
            else
                throw new InvalidDataException("UE1 and UE2 are not supported for writing indexes!");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _UnrealStream = null;
        }
    }

    /// <summary>
    /// Wrapper for Streams with specific functions for deserializing UELib.UnrealPackage.
    /// </summary>
    public class UnrealReader : BinaryReader
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected IUnrealArchive _Archive;

        // Dirty hack to implement crypto-reading, pending overhaul ;)
        public UnrealReader(IUnrealArchive archive, Stream baseStream) : base(baseStream)
        {
            _Archive = archive;
        }

        /// <summary>
        /// Reads a string that was serialized for Unreal Packages, these strings use a positive or negative size to:
        /// - indicate that the bytes were encoded in ASCII.
        /// - indicate that the bytes were encoded in Unicode.
        /// </summary>
        /// <returns>A string in either ASCII or Unicode.</returns>
        public string ReadText()
        {
#if BINARYMETADATA
            long lastPosition = BaseStream.Position;
#endif
            int unfixedSize = ReadIndex();
#if BIOSHOCK
            if (_Archive.Package.Build == BuildGeneration.Vengeance &&
                _Archive.Version >= 135)
            {
                unfixedSize = -unfixedSize;
            }
#endif
            int size = unfixedSize < 0
                ? -unfixedSize
                : unfixedSize;
            if (unfixedSize > 0) // ANSI
            {
                // We don't want to use base.Read here because it may reverse the buffer if we are using BigEndianOrder
                // TODO: Optimize
                var chars = new byte[size];
                for (var i = 0; i < chars.Length; ++i)
                {
                    byte c = ReadByte();
                    chars[i] = c;
                }
#if BINARYMETADATA
                _Archive.LastPosition = lastPosition;
#endif
                return chars[size - 1] == '\0'
                    ? Encoding.ASCII.GetString(chars, 0, chars.Length - 1)
                    : Encoding.ASCII.GetString(chars, 0, chars.Length);
            }

            if (unfixedSize < 0) // UNICODE
            {
                var chars = new char[size];
                for (var i = 0; i < chars.Length; ++i)
                {
                    var w = (char)ReadInt16();
                    chars[i] = w;
                }
#if BINARYMETADATA
                _Archive.LastPosition = lastPosition;
#endif
                return chars[size - 1] == '\0'
                    ? new string(chars, 0, chars.Length - 1)
                    : new string(chars);
            }
#if BINARYMETADATA
            _Archive.LastPosition = lastPosition;
#endif
            return string.Empty;
        }

        public string ReadAnsi()
        {
#if BINARYMETADATA
            long lastPosition = BaseStream.Position;
#endif
            var strBytes = new List<byte>();
        nextChar:
            byte c = ReadByte();
            if (c != '\0')
            {
                strBytes.Add(c);
                goto nextChar;
            }
#if BINARYMETADATA
            _Archive.LastPosition = lastPosition;
#endif
            string s = Encoding.UTF8.GetString(strBytes.ToArray());
            return s;
        }

        public string ReadUnicode()
        {
#if BINARYMETADATA
            long lastPosition = BaseStream.Position;
#endif
            var strBytes = new List<byte>();
        nextWord:
            short w = ReadInt16();
            if (w != 0)
            {
                strBytes.Add((byte)(w >> 8));
                strBytes.Add((byte)(w & 0x00FF));
                goto nextWord;
            }
#if BINARYMETADATA
            _Archive.LastPosition = lastPosition;
#endif
            string s = Encoding.Unicode.GetString(strBytes.ToArray());
            return s;
        }

        /// <summary>
        /// Unreal Engine 1 and 2
        /// Compact indices exist so that small numbers can be stored efficiently.
        /// An index named "Index" is stored as a series of 1-5 consecutive bytes.
        ///
        /// Unreal Engine 3
        /// The index is based on a Int32
        /// </summary>
        public int ReadIndex()
        {
            if (_Archive.Version >= UnrealPackage.VINDEXDEPRECATED) return ReadInt32();
#if BINARYMETADATA
            long lastPosition = BaseStream.Position;
#endif
            const byte isIndiced = 0x40; // 7th bit
            const byte isNegative = 0x80; // 8th bit
            const byte value = 0xFF - isIndiced - isNegative; // 3F
            const byte isProceeded = 0x80; // 8th bit
            const byte proceededValue = 0xFF - isProceeded; // 7F

            var index = 0;
            byte b0 = ReadByte();
            if ((b0 & isIndiced) != 0)
            {
                byte b1 = ReadByte();
                if ((b1 & isProceeded) != 0)
                {
                    byte b2 = ReadByte();
                    if ((b2 & isProceeded) != 0)
                    {
                        byte b3 = ReadByte();
                        if ((b3 & isProceeded) != 0)
                        {
                            byte b4 = ReadByte();
                            index = b4;
                        }

                        index = (index << 7) + (b3 & proceededValue);
                    }

                    index = (index << 7) + (b2 & proceededValue);
                }

                index = (index << 7) + (b1 & proceededValue);
            }
#if BINARYMETADATA
            _Archive.LastPosition = lastPosition;
#endif
            return (b0 & isNegative) != 0 // The value is negative or positive?.
                ? -((index << 6) + (b0 & value))
                : (index << 6) + (b0 & value);
        }

        public long ReadNameIndex()
        {
#if BINARYMETADATA
            long lastPosition = BaseStream.Position;
#endif
            int index = ReadIndex();
            if (_Archive.Version >= UName.VNameNumbered
#if BIOSHOCK
                || _Archive.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
               )
            {
                uint num = ReadUInt32() - 1;
#if BINARYMETADATA
                _Archive.LastPosition = lastPosition;
#endif
                return (long)((ulong)num << 32) | (uint)index;
            }

            return index;
        }

        public int ReadNameIndex(out int num)
        {
#if BINARYMETADATA
            long lastPosition = BaseStream.Position;
#endif
            int index = ReadIndex();
            if (_Archive.Version >= UName.VNameNumbered
#if BIOSHOCK
                || _Archive.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
               )
            {
                num = ReadInt32() - 1;
#if BINARYMETADATA
                _Archive.LastPosition = lastPosition;
#endif
                return index;
            }

            num = -1;
            return index;
        }

        [PublicAPI("UE Explorer - Hex Viewer")]
        public static int ReadIndexFromBuffer(byte[] value, IUnrealStream stream)
        {
            if (stream.Version >= UnrealPackage.VINDEXDEPRECATED) return BitConverter.ToInt32(value, 0);

            var index = 0;
            byte b0 = value[0];
            if ((b0 & 0x40) != 0)
            {
                byte b1 = value[1];
                if ((b1 & 0x80) != 0)
                {
                    byte b2 = value[2];
                    if ((b2 & 0x80) != 0)
                    {
                        byte b3 = value[3];
                        if ((b3 & 0x80) != 0)
                        {
                            byte b4 = value[4];
                            index = b4;
                        }

                        index = (index << 7) + (b3 & 0x7F);
                    }

                    index = (index << 7) + (b2 & 0x7F);
                }

                index = (index << 7) + (b1 & 0x7F);
            }

            return (b0 & 0x80) != 0 // The value is negative or positive?.
                ? -((index << 6) + (b0 & 0x3F))
                : (index << 6) + (b0 & 0x3F);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            _Archive = null;
        }
    }

    public class UPackageFileStream : FileStream
    {
        [CanBeNull] public IBufferDecoder Decoder { get; set; }

        protected UPackageFileStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode,
            access, share)
        {
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            long p = Position;
            int length = base.Read(buffer, index, count);
            Decoder?.DecodeRead(p, buffer, index, count);
            return length;
        }

        public override int ReadByte()
        {
            if (Decoder == null)
                return base.ReadByte();

            unsafe
            {
                long p = Position;
                var b = (byte)base.ReadByte();
                Decoder?.DecodeByte(p, &b);
                return b;
            }
        }
    }

    public class UPackageStream : UPackageFileStream, IUnrealStream, IUnrealArchive
    {
        public UnrealPackage Package { get; protected set; }
        
        public uint Version => Package.Version;
        public uint LicenseeVersion => Package.LicenseeVersion;
        public uint UE4Version => Package.Summary.UE4Version;

        public UnrealReader UR { get; private set; }
        public UnrealWriter UW { get; private set; }

        public long LastPosition { get; set; }
        
        public bool BigEndianCode { get; private set; }

        public bool IsChunked => Package.CompressedChunks != null && Package.CompressedChunks.Any();

        public UPackageStream(string path, FileMode mode, FileAccess access) : base(path, mode, access,
            FileShare.ReadWrite)
        {
            UR = null;
            UW = null;
            InitBuffer();
        }
        
        public IPackageSerializer Serializer { get; set; }

        public void SetBranch(EngineBranch packageEngineBranch)
        {
            Decoder = packageEngineBranch.Decoder;
            Serializer = packageEngineBranch.Serializer;
        }
        
        private void InitBuffer()
        {
            if (CanRead && UR == null) UR = new UnrealReader(this, this);

            if (CanWrite && UW == null) UW = new UnrealWriter(this);
        }

        public void PostInit(UnrealPackage package)
        {
            Package = package;

            if (!CanRead)
                return;

            Decoder?.PreDecode(this);

            var bytes = new byte[4];
            base.Read(bytes, 0, 4);
            var readSignature = BitConverter.ToUInt32(bytes, 0);
            if (readSignature == UnrealPackage.Signature_BigEndian)
            {
                Console.WriteLine("Encoding:BigEndian");
                BigEndianCode = true;
            }

            if (!UnrealConfig.SuppressSignature
                && readSignature != UnrealPackage.Signature
                && readSignature != UnrealPackage.Signature_BigEndian)
                throw new FileLoadException(package.PackageName + " isn't an UnrealPackage!");

            Position = 4;
        }

        public override int Read(byte[] buffer, int index, int count)
        {
#if BINARYMETADATA
            LastPosition = Position;
#endif
            int length = base.Read(buffer, index, count);
            if (BigEndianCode && length > 1) Array.Reverse(buffer, 0, length);
            return length;
        }

        public new byte ReadByte()
        {
#if BINARYMETADATA
            LastPosition = Position;
#endif
            return (byte)base.ReadByte();
        }

        public float ReadFloat()
        {
            return UR.ReadSingle();
        }

        #region Macros

        public ushort ReadUShort()
        {
            return UR.ReadUInt16();
        }

        public uint ReadUInt32()
        {
            return UR.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            return UR.ReadUInt64();
        }

        public short ReadInt16()
        {
            return UR.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return UR.ReadUInt16();
        }

        public int ReadInt32()
        {
            return UR.ReadInt32();
        }

        public long ReadInt64()
        {
            return UR.ReadInt64();
        }

        public string ReadText()
        {
            return UR.ReadText();
        }

        public string ReadASCIIString()
        {
            return UR.ReadAnsi();
        }

        public int ReadIndex()
        {
            return UR.ReadIndex();
        }

        public int ReadObjectIndex()
        {
            return UR.ReadIndex();
        }

        public UObject ReadObject()
        {
            return Package.GetIndexObject(ReadObjectIndex());
        }

        public UObject ParseObject(int index)
        {
            return Package.GetIndexObject(index);
        }

        public int ReadNameIndex()
        {
            return (int)UR.ReadNameIndex();
        }

        public string ParseName(int index)
        {
            return Package.GetIndexName(index);
        }

        public int ReadNameIndex(out int num)
        {
            return UR.ReadNameIndex(out num);
        }

        #endregion

        public void Skip(int bytes)
        {
            Position += bytes;
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            UR = null;
            UW = null;
        }
    }

    public class UObjectStream : MemoryStream, IUnrealStream, IUnrealArchive
    {
        public UnrealPackage Package { get; }
        
        public uint Version => Package.Version;
        public uint LicenseeVersion => Package.LicenseeVersion;
        public uint UE4Version => Package.Summary.UE4Version;
        
        [CanBeNull] public IBufferDecoder Decoder { get; set; }

        public string Name => Package.Stream.Name;

        public UnrealReader UR { get; private set; }
        public UnrealWriter UW { get; private set; }

        private long _PeekStartPosition;
        public long LastPosition { get; set; }

        public bool BigEndianCode => Package.Stream.BigEndianCode;

        public UObjectStream(IUnrealStream stream)
        {
            UW = null;
            UR = null;
            Package = stream.Package;
            InitBuffer();
        }

        public UObjectStream(IUnrealStream str, byte[] buffer) : base(buffer, true)
        {
            UW = null;
            UR = null;
            Package = str.Package;
            InitBuffer();
        }
        
        public void SetBranch(EngineBranch packageEngineBranch)
        {
            throw new NotImplementedException();
        }

        private void InitBuffer()
        {
            if (CanRead && UR == null) UR = new UnrealReader(this, this);

            if (CanWrite && UW == null) UW = new UnrealWriter(this);
        }

        public override int Read(byte[] buffer, int index, int count)
        {
#if BINARYMETADATA
            LastPosition = Position;
#endif
            int r = base.Read(buffer, index, count);
            if (BigEndianCode && r > 1) Array.Reverse(buffer, 0, r);

            return r;
        }

        public float ReadFloat()
        {
            return UR.ReadSingle();
        }

        #region Macros

        public new byte ReadByte()
        {
#if BINARYMETADATA
            LastPosition = Position;
#endif
            return UR.ReadByte();
        }

        public ushort ReadUShort()
        {
            return UR.ReadUInt16();
        }

        public uint ReadUInt32()
        {
            return UR.ReadUInt32();
        }

        public ulong ReadUInt64()
        {
            return UR.ReadUInt64();
        }

        public short ReadInt16()
        {
            return UR.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return UR.ReadUInt16();
        }

        public int ReadInt32()
        {
            return UR.ReadInt32();
        }

        public long ReadInt64()
        {
            return UR.ReadInt64();
        }

        public string ReadText()
        {
            return UR.ReadText();
        }

        public string ReadASCIIString()
        {
            return UR.ReadAnsi();
        }

        public int ReadIndex()
        {
            return UR.ReadIndex();
        }

        public int ReadObjectIndex()
        {
            return UR.ReadIndex();
        }

        public UObject ReadObject()
        {
            return Package.GetIndexObject(UR.ReadIndex());
        }

        public UObject ParseObject(int index)
        {
            return Package.GetIndexObject(index);
        }

        public int ReadNameIndex()
        {
            return (int)UR.ReadNameIndex();
        }

        public string ParseName(int index)
        {
            return Package.GetIndexName(index);
        }

        public int ReadNameIndex(out int num)
        {
            return UR.ReadNameIndex(out num);
        }

        #endregion

        public void Skip(int bytes)
        {
            Position += bytes;
        }

        /// <summary>
        /// Start peeking, without advancing the stream position.
        /// </summary>
        public void StartPeek()
        {
            _PeekStartPosition = Position;
        }

        /// <summary>
        /// Stop peeking, the original position is restored.
        /// </summary>
        public void EndPeek()
        {
            Position = _PeekStartPosition;
        }

        internal void DisposeBuffer()
        {
            if (UR != null)
            {
                UR.Dispose();
                UR = null;
            }

            if (UW != null)
            {
                UW.Dispose();
                UW = null;
            }

            Dispose();
        }
    }

    /// <summary>
    /// Methods that shouldn't be duplicated between UObjectStream and UPackageStream.
    /// </summary>
    [Obsolete("Don't use directly, in 2.0 these are implemented once in a single shared IUnrealStream")]
    public static class UnrealStreamImplementations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(this IUnrealStream stream)
        {
            return Convert.ToBoolean(stream.ReadInt32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadObject<T>(this IUnrealStream stream) where T : UObject
        {
            return (T)stream.Package.GetIndexObject(stream.ReadIndex());
        }

        [Obsolete("To be deprecated")]
        public static string ReadName(this IUnrealStream stream)
        {
            int index = stream.ReadNameIndex(out int num);
            string name = stream.Package.GetIndexName(index);
            if (num > UName.Numeric) name += $"_{num}";

            return name;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UName ReadNameReference(this IUnrealStream stream)
        {
            int index = stream.ReadNameIndex(out int number);
            var entry = stream.Package.Names[index];
            return new UName(entry, number);
        }

        /// <summary>
        /// Use this to read a compact integer for Arrays/Maps.
        /// TODO: Use a custom PackageStream and override ReadLength.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadLength(this IUnrealStream stream)
        {
#if VANGUARD
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Vanguard_SOH)
            {
                return stream.UR.ReadInt32();
            }
#endif
            return stream.UR.ReadIndex();
        }

        public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array)
            where T : IUnrealSerializableClass, new()
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            int c = stream.ReadLength();
            array = new UArray<T>(c);
            for (var i = 0; i < c; ++i)
            {
                var element = new T();
                element.Deserialize(stream);
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array, int count)
            where T : IUnrealSerializableClass, new()
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            array = new UArray<T>(count);
            for (var i = 0; i < count; ++i)
            {
                var element = new T();
                element.Deserialize(stream);
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadAtomicStruct<T>(this IUnrealStream stream, out T item)
            where T : IUnrealAtomicStruct
        {
            int structSize = Marshal.SizeOf<T>();
            var data = new byte[structSize];
            stream.Read(data, 0, structSize);
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            item = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();
        }

        // Can't seem to overload this :(
        public static void ReadMarshalArray<T>(this IUnrealStream stream, out UArray<T> array, int count)
            where T : IUnrealAtomicStruct
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            var structType = typeof(T);
            int structSize = Marshal.SizeOf(structType);
            array = new UArray<T>(count);
            for (var i = 0; i < count; ++i)
            {
                //ReadAtomicStruct(stream, out T element);
                var data = new byte[structSize];
                stream.Read(data, 0, structSize);
                var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var element = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), structType);
                handle.Free();
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<int> array)
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            int c = stream.ReadLength();
            array = new UArray<int>(c);
            for (var i = 0; i < c; ++i)
            {
                stream.Read(out int element);
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<string> array)
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            int c = stream.ReadLength();
            array = new UArray<string>(c);
            for (var i = 0; i < c; ++i)
            {
                string element = stream.ReadText();
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<UName> array)
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            int c = stream.ReadLength();
            array = new UArray<UName>(c);
            for (var i = 0; i < c; ++i)
            {
                UName element = stream.ReadNameReference();
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array)
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            int c = stream.ReadLength();
            array = new UArray<UObject>(c);
            for (var i = 0; i < c; ++i)
            {
                var element = stream.ReadObject();
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array, int count)
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            array = new UArray<UObject>(count);
            for (var i = 0; i < count; ++i)
            {
                var element = stream.ReadObject();
                array.Add(element);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        public static void ReadMap<TKey, TValue>(this IUnrealStream stream, out UMap<TKey, TValue> map)
            where TKey : UName
            where TValue : UObject
        {
#if BINARYMETADATA
            long position = stream.Position;
#endif
            int c = stream.ReadLength();
            map = new UMap<TKey, TValue>(c);
            for (var i = 0; i < c; ++i)
            {
                Read<TKey>(stream, out var key);
                Read<TValue>(stream, out var value);
                map.Add((TKey)key, (TValue)value);
            }
#if BINARYMETADATA
            stream.LastPosition = position;
#endif
        }

        // TODO: Implement FGuid
        public static Guid ReadGuid(this IUnrealStream stream)
        {
            // A, B, C, D
            var guidBuffer = new byte[16];
            stream.Read(guidBuffer, 0, 16);
            var guid = new Guid(guidBuffer);
            return guid;
        }

        public static UnrealFlags<TEnum> ReadFlags32<TEnum>(this IUnrealStream stream)
            where TEnum : Enum
        {
            stream.Package.Summary.Branch.EnumFlagsMap.TryGetValue(typeof(TEnum), out ulong[] enumMap);
            Debug.Assert(enumMap != null, nameof(enumMap) + " != null");

            ulong flags = stream.ReadUInt32();
            return new UnrealFlags<TEnum>(flags, ref enumMap);
        }

        public static UnrealFlags<TEnum> ReadFlags64<TEnum>(this IUnrealStream stream)
            where TEnum : Enum
        {
            stream.Package.Summary.Branch.EnumFlagsMap.TryGetValue(typeof(TEnum), out ulong[] enumMap);
            Debug.Assert(enumMap != null, nameof(enumMap) + " != null");

            ulong flags = stream.ReadUInt64();
            return new UnrealFlags<TEnum>(flags, ref enumMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VersionedDeserialize(this IUnrealStream stream, IUnrealDeserializableClass obj)
        {
            Debug.Assert(stream.Package.Branch.Serializer != null, "stream.Package.Branch.Serializer != null");
            stream.Package.Branch.Serializer.Deserialize(stream, obj);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out bool value)
        {
            value = ReadBool(stream);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(this IUnrealStream stream, out UObject value)
            where T : UObject
        {
            value = ReadObject<T>(stream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(this IUnrealStream stream, out UName value)
            where T : UName
        {
            value = ReadNameReference(stream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UName value)
        {
            value = ReadNameReference(stream);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(this IUnrealStream stream, out UArray<T> array)
            where T : IUnrealSerializableClass, new()
        {
            ReadArray(stream, out array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UArray<UObject> array)
        {
            ReadArray(stream, out array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<TKey, TValue>(this IUnrealStream stream, out UMap<TKey, TValue> map)
            where TKey : UName
            where TValue : UObject
        {
            ReadMap(stream, out map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out byte value)
        {
            value = stream.ReadByte();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out short value)
        {
            value = stream.ReadInt16();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out ushort value)
        {
            value = stream.ReadUInt16();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out int value)
        {
            value = stream.ReadInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out uint value)
        {
            value = stream.ReadUInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out long value)
        {
            value = stream.ReadInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out ulong value)
        {
            value = stream.ReadUInt64();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteArray<T>(this IUnrealStream stream, ref UArray<T> array)
            where T : IUnrealSerializableClass
        {
            Debug.Assert(array != null);
            WriteIndex(stream, array.Count);
            foreach (var element in array)
            {
                element.Serialize(stream);
            }
        }

        public static void Write(this IUnrealStream stream, UName name)
        {
            stream.UW.WriteIndex(name.Index);
            if (stream.Version < UName.VNameNumbered) return;
            stream.UW.Write((uint)name.Number + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, UObject obj)
        {
            stream.UW.WriteIndex(obj != null ? (int)obj : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this IUnrealStream stream, ref UArray<T> array)
            where T : IUnrealSerializableClass
        {
            WriteArray(stream, ref array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, byte value)
        {
            stream.UW.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, short value)
        {
            stream.UW.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, ushort value)
        {
            stream.UW.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, int value)
        {
            stream.UW.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, uint value)
        {
            stream.UW.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, bool value)
        {
            stream.UW.Write(Convert.ToInt32(value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, long value)
        {
            stream.UW.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, ulong value)
        {
            stream.UW.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, byte[] buffer, int index, int count)
        {
            stream.UW.Write(buffer, index, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteIndex(this IUnrealStream stream, int index)
        {
            stream.UW.WriteIndex(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, string value)
        {
            stream.UW.WriteString(value);
        }
    }
}