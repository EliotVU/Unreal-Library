using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UELib.Annotations;
using UELib.Core;

namespace UELib
{
    public interface IUnrealStream : IDisposable
    {
        UnrealPackage Package { get; }
        UnrealReader UR { get; }
        UnrealWriter UW { get; }

        /// <summary>
        /// The version of the package this stream is working for.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Reads the next bytes as characters either in ASCII or Unicode with prefix string length.
        /// </summary>
        /// <returns></returns>
        string ReadText();

        string ReadASCIIString();

        /// <summary>
        /// Reads the next bytes as a index to an Object.
        /// </summary>
        /// <returns></returns>
        int ReadObjectIndex();

        UObject ReadObject();

        [PublicAPI("UE Explorer")]
        UObject ParseObject(int index);

        /// <summary>
        /// Reads the next bytes as a index to an NameTable.
        /// </summary>
        /// <returns></returns>
        int ReadNameIndex();

        UName ReadNameReference();

        [PublicAPI("UE Explorer")]
        string ParseName(int index);

        /// <summary>
        /// Reads the next bytes as a index to an NameTable.
        /// </summary>
        /// <returns></returns>
        int ReadNameIndex(out int num);

        /// <summary>
        /// Reads the next bytes as a index.
        /// </summary>
        /// <returns></returns>
        int ReadIndex();

        /// <summary>
        /// Reads the next 4 bytes as a float converted to an Unreal float format.
        /// </summary>
        /// <returns></returns>
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

    public interface IUnrealArchive
    {
        UnrealPackage Package { get; }
        uint Version { get; }

        bool BigEndianCode { get; }
        long LastPosition { get; set; }
    }

    /// <summary>
    /// Wrapper for Streams with specific functions for deserializing UELib.UnrealPackage.
    /// </summary>
    public class UnrealReader : BinaryReader
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
            "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        private IUnrealArchive _Archive;

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
            int unfixedSize;
            int size = (unfixedSize =
#if BIOSHOCK
                // In Bioshock packages always give a positive Size despite being Unicode, so we reverse this.
                _Archive.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock
                    ? -ReadIndex()
                    :
#endif
                    ReadIndex()) < 0
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
#if TRANSFORMERS
                // No null-termination in Transformer games.
                if (_Archive.Package.Build == UnrealPackage.GameBuild.BuildName.Transformers &&
                    _Archive.Package.LicenseeVersion >= 181)
                {
                    return Encoding.ASCII.GetString(chars, 0, chars.Length);
                }
#endif
                return Encoding.ASCII.GetString(chars, 0, chars.Length - 1);
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
#if TRANSFORMERS
                // No null-termination in Transformer games.
                if (_Archive.Package.Build == UnrealPackage.GameBuild.BuildName.Transformers &&
                    _Archive.Package.LicenseeVersion >= 181)
                {
                    return new string(chars);
                }
#endif
                // Strip off the null
                return new string(chars, 0, chars.Length - 1);
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
                || _Archive.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock
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
                || _Archive.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock
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

        public Guid ReadGuid()
        {
            // A, B, C, D
            var guidBuffer = new byte[16];
            Read(guidBuffer, 0, 16);
            var g = new Guid(guidBuffer);
            return g;
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
        public UnrealPackage Package { get; protected set; }

        protected UPackageFileStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode,
            access, share)
        {
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            long p = Position;
            int length = base.Read(buffer, index, count);
            Package.Decoder?.DecodeRead(p, buffer, index, count);
            return length;
        }

        public override int ReadByte()
        {
            if (Package.Decoder == null)
                return base.ReadByte();

            long p = Position;
            var buffer = new[] { (byte)base.ReadByte() };
            Package.Decoder?.DecodeRead(p, buffer, 0, 1);
            return buffer[0];
        }
    }

    public class UPackageStream : UPackageFileStream, IUnrealStream, IUnrealArchive
    {
        public uint Version => Package?.Version ?? 0;

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

