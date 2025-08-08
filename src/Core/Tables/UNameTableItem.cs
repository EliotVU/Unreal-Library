using System.Diagnostics;
using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.Core;

namespace UELib
{
    /// <summary>
    /// A unique package name entry.
    /// </summary>
    public sealed class UNameTableItem : UTableItem, IUnrealSerializableClass
    {
        internal IndexName? IndexName;

        /// <summary>
        /// The case-preserved name of this package name entry.
        /// </summary>
        public string Name
        {
            get => _Name;
            set
            {
                _Name = value;
                IndexName = IndexName.FromText(value);
            }
        }

        internal string _Name;

        /// <summary>
        /// The flags of this package name entry, see <see cref="ObjectFlagsLO"/>
        /// </summary>
        [BuildGenerationRange(BuildGeneration.UE1, BuildGeneration.UE3)]
        public ulong Flags
        {
            get => _Flags;
            set => _Flags = value;
        }

        internal ulong _Flags;

        [BuildGeneration(BuildGeneration.UE4)]
        public ushort NonCasePreservingHash;

        [BuildGeneration(BuildGeneration.UE4)]
        public ushort CasePreservingHash;

        public UNameTableItem()
        {
        }

        public UNameTableItem(string text)
        {
            Name = text;
            IndexName = IndexName.FromText(text);
        }

        public UNameTableItem(int index)
        {
            var indexName = IndexName.FromIndex(index);
            Contract.Assert(indexName != null, "Cannot construct from an unregistered index.");

            Name = IndexName.Text;
            IndexName = indexName;
        }

        public UNameTableItem(UName name)
        {
            Name = name.Text; // This might allocate a new string.
            IndexName = IndexName.FromIndex(name.Index);
        }

        /// <summary>
        /// Deserializes the name from a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Deserialize(IUnrealStream, UNameTableItem)"/>
        /// </summary>
        /// <param name="stream">The input stream</param>
        public void Deserialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.Release64)
            {
                stream.Read(out _Name);
            }
            else
            {
                _Name = stream.ReadAnsiNullString();
            }

            Debug.Assert(_Name.Length <= 1024,
                "Maximum name length exceeded! Possible corrupt or unsupported package.");
#if BIOSHOCK
            if (stream.Build == UnrealPackage.GameBuild.BuildName.BioShock)
            {
                stream.Read(out _Flags);

                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                stream.Read(out _Flags);
            }
            else
            {
                stream.Read(out uint flags);
                _Flags = flags;
            }
        }

        /// <summary>
        /// Serializes the name to a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Serialize(IUnrealStream, UNameTableItem)"/>
        /// </summary>
        /// <param name="stream">The output stream</param>
        public void Serialize(IUnrealStream stream)
        {
            Debug.Assert(_Name.Length <= 1024,
                         "Maximum name length exceeded! Possible corrupt or unsupported package.");

            if (stream.Version >= (uint)PackageObjectLegacyVersion.Release64)
            {
                stream.Write(_Name);
            }
            else
            {
                stream.WriteAnsiNullString(_Name);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                stream.Write(_Flags);
            }
            else
            {
                stream.Write((uint)_Flags);
            }
        }

        public override string ToString()
        {
            return _Name;
        }

        public static implicit operator string(UNameTableItem a)
        {
            return a._Name;
        }

        public static implicit operator int(UNameTableItem a)
        {
            return a.Index;
        }

        public static explicit operator IndexName(UNameTableItem a)
        {
            return a.IndexName!;
        }
    }
}
