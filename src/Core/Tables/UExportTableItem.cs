using System;
using System.ComponentModel;
using System.IO;
using UELib.Annotations;
using UELib.Branch;
using UELib.Core;

namespace UELib
{
    /// <summary>
    /// An export table entry, represents a @UObject export within a package.
    /// </summary>
    public sealed class UExportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        [Obsolete] public const int VObjectFlagsToULONG = 195;

        #region Serialized Members

        private int _ClassIndex;

        public int ClassIndex
        {
            get => _ClassIndex;
            set => _ClassIndex = value;
        }

        [CanBeNull] public UObjectTableItem Class => Owner.GetIndexTable(ClassIndex);

        private int _SuperIndex;

        public int SuperIndex
        {
            get => _SuperIndex;
            set => _SuperIndex = value;
        }

        [CanBeNull] public UObjectTableItem Super => Owner.GetIndexTable(_SuperIndex);

        private int _TemplateIndex;

        public int TemplateIndex
        {
            get => _TemplateIndex;
            set => _TemplateIndex = value;
        }

        [CanBeNull] public UObjectTableItem Template => Owner.GetIndexTable(_TemplateIndex);

        private int _ArchetypeIndex;

        public int ArchetypeIndex
        {
            get => _ArchetypeIndex;
            set => _ArchetypeIndex = value;
        }

        [CanBeNull] public UObjectTableItem Archetype => Owner.GetIndexTable(_ArchetypeIndex);

        [Obsolete("Use Class"), Browsable(false)]
        public UObjectTableItem ClassTable => Owner.GetIndexTable(_ClassIndex);

        [Obsolete] protected override int __ClassIndex => _ClassIndex;

        [Obsolete] [NotNull] protected override string __ClassName => Class?.ObjectName ?? "Class";

        [Obsolete("Use Super"), Browsable(false)]
        public UObjectTableItem SuperTable => Owner.GetIndexTable(_SuperIndex);

        [Obsolete("Use Super?.ObjectName"), Browsable(false)]
        public string SuperName
        {
            get
            {
                var table = SuperTable;
                return table != null ? table.ObjectName : string.Empty;
            }
        }

        [Obsolete("Use Archetype"), Browsable(false)]
        public UObjectTableItem ArchetypeTable => Owner.GetIndexTable(_ArchetypeIndex);

        [Obsolete("Use Archetype?.ObjectName"), Browsable(false)]
        public string ArchetypeName
        {
            get
            {
                var table = ArchetypeTable;
                return table != null ? table.ObjectName : string.Empty;
            }
        }

        /// <summary>
        /// Object flags, such as Public, Protected and Private.
        /// 32bit aligned.
        /// </summary>
        public ulong ObjectFlags;

        /// <summary>
        /// Object size in bytes.
        /// </summary>
        public int SerialSize;

        /// <summary>
        /// Object offset in bytes. Starting from the beginning of a file.
        /// </summary>
        public int SerialOffset;

        public uint ExportFlags;

        /// <summary>
        /// Always null if version is lower than <see cref="PackageObjectLegacyVersion.ArchetypeAddedToExports"/>
        /// or higher than <see cref="PackageObjectLegacyVersion.ComponentMapDeprecated"/>
        /// </summary>
        public UMap<UName, int> ComponentMap;

        //public List<int> NetObjects;

        public UGuid PackageGuid;
        public uint PackageFlags;

        public bool IsNotForServer;
        public bool IsNotForClient;
        public bool IsForcedExport;
        public bool IsNotForEditorGame;
        public bool IsAsset;

        #endregion

        // @Warning - Only supports Official builds.
        public void Serialize(IUnrealStream stream)
        {
            stream.Write(_ClassIndex);
            stream.Write(_SuperIndex);
            stream.Write(_OuterIndex);
            stream.Write(_ObjectName);
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ArchetypeAddedToExports)
            {
                _ArchetypeIndex = stream.ReadInt32();
            }

            stream.Write(stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits
                ? ObjectFlags
                : (uint)ObjectFlags);
            stream.WriteIndex(SerialSize); // Assumes SerialSize has been updated to @Object's buffer size.
            if (SerialSize > 0 || stream.Version >= (uint)PackageObjectLegacyVersion.SerialSizeConditionRemoved)
            {
                // SerialOffset has to be set and written after this object has been serialized.
                stream.WriteIndex(SerialOffset); // Assumes the same as @SerialSize comment.
            }

            // TODO: Continue.
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ArchetypeAddedToExports)
            {
                throw new NotSupportedException();
            }
        }

        public void Deserialize(IUnrealStream stream)
        {
            _ClassIndex = stream.ReadObjectIndex();
            _SuperIndex = stream.ReadObjectIndex();
            _OuterIndex = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.Version >= 132)
            {
                stream.Skip(sizeof(int));
            }
