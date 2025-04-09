using System;
using System.IO;
using UELib;
using UELib.Branch;
using UELib.Core;
using UELib.Decoding;

namespace Eliot.UELib.Benchmark
{
    public class UnrealTestArchive : IUnrealArchive
    {
        public UnrealTestArchive(UnrealPackage package, uint version, uint licenseeVersion = 0, uint ue4Version = 0, bool bigEndianCode = false)
        {
            Package = package;
            Version = version;
            LicenseeVersion = licenseeVersion;
            UE4Version = ue4Version;
            BigEndianCode = bigEndianCode;
        }

        public UnrealPackage Package { get; }
        public uint Version { get; }
        public uint LicenseeVersion { get; }
        public uint UE4Version { get; }
        public bool BigEndianCode { get; }
    }

    /// Hackish workaround for the issue with UPackageStream requiring a file and path, so that we can perform stream tests without a package.
    public class UnrealTestStream : UnrealReader, IUnrealStream
    {
        public uint Version => Archive.Version;
        public uint LicenseeVersion => Archive.LicenseeVersion;
        public uint UE4Version => Archive.UE4Version;
        public bool BigEndianCode => Archive.BigEndianCode;

        public UnrealPackage Package => Archive.Package;
        public UnrealReader UR { get; }
        public UnrealWriter UW { get; }

        public IBufferDecoder Decoder { get; set; }
        public IPackageSerializer Serializer { get; set; }

        public UnrealTestStream(IUnrealArchive archive, Stream baseStream) : base(archive, baseStream)
        {
            UR = new UnrealReader(archive, baseStream);
            UW = new UnrealWriter(archive, baseStream);
        }

        public int ReadObjectIndex()
        {
            throw new NotImplementedException();
        }

        public UObject ParseObject(int index)
        {
            throw new NotImplementedException();
        }

        public new int ReadNameIndex()
        {
            throw new NotImplementedException();
        }

        public new int ReadNameIndex(out int num)
        {
            throw new NotImplementedException();
        }

        public string ParseName(int index)
        {
            throw new NotImplementedException();
        }

        public void WriteName(in UName value)
        {
            throw new NotImplementedException();
        }

        public void Skip(int bytes)
        {
            Position += bytes;
        }

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public long Length => BaseStream.Length;

        public long AbsolutePosition
        {
            get => Position;
            set => Position = value;
        }

        public T ReadObject<T>() where T : UObject
        {
            throw new NotImplementedException();
        }

        public void WriteObject<T>(T value) where T : UObject
        {
            throw new NotImplementedException();
        }

        public new UName ReadName()
        {
            return base.ReadName();
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public IUnrealStream Record(string name, object? value)
        {
            return this;
        }

        public void ConformRecordPosition()
        {
        }
    }
}
