using System;
using System.IO;
using UELib.Core;
using UELib.Decoding;

namespace UELib.Branch.UE3.R6.PackageFormat
{
    public class UnrealStaticDataPackageStream : IUnrealStream
    {
        private readonly Stream _BaseStream;

        public UnrealStaticDataPackageStream(Stream baseStream)
        {
            _BaseStream = baseStream;

            Package = new UnrealPackage(new UnrealPackage.GameBuild(UnrealPackage.GameBuild.BuildName.R6Vegas,
                BuildGeneration.UE3, typeof(EngineBranchKeller)));
            Version = (uint)PackageObjectLegacyVersion.UE3;

            if (baseStream.CanRead)
            {
                UR = new UnrealReader(this, baseStream);
            }

            if (baseStream.CanWrite)
            {
                UW = new UnrealWriter(this, baseStream);
            }
        }

        public UnrealPackage Package { get; }
        public UnrealReader UR { get; }
        public UnrealWriter UW { get; }
        public IBufferDecoder Decoder { get; set; }
        public IPackageSerializer Serializer { get; set; }

        public long Position
        {
            get => _BaseStream.Position;
            set => _BaseStream.Position = value;
        }

        public long AbsolutePosition { get; set; }

        public int ReadObjectIndex() => throw new NotImplementedException();
        public UObject ParseObject(int index) => throw new NotImplementedException();
        public int ReadNameIndex() => throw new NotImplementedException();
        public int ReadNameIndex(out int num) => throw new NotImplementedException();
        public string ParseName(int index) => throw new NotImplementedException();

        public void Skip(int bytes) => _BaseStream.Position += bytes;
        public int Read(byte[] buffer, int index, int count) => _BaseStream.Read(buffer, index, count);
        public long Seek(long offset, SeekOrigin origin) => _BaseStream.Seek(offset, SeekOrigin.Begin);

        public uint Version { get; }
        public uint LicenseeVersion { get; }
        public uint UE4Version { get; }
        public bool BigEndianCode { get; }

        public void Dispose() => _BaseStream.Dispose();
    }
}
