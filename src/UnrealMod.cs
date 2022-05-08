using System;
using System.Diagnostics.CodeAnalysis;
using UELib.Core;

namespace UELib
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "NotAccessedField.Global")]
    public class UnrealMod : IUnrealDeserializableClass
    {
        public const uint Signature = 0x9FE3C5A3;

        public struct FileSummary : IUnrealDeserializableClass
        {
            public int FileTableOffset;
            public int FileSize;
            public int Version;
            public int CRC32;

            public void Deserialize(IUnrealStream stream)
            {
                FileTableOffset = stream.ReadInt32();
                FileSize = stream.ReadInt32();
                Version = stream.ReadInt32();
                CRC32 = stream.ReadInt32();
            }
        }

        public FileSummary Summary;

        // Table values are not initialized!

        public struct ModFile : IUnrealSerializableClass
        {
            public string FileName;
            public int SerialOffset;
            public int SerialSize;
            public uint FileFlags;

            /*[Flags]
            public enum Flags : uint
            {
                NoSystem = 0x03
            }*/

            public void Serialize(IUnrealStream stream)
            {
                throw new NotImplementedException();
            }

            public void Deserialize(IUnrealStream stream)
            {
                FileName = stream.ReadText();
                SerialOffset = stream.ReadIndex();
                SerialSize = stream.ReadIndex();
                FileFlags = stream.ReadUInt32();
            }
        }

        public UArray<ModFile> FileMap;

        public void Deserialize(IUnrealStream stream)
        {
            if (stream.ReadUInt32() != Signature)
            {
                throw new System.IO.FileLoadException(stream + " isn't a UnrealMod file!");
            }

            Summary = new FileSummary();
            Summary.Deserialize(stream);

            stream.Seek(Summary.FileTableOffset, System.IO.SeekOrigin.Begin);
            stream.ReadArray(out FileMap);
        }
    }
}