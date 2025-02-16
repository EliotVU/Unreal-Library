using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UELib.Annotations;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;
using UELib.Flags;
using UELib.Services;

namespace UELib
{
    public interface IUnrealArchive
    {
        UnrealPackage Package { get; }
        uint Version { get; }
        uint LicenseeVersion { get; }
        uint UE4Version { get; }

        bool BigEndianCode { get; }
    }

    public interface IUnrealStream : IUnrealArchive, IDisposable
    {
        UnrealPackage Package { get; }

        [Obsolete("To be deprecated")] UnrealReader UR { get; }
        [Obsolete("To be deprecated")] UnrealWriter UW { get; }

        [CanBeNull] IBufferDecoder Decoder { get; set; }
        IPackageSerializer Serializer { get; set; }

        long Position { get; set; }

        // HACK: To be deprecated, but for now we need this to help us retrieve the absolute position when serializing within an UObject's buffer.
        long AbsolutePosition { get; set; }
        
        // We need to virtualize this so we can override the logic to track where objects are read.
        T ReadObject<T>() where T : UObject;
        
        // We need to virtualize this so we can override the logic to track where objects are written.
        void WriteObject<T>(T value) where T : UObject;

        // We need to virtualize this so we can override the logic to track where names are read.
        UName ReadName();
        
        // We need to virtualize this so we can override the logic to track where names are written.
        void WriteName(in UName value);

        void Skip(int bytes);

        int Read(byte[] buffer, int index, int count);
        long Seek(long offset, SeekOrigin origin);

        public IUnrealStream Record(string name, [CanBeNull] object value);
        public void ConformRecordPosition();

        [Obsolete("Use ReadObject")]
        int ReadObjectIndex();

        [Obsolete("Use ReadObject")]
        UObject ParseObject(int index);

        [Obsolete("use ReadName")]
        int ReadNameIndex();

        [Obsolete("use ReadName")]
        int ReadNameIndex(out int num);

        [Obsolete("Use ReadName")]
        string ParseName(int index);
    }

    public class UnrealWriter : BinaryWriter
    {
        private readonly byte[] _IndexBuffer = new byte[5];

        public UnrealWriter(IUnrealArchive archive, Stream baseStream) : base(baseStream) => Archive = archive;

        public IUnrealArchive Archive { get; private set; }

