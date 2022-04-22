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

        private const int QWORDVersion = 141;

        public void Deserialize(IUnrealStream stream)
        {
            Name = DeserializeName(stream);
            Debug.Assert(Name.Length <= 1024, "Maximum name length exceeded! Possible corrupt or unsupported package.");
            Flags = stream.Version >= QWORDVersion
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
#if DCUO
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.DCUO)
            {
                // FIXME: DCUO doesn't null terminate name entry strings
            }
#endif
            return stream.ReadText();
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteString(Name);

            if (stream.Version < QWORDVersion)
                // Writing UINT
                stream.UW.Write((uint)Flags);
            else
                // Writing ULONG
                stream.UW.Write(Flags);
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