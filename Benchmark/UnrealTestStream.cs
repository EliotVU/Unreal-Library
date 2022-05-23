using System;
using System.IO;
using UELib;
using UELib.Core;

namespace Eliot.UELib.Benchmark
{
    /// Hackish workaround for the issue with UPackageStream requiring a file and path, so that we can perform stream tests without a package.
    public class UnrealTestStream : UnrealReader, IUnrealStream
    {
        public UnrealTestStream(IUnrealArchive archive, Stream baseStream) : base(archive, baseStream)
        {
            _Archive = archive;
        }

        public UnrealPackage Package => _Archive.Package;
        public UnrealReader UR => this;
        public UnrealWriter UW { get; }
        public uint Version => _Archive.Version;

        public string ReadASCIIString()
        {
            throw new NotImplementedException();
        }

        public int ReadObjectIndex()
        {
            throw new NotImplementedException();
        }

        public UObject ReadObject()
        {
            throw new NotImplementedException();
        }

        public UObject ParseObject(int index)
        {
            throw new NotImplementedException();
        }

        public int ReadNameIndex()
        {
            throw new NotImplementedException();
        }

        public int ReadNameIndex(out int num)
        {
            throw new NotImplementedException();
        }

        public string ParseName(int index)
        {
            throw new NotImplementedException();
        }

        public float ReadFloat()
        {
            throw new NotImplementedException();
        }

        public void Skip(int bytes)
        {
            throw new NotImplementedException();
        }

        public long Length => BaseStream.Length;
        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public long LastPosition
        {
            get => _Archive.LastPosition;
            set => _Archive.LastPosition = value;
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }
    }
}