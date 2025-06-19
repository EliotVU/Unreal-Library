using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace UELib.Core
{
    [UnrealRegisterClass]
    public partial class UTextBuffer : UObject
    {
        #region Serialized Members

        public uint Top;
        public uint Pos;

        public string ScriptText;

        #endregion

        #region Constructors

        public UTextBuffer()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            Top = _Buffer.ReadUInt32();
            Record(nameof(Top), Top);
            Pos = _Buffer.ReadUInt32();
            Record(nameof(Pos), Pos);
#if UNDYING
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                _Buffer.Version >= 85)
            {
                int uncompressedDataSize = _Buffer.ReadIndex();
                Record(nameof(uncompressedDataSize), uncompressedDataSize);

                int compressedDataSize = _Buffer.ReadIndex();
                Record(nameof(compressedDataSize), compressedDataSize);
                if (compressedDataSize > 0)
                {
                    var compressedData = new byte[compressedDataSize];
                    _Buffer.Read(compressedData, 0, compressedDataSize);
                    Record(nameof(compressedData), compressedData);

                    byte[] uncompressedData = new byte[uncompressedDataSize];
#if NET6_0_OR_GREATER
                    using var s = new ZLibStream(new MemoryStream(compressedData), CompressionMode.Decompress);
                    s.ReadExactly(uncompressedData, 0, uncompressedDataSize);

                    ScriptText = UnrealEncoding.ANSI.GetString(uncompressedData);
                    return;
#endif
                }

                ScriptText = "Text data is compressed";
                return;
            }
#endif
            ScriptText = _Buffer.ReadString();
            Record(nameof(ScriptText), "...");
        }

        #endregion
    }
}
