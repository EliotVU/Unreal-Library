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
        /// An unique name in a package.
        /// </summary>
        public string Name = string.Empty;

        /// <summary>
        /// Object Flags, such as LoadForEdit, LoadForServer, LoadForClient
        /// 32bit in UE2
        /// 64bit in UE3
        /// </summary>
        public ulong Flags;

        public ushort NonCasePreservingHash;
        public ushort CasePreservingHash;

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