        [Obsolete("Use WriteString")]
        public void WriteText(string s) => WriteString(s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUnicode(string s) => s.Any(c => c >= 127);

        public void WriteString(string s)
        {
            if (!IsUnicode(s))
            {
                int length = s.Length + 1;
                WriteIndex(length);
                foreach (byte c in s)
                {
                    BaseStream.WriteByte(c);
                }

                BaseStream.WriteByte(0);
            }
            else
            {
                int length = s.Length + 1;
                WriteIndex(-length);
                foreach (char c in s)
                {
                    Write((short)c);
                }

                BaseStream.WriteByte(0);
                BaseStream.WriteByte(0);
            }
        }

        public void WriteAnsi(string s)
        {
            byte[] data = Encoding.UTF8.GetBytes(s);
            // TODO: Byte-order
            BaseStream.Write(data, 0, data.Length);
            BaseStream.WriteByte(0);
        }

        public void WriteUnicode(string s)
        {
            byte[] data = Encoding.Unicode.GetBytes(s);
            // TODO: Byte-order
            BaseStream.Write(data, 0, data.Length);
            BaseStream.WriteByte(0);
            BaseStream.WriteByte(0);
        }

        public void WriteCompactIndex(int index)
        {
            bool isPositive = index >= 0;
            index = Math.Abs(index);
            byte b0 = (byte)((isPositive ? 0 : 0x80) + (index < 0x40 ? index : (index & 0x3F) + 0x40));
            _IndexBuffer[0] = b0;
            BaseStream.Write(_IndexBuffer, 0, 1);
            if ((b0 & 0x40) != 0)
            {
                index >>= 6;
                byte b1 = (byte)(index < 0x80 ? index : (index & 0x7F) + 0x80);
                _IndexBuffer[1] = b1;
                BaseStream.Write(_IndexBuffer, 1, 1);
                if ((b1 & 0x80) != 0)
                {
                    index >>= 7;
                    byte b2 = (byte)(index < 0x80 ? index : (index & 0x7F) + 0x80);
                    _IndexBuffer[2] = b1;
                    BaseStream.Write(_IndexBuffer, 2, 1);
                    if ((b2 & 0x80) != 0)
                    {
                        index >>= 7;
                        byte b3 = (byte)(index < 0x80 ? index : (index & 0x7F) + 0x80);
                        _IndexBuffer[3] = b1;
                        BaseStream.Write(_IndexBuffer, 3, 1);
                        if ((b3 & 0x80) != 0)
                        {
                            _IndexBuffer[4] = (byte)(index >> 7);
                            BaseStream.Write(_IndexBuffer, 4, 1);
                        }
                    }
                }
            }
        }

        public void WriteIndex(int index)
        {
            if (Archive.Version >= (uint)PackageObjectLegacyVersion.CompactIndexDeprecated)
            {
                Write(index);
                return;
            }

            WriteCompactIndex(index);
        }

        /// <summary>
        /// Serializes a <seealso cref="UName"/>
        /// TODO: Version logic should be wrapped by an overlaying stream instead.
        /// </summary>
        public void WriteName(in UName value)
        {
            WriteIndex(value.Index);

#if SHADOWSTRIKE
            if (Archive.Package.Build == BuildGeneration.ShadowStrike)
            {
                LibServices.LogService.SilentException(new NotSupportedException("Writing index 0 for ShadowStrike."));
                WriteIndex(0);
            }
#endif
            if (Archive.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName
#if BIOSHOCK
                || Archive.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
               )
            {
                Write(value.Number + 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            Archive = null;
        }
    }

    /// <summary>
    ///     Wrapper for Streams with specific functions for deserializing UELib.UnrealPackage.
    /// </summary>
    public class UnrealReader : BinaryReader
    {
        private readonly byte[] _IndexBuffer = new byte[5];

        // Dirty hack to implement crypto-reading, pending overhaul ;)
        public UnrealReader(IUnrealArchive archive, Stream baseStream) : base(baseStream) => Archive = archive;

        public IUnrealArchive Archive { get; private set; }

        /// <summary>Reads a string from the current stream.</summary>
        /// <param name="length">The length in characters; a negative length indicates an unicode string.</param>
        /// <returns>The string being read with the null termination cut off.</returns>
        public string ReadString(int length)
        {
            int size = length < 0
                ? -length
                : length;
            if (length > 0) // ANSI
            {
                byte[] chars = new byte[size];
                for (int i = 0; i < chars.Length; ++i)
                {
                    BaseStream.Read(chars, i, 1);
                }

                return chars[size - 1] == '\0'
                    ? Encoding.ASCII.GetString(chars, 0, chars.Length - 1)
                    : Encoding.ASCII.GetString(chars, 0, chars.Length);
            }

            if (length < 0) // UNICODE
            {
                char[] chars = new char[size];
                for (int i = 0; i < chars.Length; ++i)
                {
                    char w = (char)ReadInt16();
                    chars[i] = w;
                }

                return chars[size - 1] == '\0'
                    ? new string(chars, 0, chars.Length - 1)
                    : new string(chars);
            }

            return string.Empty;
        }

        /// <summary>Reads a length prefixed string from the current stream.</summary>
        /// <returns>The string being read with the null termination cut off.</returns>
        public override string ReadString()
        {
            int unfixedSize = ReadIndex();
#if BIOSHOCK
            // TODO: Make this a build option instead.
            if (Archive.Package.Build == BuildGeneration.Vengeance &&
                Archive.Version >= 135)
            {
                unfixedSize = -unfixedSize;
            }
#endif
            return ReadString(unfixedSize);
        }

        [Obsolete("Use ReadString")]
        public string ReadText() => ReadString();

        public string ReadAnsi()
        {
            var strBytes = new List<byte>();
        nextChar:
            byte c = (byte)BaseStream.ReadByte();
            if (c != '\0')
            {
                strBytes.Add(c);
                goto nextChar;
            }

            string s = Encoding.UTF8.GetString(strBytes.ToArray());
            return s;
        }

        public string ReadUnicode()
        {
            var strBytes = new List<byte>();
        nextWord:
            short w = ReadInt16();
            if (w != 0)
            {
                strBytes.Add((byte)w);
                strBytes.Add((byte)(w >> 8));
                goto nextWord;
            }

            string s = Encoding.Unicode.GetString(strBytes.ToArray());
            return s;
        }

        /// <summary>Reads a compact 1-5-byte signed integer from the current stream.</summary>
        /// <returns>A 4-byte signed integer read from the current stream.</returns>
        public int ReadCompactIndex()
        {
            int index = 0;
            BaseStream.Read(_IndexBuffer, 0, 1);
            byte b0 = _IndexBuffer[0];
            if ((b0 & 0x40) != 0)
            {
                BaseStream.Read(_IndexBuffer, 1, 1);
                byte b1 = _IndexBuffer[1];
                if ((b1 & 0x80) != 0)
                {
                    BaseStream.Read(_IndexBuffer, 2, 1);
                    byte b2 = _IndexBuffer[2];
                    if ((b2 & 0x80) != 0)
                    {
                        BaseStream.Read(_IndexBuffer, 3, 1);
                        byte b3 = _IndexBuffer[3];
                        if ((b3 & 0x80) != 0)
                        {
                            BaseStream.Read(_IndexBuffer, 4, 1);
                            byte b4 = _IndexBuffer[4];
                            index = b4;
                        }

                        index = (index << 7) + (b3 & 0x7F);
                    }

                    index = (index << 7) + (b2 & 0x7F);
                }

                index = (index << 7) + (b1 & 0x7F);
            }

            index = (index << 6) + (b0 & 0x3F);
            return (b0 & 0x80) != 0 ? -index : index;
        }

        /// <summary>Reads an index from the current stream.</summary>
        /// <returns>A 4-byte signed integer read from the current stream.</returns>
        public int ReadIndex() =>
            Archive.Version >= (uint)PackageObjectLegacyVersion.CompactIndexDeprecated
                ? ReadInt32()
                : ReadCompactIndex();

        /// <summary>
        /// Deserializes a <seealso cref="UName"/> from the base stream.
        /// TODO: Version logic should be wrapped by an overlaying stream instead.
        /// </summary>
        /// <returns>A unique UName representing the number and index to a <seealso cref="UNameTableItem"/></returns>
        public UName ReadName()
        {
            int index = ReadIndex();
            int number = -1;
            
#if SHADOWSTRIKE
            if (Archive.Package.Build == BuildGeneration.ShadowStrike)
            {
                int externalIndex = ReadIndex();
            }
#endif
            if (Archive.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName
#if BIOSHOCK
                || Archive.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
               )
            {
                number = ReadInt32() - 1;
            }

            var nameEntry = Archive.Package.Names[index];
            return new UName(nameEntry, number);
        }

        [Obsolete]
        // HACK: Specific builds logic should be displaced by a specialized stream.
        public int ReadNameIndex(out int num)
        {
            int index = ReadIndex();
#if SHADOWSTRIKE
            if (Archive.Package.Build == BuildGeneration.ShadowStrike)
            {
                int externalIndex = ReadIndex();
            }
#endif
            if (Archive.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName
#if BIOSHOCK
                || Archive.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock
#endif
               )
            {
                num = ReadInt32() - 1;
                return index;
            }

            num = -1;
            return index;
        }

        [Obsolete("UE Explorer - Hex Viewer")]
        public static int ReadIndexFromBuffer(byte[] value, IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.CompactIndexDeprecated)
            {
                return BitConverter.ToInt32(value, 0);
            }

            int index = 0;
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
            {
                return;
            }

            Archive = null;
        }
    }

    public class UPackageFileStream : FileStream
    {
        protected UPackageFileStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode,
            access, share)
        {
        }

        [CanBeNull] public IBufferDecoder Decoder { get; set; }
        [CanBeNull] public IPackageSerializer Serializer { get; set; }

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
            {
                return base.ReadByte();
            }

            unsafe
            {
                long p = Position;
                byte b = (byte)base.ReadByte();
                Decoder?.DecodeByte(p, &b);
                return b;
            }
        }
    }