#endif
            _ObjectName = stream.ReadNameReference();
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ArchetypeAddedToExports)
            {
                _ArchetypeIndex = stream.ReadInt32();
            }
#if BATMAN
            if (stream.Package.Build == BuildGeneration.RSS && stream.LicenseeVersion > 21)
            {
                stream.Skip(sizeof(int));
            }
#endif
            _ObjectFlagsOffset = stream.Position;
#if BIOSHOCK
            // Like UE3 but without the shifting of flags
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.LicenseeVersion >= 40)
            {
                ObjectFlags = stream.ReadUInt64();
                goto streamSerialSize;
            }
#endif
            ObjectFlags = stream.ReadUInt32();
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                ObjectFlags = (ObjectFlags << 32) | stream.ReadUInt32();
            }

        streamSerialSize:
            SerialSize = stream.ReadIndex();
            if (SerialSize > 0 || stream.Version >= (uint)PackageObjectLegacyVersion.SerialSizeConditionRemoved)
            {
#if ROCKETLEAGUE
                // FIXME: Can't change SerialOffset to 64bit due UE Explorer.

                if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                    stream.LicenseeVersion >= 22)
                {
                    SerialOffset = (int)stream.ReadInt64();
                    goto streamExportFlags;
                }
#endif
                SerialOffset = stream.ReadIndex();
            }
#if BIOSHOCK
            // Overlaps with Tribes: Vengeance (130)
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.Version >= 130)
            {
                stream.Skip(sizeof(int));
            }
#endif
#if HUXLEY
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Huxley)
            {
                if (stream.LicenseeVersion >= 22)
                {
                    int SerialSize2 = stream.ReadInt32();
                }
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedComponentMapToExports &&
                stream.Version < (uint)PackageObjectLegacyVersion.ComponentMapDeprecated
#if ALPHAPROTOCOL
                && stream.Package.Build != UnrealPackage.GameBuild.BuildName.AlphaProtocol
#endif
#if TRANSFORMERS
                && (stream.Package.Build != BuildGeneration.HMS ||
                    stream.LicenseeVersion < 37)
#endif
               )
            {
                stream.Read(out int c);
                ComponentMap = new UMap<UName, int>(c);
                for (int i = 0; i < c; ++i)
                {
                    stream.Read(out UName key);
                    stream.Read(out int value);
                    ComponentMap.Add(key, value);
                }
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.ExportFlagsAddedToExports)
                return;

        streamExportFlags:
            ExportFlags = stream.ReadUInt32();
            if (stream.Version < (uint)PackageObjectLegacyVersion.NetObjectCountAdded)
                return;
#if TRANSFORMERS
            if (stream.Package.Build == BuildGeneration.HMS &&
                stream.LicenseeVersion >= 116)
            {
                byte flag = stream.ReadByte();
                if (flag == 0)
                {
                    return;
                }
            }
#endif
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock_Infinite)
            {
                uint unk = stream.ReadUInt32();
                if (unk == 1)
                {
                    uint flags = stream.ReadUInt32();
                    if ((flags & 1) != 0x0)
                    {
                        stream.ReadUInt32();
                    }

                    stream.Skip(16); // guid
                    stream.ReadUInt32(); // 01000020
                }

                return;
            }
#endif
#if MKKE
            if (stream.Package.Build != UnrealPackage.GameBuild.BuildName.MKKE)
            {
#endif
                int netObjectCount = stream.ReadInt32();
                stream.Skip(netObjectCount * 4);
#if MKKE
            }
#endif
            stream.Skip(16); // Package guid
#if HUXLEY
            if (stream.Version >= 475)
#else
            if (stream.Version > 486) // 475?  486(> Stargate Worlds)
#endif
            {
                stream.Skip(4); // Package flags
            }
        }

        [Obsolete] private long _ObjectFlagsOffset;

        /// <summary>
        /// Updates the ObjectFlags inside the Stream to the current set ObjectFlags of this Table
        /// </summary>
        [Obsolete]
        public void WriteObjectFlags()
        {
            Owner.Stream.Seek(_ObjectFlagsOffset, SeekOrigin.Begin);
            Owner.Stream.Writer.Write((uint)ObjectFlags);
        }

        public override string GetReferencePath()
        {
            return Class != null
                ? $"{Class.ObjectName}'{GetPath()}'"
                : $"Class'{GetPath()}'";
        }

        public override string ToString()
        {
            return $"{ObjectName}({Index + 1})";
        }

        [Obsolete("Use ToString()")]
        public string ToString(bool v)
        {
            return ToString();
        }

        public static explicit operator int(UExportTableItem item)
        {
            return (item.Index + 1);
        }
    }
}
