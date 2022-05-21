using System.Diagnostics;
using System.Runtime.CompilerServices;
using UELib.Decoding;

namespace UELib
{
    /// <summary>
    /// A names table entry, representing all unique names within a package.
    /// </summary>
    public sealed class UNameTableItem : UTableItem, IUnrealSerializableClass
    {
        #region Serialized Members

        /// <summary>
        /// Object Name
        /// </summary>
        public string Name = string.Empty;

        /// <summary>
        /// Object Flags, such as LoadForEdit, LoadForServer, LoadForClient
        /// </summary>
        /// <value>
        /// 32bit in UE2
        /// 64bit in UE3
        /// </value>
        public ulong Flags;

        #endregion

        public void Deserialize(IUnrealStream stream)
        {
            Name = DeserializeName(stream);
            Debug.Assert(Name.Length <= 1024, "Maximum name length exceeded! Possible corrupt or unsupported package.");
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock)
            {
                Flags = stream.ReadUInt64();
                return;
            }
#endif
            Flags = stream.Version >= UExportTableItem.VObjectFlagsToULONG
                ? stream.ReadUInt64()
                : stream.ReadUInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string DeserializeName(IUnrealStream stream)
        {
#if UE1
            // Very old packages use a simple Ansi encoding.
            if (stream.Version < UnrealPackage.VSIZEPREFIXDEPRECATED) return stream.ReadASCIIString();
#endif
#if AA2
            // Names are not encrypted in AAA/AAO 2.6 (LicenseeVersion 32)
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.AA2
                && stream.Package.LicenseeVersion >= 33
                && stream.Package.Decoder is CryptoDecoderAA2)
            {
                // Thanks to @gildor2, decryption code transpiled from https://github.com/gildor2/UEViewer, 
                int length = stream.ReadIndex();
                Debug.Assert(length < 0);
                int size = -length;

                const byte n = 5;
                byte shift = n;
                var buffer = new char[size];
                for (var i = 0; i < size; i++)
                {
                    ushort c = stream.ReadUInt16();
                    ushort c2 = CryptoCore.RotateRight(c, shift);
                    Debug.Assert(c2 < byte.MaxValue);
                    buffer[i] = (char)(byte)c2;
                    shift = (byte)((c - n) & 0x0F);
                }

                var str = new string(buffer, 0, buffer.Length - 1);
                // Part of name ?
                int number = stream.ReadIndex();
                //Debug.Assert(number == 0, "Unknown value");
                return str;
            }
#endif
            return stream.ReadText();
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(Name);
            if (stream.Version < UExportTableItem.VObjectFlagsToULONG)
                // Writing UINT
                stream.Write((uint)Flags);
            else
                // Writing ULONG
                stream.Write(Flags);
        }

        public override string ToString()
        {
            return Name;
        }

        public static implicit operator string(UNameTableItem a)
        {
            return a.Name;
        }

        public static implicit operator int(UNameTableItem a)
        {
            return a.Index;
        }
    }
}