    public class UPackageStream : UPackageFileStream, IUnrealStream, IUnrealArchive
    {
        public UPackageStream(string path, FileMode mode, FileAccess access) : base(path, mode, access,
            FileShare.ReadWrite)
        {
            Reader = null;
            Writer = null;
            InitBuffer();
        }

        private UnrealReader Reader { get; set; }

        internal UnrealWriter Writer { get; set; }

        public long LastPosition { get; set; }

        public bool IsChunked => Package.CompressedChunks != null && Package.CompressedChunks.Any();
        public UnrealPackage Package { get; private set; }

        public uint Version => Package.Version;
        public uint LicenseeVersion => Package.LicenseeVersion;
        public uint UE4Version => Package.Summary.UE4Version;

        UnrealReader IUnrealStream.UR => Reader;

        UnrealWriter IUnrealStream.UW => Writer;

        public long AbsolutePosition
        {
            get => Position;
            set => Position = value;
        }

        public bool BigEndianCode { get; private set; }

        public override int Read(byte[] buffer, int index, int count)
        {
            int length = base.Read(buffer, index, count);
            if (BigEndianCode && length > 1)
            {
                Array.Reverse(buffer, 0, length);
            }

            return length;
        }

        public IUnrealStream Record(string name, object value)
        {
            return this;
        }

