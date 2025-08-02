using UELib.ObjectModel.Annotations;
#if NET6_0_OR_GREATER
using System.IO;
using System.IO.Compression;
#endif

namespace UELib.Core
{
    [UnrealRegisterClass]
    public partial class UTextBuffer : UObject
    {
        #region Serialized Members

        [StreamRecord]
        public uint Top { get; private set; }

        [StreamRecord]
        public uint Pos { get; private set; }

        [StreamRecord]
        public string ScriptText { get; set; }

        #endregion

        public UTextBuffer()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Top = stream.ReadUInt32();
            stream.Record(nameof(Top), Top);
            Pos = stream.ReadUInt32();
            stream.Record(nameof(Pos), Pos);
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                stream.Version >= 85)
            {
                int uncompressedDataSize = stream.ReadIndex();
                stream.Record(nameof(uncompressedDataSize), uncompressedDataSize);

                int compressedDataSize = stream.ReadIndex();
                stream.Record(nameof(compressedDataSize), compressedDataSize);
                if (compressedDataSize > 0)
                {
                    var compressedData = new byte[compressedDataSize];
                    stream.Read(compressedData, 0, compressedDataSize);
                    stream.Record(nameof(compressedData), compressedData);

                    var uncompressedData = new byte[uncompressedDataSize];
#if NET6_0_OR_GREATER
                    using var zlib = new ZLibStream(new MemoryStream(compressedData), CompressionMode.Decompress);
                    zlib.ReadExactly(uncompressedData, 0, uncompressedDataSize);

                    ScriptText = UnrealEncoding.ANSI.GetString(uncompressedData);

                    return;
#endif
                }

                ScriptText = "Text data is compressed";

                return;
            }
#endif
            ScriptText = stream.ReadString();
            stream.Record(nameof(ScriptText), "...");
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(Top);
            stream.Write(Pos);
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                stream.Version >= 85)
            {
                if (!string.IsNullOrEmpty(ScriptText))
                {
                    byte[] uncompressedData = UnrealEncoding.ANSI.GetBytes(ScriptText);
#if NET6_0_OR_GREATER
                    using var memoryStream = new MemoryStream();
                    using var zlib = new ZLibStream(memoryStream, CompressionMode.Compress);
                    zlib.Write(uncompressedData, 0, uncompressedData.Length);
                    byte[] compressedData = memoryStream.ToArray();
                    stream.WriteIndex(uncompressedData.Length);
                    stream.WriteIndex(compressedData.Length);
                    stream.Write(compressedData, 0, compressedData.Length);
#else
                    stream.WriteIndex(0);
                    stream.WriteIndex(0);
#endif
                }
                else
                {
                    stream.WriteIndex(0);
                    stream.WriteIndex(0);
                }

                return;
            }
#endif
            stream.WriteString(ScriptText);
        }
    }
}