            package.Decoder?.PreDecode(this);

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

        /// <summary>
        /// Called as soon the build for @Package is detected.
        /// </summary>
        /// <param name="build"></param>
        public void BuildDetected(UnrealPackage.GameBuild build)
        {
            Package.Decoder?.DecodeBuild(this, build);
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

        /// <summary>
        /// Reads a float converted to a unreal float string format.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read float converted to a unreal float string format</returns>
        public float ReadFloat()
        {
            return UR.ReadSingle();
        }

        #region Macros

        /// <summary>
        /// Reads a Unsigned Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ushort</returns>
        public ushort ReadUShort()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read uint</returns>
        public uint ReadUInt32()
        {
            return UR.ReadUInt32();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ulong</returns>
        public ulong ReadUInt64()
        {
            return UR.ReadUInt64();
        }

        /// <summary>
        /// Reads a Signed Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public short ReadInt16()
        {
            return UR.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Signed Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public int ReadInt32()
        {
            return UR.ReadInt32();
        }

        /// <summary>
        /// Reads a Signed Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read long</returns>
        public long ReadInt64()
        {
            return UR.ReadInt64();
        }

        /// <summary>
        /// Reads a Name/String with no known size, expecting that the next bytes are the size of the string.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string without the end \0 char</returns>
        public string ReadText()
        {
            return UR.ReadText();
        }

        /// <summary>
        /// Reads a string with no known length, ends when the first \0 char is reached.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string</returns>
        public string ReadASCIIString()
        {
            return UR.ReadAnsi();
        }

        /// <summary>
        /// Unreal Engine 1 and 2
        /// Compact indices exist so that small numbers can be stored efficiently.
        /// An index named "Index" is stored as a series of 1-5 consecutive bytes.
        ///
        /// Unreal Engine 3
        /// The index is based on a Int32
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read index</returns>
        public int ReadIndex()
        {
            return UR.ReadIndex();
        }

        /// <summary>
        /// Same as ReadIndex, just a placeholder for UE3 compatibly.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex()
        {
            return (int)UR.ReadNameIndex();
        }

        public UName ReadNameReference()
        {
            return new UName(this);
        }

        public string ParseName(int index)
        {
            return Package.GetIndexName(index);
        }

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex(out int num)
        {
            return UR.ReadNameIndex(out num);
        }

        /// <summary>
        /// Reads a Guid of type A-B-C-D.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read guid</returns>
        public Guid ReadGuid()
        {
            return UR.ReadGuid();
        }

        #endregion

        /// <summary>
        /// Skip a amount of bytes.
        /// </summary>
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
        public string Name => Package.Stream.Name;

        /// <summary>
        /// The package I am streaming for.
        /// </summary>
        public UnrealPackage Package { get; }

        /// <inheritdoc/>
        public uint Version => Package?.Version ?? 0;

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

        /// <summary>
        /// Reads a float
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>The read float</returns>
        public float ReadFloat()
        {
            return UR.ReadSingle();
        }

        #region Macros

        // Macros
        /// <summary>
        /// overidden: Reads a byte
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read byte</returns>
        public new byte ReadByte()
        {
#if BINARYMETADATA
            LastPosition = Position;
#endif
            return UR.ReadByte();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ushort</returns>
        public ushort ReadUShort()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read uint</returns>
        public uint ReadUInt32()
        {
            return UR.ReadUInt32();
        }

        /// <summary>
        /// Reads a Unsigned Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read ulong</returns>
        public ulong ReadUInt64()
        {
            return UR.ReadUInt64();
        }

        /// <summary>
        /// Reads a Signed Integer of 16bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public short ReadInt16()
        {
            return UR.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            return UR.ReadUInt16();
        }

        /// <summary>
        /// Reads a Signed Integer of 32bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read int</returns>
        public int ReadInt32()
        {
            return UR.ReadInt32();
        }

        /// <summary>
        /// Reads a Signed Integer of 64bits
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read long</returns>
        public long ReadInt64()
        {
            return UR.ReadInt64();
        }

        /// <summary>
        /// Reads a Name/String with no known size, expecting that the next bytes are the size of the string.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string without the end \0 char</returns>
        public string ReadText()
        {
            return UR.ReadText();
        }

        /// <summary>
        /// Reads a string with no known length, ends when the first \0 char is reached.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read string</returns>
        public string ReadASCIIString()
        {
            return UR.ReadAnsi();
        }

        /// <summary>
        /// Unreal Engine 1 and 2
        /// Compact indices exist so that small numbers can be stored efficiently.
        /// An index named "Index" is stored as a series of 1-5 consecutive bytes.
        ///
        /// Unreal Engine 3
        /// The index is based on a Int32
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read index</returns>
        public int ReadIndex()
        {
            return UR.ReadIndex();
        }

        /// <summary>
        /// Same as ReadIndex, just a placeholder for UE3 compatibly.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex()
        {
            return (int)UR.ReadNameIndex();
        }

        public UName ReadNameReference()
        {
            return new UName(this);
        }

        public string ParseName(int index)
        {
            return Package.GetIndexName(index);
        }

        /// <summary>
        /// Same as ReadIndex except this one handles differently if the version is something above UE3.
        /// </summary>
        /// <returns>The read 64bit index casted to a 32bit index.</returns>
        public int ReadNameIndex(out int num)
        {
            return UR.ReadNameIndex(out num);
        }

        /// <summary>
        /// Reads a Guid of type A-B-C-D.
        ///
        /// Advances the position.
        /// </summary>
        /// <returns>the read guid</returns>
        public Guid ReadGuid()
        {
            return UR.ReadGuid();
        }

        #endregion

        /// <summary>
        /// Skip a amount of bytes.
        /// </summary>
        /// <param name="bytes">The amount of bytes to skip.</param>
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

            // Closed by the above, but this is just for ensurance.
            Dispose();
        }
    }

    /// <summary>
    /// Methods that shouldn't be duplicated between UObjectStream and UPackageStream.
    /// </summary>
    public static class UnrealStreamImplementations
    {
        public static string ReadName(this IUnrealStream stream)
        {
            int num;
            string name = stream.Package.GetIndexName(stream.ReadNameIndex(out num));
            if (num > UName.Numeric) name += "_" + num;

            return name;
        }

        public static void Write(this IUnrealStream stream, UName name)
        {
            name.Serialize(stream);
        }

        public static void Write(this IUnrealStream stream, UObjectTableItem obj)
        {
            stream.UW.WriteIndex(obj != null ? (int)obj.Object : 0);
        }

        public static void Write(this IUnrealStream stream, UObject obj)
        {
            stream.UW.WriteIndex(obj != null ? (int)obj : 0);
        }

        public static void Write(this IUnrealStream stream, short number)
        {
            stream.UW.Write(number);
        }

        public static void Write(this IUnrealStream stream, ushort number)
        {
            stream.UW.Write(number);
        }

        public static void Write(this IUnrealStream stream, int number)
        {
            stream.UW.Write(number);
        }

        public static void Write(this IUnrealStream stream, uint number)
        {
            stream.UW.Write(number);
        }

        public static void Write(this IUnrealStream stream, long number)
        {
            stream.UW.Write(number);
        }

        public static void Write(this IUnrealStream stream, ulong number)
        {
            stream.UW.Write(number);
        }

        public static void Write(this IUnrealStream stream, byte[] buffer, int index, int count)
        {
            stream.UW.Write(buffer, index, count);
        }

        public static void WriteIndex(this IUnrealStream stream, int index)
        {
            stream.UW.WriteIndex(index);
        }

        // Don't overload Write() because this string writes using the Unreal instead of the .NET format.
        public static void WriteString(this IUnrealStream stream, string s)
        {
            stream.UW.WriteString(s);
        }
    }
}