        public void ConformRecordPosition()
        {
            // Do nothing, we don't record anything for this stream.
        }

        [Obsolete]
        public int ReadObjectIndex() => Reader.ReadIndex();

        [Obsolete]
        public UObject ParseObject(int index) => Package.GetIndexObject(index);

        [Obsolete]
        public int ReadNameIndex() => Reader.ReadNameIndex(out int _);

        [Obsolete]
        public string ParseName(int index) => Package.GetIndexName(index);

        [Obsolete]
        public int ReadNameIndex(out int num) => Reader.ReadNameIndex(out num);

        [CanBeNull]
        public T ReadObject<T>() where T : UObject
        {
            int index = Reader.ReadIndex();
            return (T)Package.GetIndexObject(index);
        }

        public void WriteObject<T>([CanBeNull] T value) where T : UObject
        {
            Writer.WriteIndex((int)value);
        }

        public UName ReadName()
        {
            return Reader.ReadName();
        }

        public void WriteName(in UName value)
        {
            Writer.WriteName(value);
        }

        public void Skip(int bytes) => Position += bytes;

        private void InitBuffer()
        {
            if (CanRead && Reader == null)
            {
                Reader = new UnrealReader(this, this);
            }

            if (CanWrite && Writer == null)
            {
                Writer = new UnrealWriter(this, this);
            }
        }

        public void PostInit(UnrealPackage package)
        {
            Package = package;

            if (!CanRead)
            {
                return;
            }

            Decoder?.PreDecode(this);

            byte[] bytes = new byte[4];
            base.Read(bytes, 0, 4);
            uint readSignature = BitConverter.ToUInt32(bytes, 0);
            if (readSignature == UnrealPackage.Signature_BigEndian)
            {
                Console.WriteLine("Encoding:BigEndian");
                BigEndianCode = true;
            }

            if (!UnrealConfig.SuppressSignature
                && readSignature != UnrealPackage.Signature
                && readSignature != UnrealPackage.Signature_BigEndian)
            {
                throw new FileLoadException(package.PackageName + " isn't an UnrealPackage!");
            }

            Package.Summary.Tag = readSignature;

            Position = 4;
        }

        public int EndianAgnosticRead(byte[] buffer, int index, int count)
        {
            int length = base.Read(buffer, index, count);
            return length;
        }

        public new byte ReadByte() => (byte)base.ReadByte();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Reader = null;
                Writer = null;
            }
        }
    }

    public class UObjectStream : MemoryStream, IUnrealStream
    {
        private readonly long _ObjectPositionInPackage;

        private long _PeekStartPosition;

        public UObjectStream(IUnrealStream stream)
        {
            _ObjectPositionInPackage = stream.Position;

            Writer = null;
            Reader = null;
            Package = stream.Package;
            InitBuffer();
        }

        public UObjectStream(IUnrealStream str, byte[] buffer) : base(buffer, true)
        {
            _ObjectPositionInPackage = str.Position;

            Writer = null;
            Reader = null;
            Package = str.Package;
            InitBuffer();
        }

        public string Name => Package.Stream.Name;

        private UnrealReader Reader { get; set; }

        private UnrealWriter Writer { get; set; }

        [Obsolete("Displaced")]
        public long LastPosition { get; set; }
        public UnrealPackage Package { get; }

        public uint Version => Package.Version;
        public uint LicenseeVersion => Package.LicenseeVersion;
        public uint UE4Version => Package.Summary.UE4Version;

        public IBufferDecoder Decoder { get; set; }
        public IPackageSerializer Serializer { get; set; }
        UnrealReader IUnrealStream.UR => Reader;
        UnrealWriter IUnrealStream.UW => Writer;

        public long AbsolutePosition
        {
            get => Position + _ObjectPositionInPackage;
            set => Position = value - _ObjectPositionInPackage;
        }

        public bool BigEndianCode => Package.Stream.BigEndianCode;

        public override int Read(byte[] buffer, int index, int count)
        {
            int r = base.Read(buffer, index, count);
            if (BigEndianCode && r > 1)
            {
                Array.Reverse(buffer, 0, r);
            }

            return r;
        }

        public virtual IUnrealStream Record(string name, object value)
        {
            return this;
        }

        public void ConformRecordPosition()
        {
        }

        [Obsolete]
        public int ReadObjectIndex() => Reader.ReadIndex();

        [Obsolete]
        public UObject ParseObject(int index) => Package.GetIndexObject(index);

        [Obsolete]
        public int ReadNameIndex() => Reader.ReadNameIndex(out int _);

        [Obsolete]
        public string ParseName(int index) => Package.GetIndexName(index);

        [Obsolete]
        public int ReadNameIndex(out int num) => Reader.ReadNameIndex(out num);

        [CanBeNull]
        public T ReadObject<T>() where T : UObject
        {
            int index = Reader.ReadIndex();
            return (T?)Package.GetIndexObject(index);
        }

        public void WriteObject<T>([CanBeNull] T value) where T : UObject
        {
            Writer.WriteIndex((int)value);
        }

        public UName ReadName()
        {
            return Reader.ReadName();
        }

        public void WriteName(in UName value)
        {
            Writer.WriteName(value);
        }

        public void Skip(int bytes) => Position += bytes;

        private void InitBuffer()
        {
            if (CanRead && Reader == null)
            {
                Reader = new UnrealReader(this, this);
            }

            if (CanWrite && Writer == null)
            {
                Writer = new UnrealWriter(this, this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new byte ReadByte() => Reader.ReadByte();

        /// <summary>
        ///     Start peeking, without advancing the stream position.
        /// </summary>
        public void StartPeek() => _PeekStartPosition = Position;

        public void StartPeek(long position)
        {
            _PeekStartPosition = Position;
            Position = position;
        }

        /// <summary>
        ///     Stop peeking, the original position is restored.
        /// </summary>
        public void EndPeek() => Position = _PeekStartPosition;

        internal void DisposeBuffer()
        {
            if (Reader != null)
            {
                Reader.Dispose();
                Reader = null;
            }

            if (Writer != null)
            {
                Writer.Dispose();
                Writer = null;
            }
        }
    }

    public sealed class UObjectRecordStream : UObjectStream
    {
#if BINARYMETADATA
        private long _LastRecordPosition;
#endif
        public UObjectRecordStream(IUnrealStream stream, byte[] buffer) : base(stream, buffer) =>
            BinaryMetaData = new BinaryMetaData();
#if BINARYMETADATA
        public BinaryMetaData BinaryMetaData { get; }

        /// <summary>
        ///     TODO: Move this feature into a stream.
        ///     Outputs the present position and the value of the parsed object.
        ///     Only called in the DEBUGBUILD!
        /// </summary>
        /// <param name="varName">The struct that was read from the previous buffer position.</param>
        /// <param name="varObject">The struct's value that was read.</param>
        public override IUnrealStream Record(string varName, object varObject = null)
        {
            long size = Position - _LastRecordPosition;
            BinaryMetaData.AddField(varName, varObject, _LastRecordPosition, size);
            _LastRecordPosition = Position;

            return this;
        }
#endif

        [Conditional("BINARYMETADATA")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConformRecordPosition() => _LastRecordPosition = Position;
    }

    /// <summary>
    ///     Methods that shouldn't be duplicated between UObjectStream and UPackageStream.
    /// </summary>
    [Obsolete("Pending deprecation")]
    public static class UnrealStreamImplementations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(this IUnrealStream stream)
        {
            byte[] buffer = new byte[1];
            stream.Read(buffer, 0, 1);
            return buffer[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUShort(this IUnrealStream stream) => stream.UR.ReadUInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this IUnrealStream stream) => stream.UR.ReadUInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(this IUnrealStream stream) => stream.UR.ReadUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this IUnrealStream stream) => stream.UR.ReadInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this IUnrealStream stream) => stream.UR.ReadUInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this IUnrealStream stream) => stream.UR.ReadInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this IUnrealStream stream) => stream.UR.ReadInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(this IUnrealStream stream) => stream.UR.ReadSingle();

        [Obsolete("See ReadString")]
        public static string ReadText(this IUnrealStream stream) => stream.UR.ReadString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadString(this IUnrealStream stream) => stream.UR.ReadString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadAnsiNullString(this IUnrealStream stream) => stream.UR.ReadAnsi();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadUnicodeNullString(this IUnrealStream stream) => stream.UR.ReadUnicode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(this IUnrealStream stream)
        {
            int value = stream.ReadInt32();
            Debug.Assert(value <= 1, $"Unexpected value '{value}' for a boolean");
            return Convert.ToBoolean(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadIndex(this IUnrealStream stream) => stream.UR.ReadIndex();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UObject ReadObject(this IUnrealStream stream) => stream.ReadObject<UObject>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadObject<T>(this IUnrealStream stream) where T : UObject => stream.ReadObject<T>();

        [Obsolete("To be deprecated")]
        public static string ReadName(this IUnrealStream stream) => stream.ReadName();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UName ReadNameReference(this IUnrealStream stream) => stream.ReadName();

        /// <summary>
        ///     Use this to read a compact integer for Arrays/Maps.
        ///     TODO: Use a custom PackageStream and override ReadLength.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadLength(this IUnrealStream stream)
        {
#if VANGUARD
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Vanguard_SOH)
            {
                return stream.ReadInt32();
            }
#endif
            return stream.ReadIndex();
        }

        public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array)
            where T : IUnrealDeserializableClass, new()
        {
            int c = stream.ReadLength();
            array = new UArray<T>(c);
            for (int i = 0; i < c; ++i)
            {
                var element = new T();
                element.Deserialize(stream);
                array.Add(element);
            }
        }

        public static void ReadArray<T>(this IUnrealStream stream, out UArray<T> array, int count)
            where T : IUnrealDeserializableClass, new()
        {
            array = new UArray<T>(count);
            for (int i = 0; i < count; ++i)
            {
                var element = new T();
                element.Deserialize(stream);
                array.Add(element);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadStruct<T>(this IUnrealStream stream, out T item)
            where T : struct, IUnrealDeserializableClass
        {
            item = new T();
            item.Deserialize(stream);
        }

        public static unsafe void ReadStructMarshal<T>(this IUnrealStream stream, out T item)
            where T : unmanaged, IUnrealAtomicStruct
        {
            int structSize = sizeof(T);
            byte[] data = new byte[structSize];
            stream.Read(data, 0, structSize);
            fixed (byte* ptr = &data[0])
            {
                item = *(T*)ptr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadClass<T>(this IUnrealStream stream, out T item)
            where T : class, IUnrealDeserializableClass, new()
        {
            item = new T();
            item.Deserialize(stream);
        }

        // Can't seem to overload this :(
        public static unsafe void ReadArrayMarshal<T>(this IUnrealStream stream, out UArray<T> array, int count)
            where T : unmanaged, IUnrealAtomicStruct
        {
            int structSize = sizeof(T);
            array = new UArray<T>(count);
            byte[] data = new byte[structSize * count];
            stream.Read(data, 0, structSize * count);

            for (int i = 0; i < count; ++i)
            {
                fixed (byte* ptr = &data[i * structSize])
                {
                    var element = *(T*)ptr;
                    array.Add(element);
                }
            }
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<byte> array)
        {
            int c = stream.ReadLength();
            var data = new byte[c];
            stream.Read(data, 0, c);
            // FIXME: Surely this could be done much faster!
            array = new UArray<byte>(c);
            array.AddRange(data);
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<int> array)
        {
            int c = stream.ReadLength();
            array = new UArray<int>(c);
            for (int i = 0; i < c; ++i)
            {
                stream.Read(out int element);
                array.Add(element);
            }
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<float> array)
        {
            int c = stream.ReadLength();
            array = new UArray<float>(c);
            for (int i = 0; i < c; ++i)
            {
                Read(stream, out float element);
                array.Add(element);
            }
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<string> array)
        {
            int c = stream.ReadLength();
            array = new UArray<string>(c);
            for (int i = 0; i < c; ++i)
            {
                string element = stream.ReadString();
                array.Add(element);
            }
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<UName> array)
        {
            int c = stream.ReadLength();
            array = new UArray<UName>(c);
            for (int i = 0; i < c; ++i)
            {
                var element = stream.ReadNameReference();
                array.Add(element);
            }
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array)
        {
            int c = stream.ReadLength();
            array = new UArray<UObject>(c);
            for (int i = 0; i < c; ++i)
            {
                var element = stream.ReadObject<UObject>();
                array.Add(element);
            }
        }

        public static void ReadArray(this IUnrealStream stream, out UArray<UObject> array, int count)
        {
            array = new UArray<UObject>(count);
            for (int i = 0; i < count; ++i)
            {
                var element = stream.ReadObject<UObject>();
                array.Add(element);
            }
        }

        public static void ReadMap(this IUnrealStream stream, out UMap<ushort, ushort> map)
        {
            int c = stream.ReadLength();
            map = new UMap<ushort, ushort>(c);
            for (int i = 0; i < c; ++i)
            {
                Read(stream, out ushort key);
                Read(stream, out ushort value);
                map.Add(key, value);
            }
        }

        public static void ReadMap<TKey, TValue>(this IUnrealStream stream, out UMap<TKey, TValue> map)
            where TKey : UName
            where TValue : UObject
        {
            int c = stream.ReadLength();
            map = new UMap<TKey, TValue>(c);
            for (int i = 0; i < c; ++i)
            {
                Read(stream, out UName key);
                Read(stream, out TValue value);
                map.Add((TKey)key, value);
            }
        }

        public static void ReadMap(this IUnrealStream stream, out UMap<string, UArray<string>> map)
        {
            int c = stream.ReadLength();
            map = new UMap<string, UArray<string>>(c);
            for (int i = 0; i < c; ++i)
            {
                Read(stream, out UName key);
                ReadArray(stream, out UArray<string> value);
                map.Add(key, value);
            }
        }

        public static void ReadMap(this IUnrealStream stream, out UMap<UObject, UName> map)
        {
            int c = stream.ReadLength();
            map = new UMap<UObject, UName>(c);
            for (int i = 0; i < c; ++i)
            {
                Read(stream, out UObject key);
                Read(stream, out UName value);
                map.Add(key, value);
            }
        }

        [Obsolete("See UGuid")]
        public static Guid ReadGuid(this IUnrealStream stream)
        {
            // A, B, C, D
            byte[] guidBuffer = new byte[16];
            stream.Read(guidBuffer, 0, 16);
            var guid = new Guid(guidBuffer);
            return guid;
        }

        public static UnrealFlags<TEnum> ReadFlags32<TEnum>(this IUnrealStream stream)
            where TEnum : Enum
        {
            stream.Package.Branch.EnumFlagsMap.TryGetValue(typeof(TEnum), out ulong[] enumMap);
            Debug.Assert(enumMap != null, nameof(enumMap) + " != null");

            ulong flags = stream.ReadUInt32();
            return new UnrealFlags<TEnum>(flags, enumMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(this IUnrealStream stream, out UnrealFlags<T> value) where T : Enum =>
            value = ReadFlags32<T>(stream);

        public static UnrealFlags<TEnum> ReadFlags64<TEnum>(this IUnrealStream stream)
            where TEnum : Enum
        {
            stream.Package.Branch.EnumFlagsMap.TryGetValue(typeof(TEnum), out ulong[] enumMap);
            Debug.Assert(enumMap != null, nameof(enumMap) + " != null");

            ulong flags = stream.ReadUInt64();
            return new UnrealFlags<TEnum>(flags, enumMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VersionedDeserialize(this IUnrealStream stream, IUnrealDeserializableClass obj)
        {
            Debug.Assert(stream.Serializer != null, "stream.Serializer != null");
            stream.Serializer.Deserialize(stream, obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(this IUnrealStream stream, out T value)
            where T : UObject =>
            value = ReadObject<T>(stream);

        public static void Read<T>(this IUnrealStream stream, out IUnrealDeserializableClass item)
            where T : struct, IUnrealDeserializableClass
        {
            item = new T();
            item.Deserialize(stream);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(this IUnrealStream stream, out UBulkData<T> value)
            where T : unmanaged =>
            ReadStruct(stream, out value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UObject value) => value = ReadObject<UObject>(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out string value) => value = stream.ReadString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UName value) => value = ReadNameReference(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<T>(this IUnrealStream stream, out UArray<T> array)
            where T : IUnrealSerializableClass, new() =>
            ReadArray(stream, out array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UArray<UObject> array) => ReadArray(stream, out array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UArray<UName> array) => ReadArray(stream, out array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UArray<string> array) => ReadArray(stream, out array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read<TKey, TValue>(this IUnrealStream stream, out UMap<TKey, TValue> map)
            where TKey : UName
            where TValue : UObject =>
            ReadMap(stream, out map);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out UMap<ushort, ushort> map) => ReadMap(stream, out map);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, byte[] data) => stream.Read(data, 0, data.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out byte value) => value = stream.ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out short value) => value = stream.ReadInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out ushort value) => value = stream.ReadUInt16();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out int value) => value = stream.ReadInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out uint value) => value = stream.ReadUInt32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out long value) => value = stream.ReadInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out ulong value) => value = stream.ReadUInt64();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out float value) => value = stream.ReadFloat();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(this IUnrealStream stream, out bool value) => value = ReadBool(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteString(this IUnrealStream stream, string value) => stream.UW.WriteString(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteAnsiNullString(this IUnrealStream stream, string value) => stream.UW.WriteAnsi(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnicodeNullString(this IUnrealStream stream, string value) =>
            stream.UW.WriteUnicode(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteArray<T>(this IUnrealStream stream, [CanBeNull] in UArray<T> array)
            where T : IUnrealSerializableClass
        {
            if (array == null)
            {
                WriteIndex(stream, 0);

                return;
            }

            WriteIndex(stream, array.Count);
            foreach (var element in array)
            {
                element.Serialize(stream);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteArray(this IUnrealStream stream, [CanBeNull] in UArray<int> array)
        {
            if (array == null)
            {
                WriteIndex(stream, 0);

                return;
            }

            WriteIndex(stream, array.Count);
            foreach (int element in array)
            {
                stream.Write(element);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteArray(this IUnrealStream stream, [CanBeNull] in UArray<byte> array)
        {
            if (array == null)
            {
                WriteIndex(stream, 0);

                return;
            }

            WriteIndex(stream, array.Count);
            Write(stream, array.ToArray());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteArray(this IUnrealStream stream, [CanBeNull] in UArray<string> array)
        {
            if (array == null)
            {
                WriteIndex(stream, 0);

                return;
            }

            WriteIndex(stream, array.Count);
            foreach (string element in array)
            {
                stream.Write(element);
            }
        }

        public static void Write(this IUnrealStream stream, in IUnrealSerializableClass item) => item.Serialize(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStruct<T>(this IUnrealStream stream, ref T item)
            where T : struct, IUnrealSerializableClass =>
            item.Serialize(stream);

        public static unsafe void WriteStructMarshal<T>(this IUnrealStream stream, ref T item)
            where T : unmanaged, IUnrealAtomicStruct
        {
            int structSize = sizeof(T);
            byte[] data = new byte[structSize];

            fixed (void* ptr = &item)
            {
                //Marshal.Copy((IntPtr)ptr, data, 0, structSize);
                fixed (byte* dataPtr = data)
                {
                    Unsafe.CopyBlock(dataPtr, ptr, (uint)structSize);
                }
            }

            stream.Write(data, 0, data.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteClass<T>(this IUnrealStream stream, ref T item)
            where T : class, IUnrealSerializableClass, new() =>
            item.Serialize(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, byte[] data) => stream.Write(data, 0, data.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, string value) => stream.UW.WriteString(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, UName name) => stream.WriteName(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this IUnrealStream stream, ref UBulkData<T> value)
            where T : unmanaged =>
            WriteStruct(stream, ref value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, UObject obj) => stream.WriteObject(obj);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this IUnrealStream stream, UArray<T> array)
            where T : IUnrealSerializableClass =>
            WriteArray(stream, in array);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this IUnrealStream stream, ref UArray<T> array)
            where T : IUnrealSerializableClass =>
            WriteArray(stream, in array);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static void Write<T>(this IUnrealStream stream, ref T item)
        //    where T : struct, IUnrealSerializableClass =>
        //    stream.WriteStruct(ref item);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, byte value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, short value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, ushort value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, int value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, uint value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, long value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, ulong value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, float value) => stream.UW.Write(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, bool value) => stream.UW.Write(Convert.ToInt32(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this IUnrealStream stream, byte[] buffer, int index, int count) =>
            stream.UW.Write(buffer, index, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteIndex(this IUnrealStream stream, int index) => stream.UW.WriteIndex(index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IUnrealStream Record(this IUnrealStream stream, string name, [CanBeNull] object value) =>
            stream.Record(name, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ConformRecordPosition(this IUnrealStream stream) => stream.ConformRecordPosition();
    }
}