using System;
using System.ComponentModel;
using System.IO;

namespace UELib
{
    /// <summary>
    /// An export table entry, representing a @UObject in a package.
    /// </summary>
    public sealed class UExportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        private const int VArchetype = 220;
        public const int VObjectFlagsToULONG = 195;

        private const int VSerialSizeConditionless = 249;

        // FIXME: Version?
        public const int VNetObjects = 322;

        #region Serialized Members
        
        private int _ClassIndex;
        public int ClassIndex
        {
            get => _ClassIndex;
            set => _ClassIndex = value;
        }
        public UObjectTableItem Class => Owner.GetIndexTable(ClassIndex);

        public int _SuperIndex;
        public int SuperIndex
        {
            get => _SuperIndex;
            set => _SuperIndex = value;
        }
        public UObjectTableItem Super => Owner.GetIndexTable(_SuperIndex);

        public int _TemplateIndex;
        public int TemplateIndex
        {
            get => _TemplateIndex;
            set => _TemplateIndex = value;
        }
        public UObjectTableItem Template => Owner.GetIndexTable(_TemplateIndex);

        public int _ArchetypeIndex;
        public int ArchetypeIndex
        {
            get => _ArchetypeIndex;
            set => _ArchetypeIndex = value;
        }
        public UObjectTableItem Archetype => Owner.GetIndexTable(_ArchetypeIndex);

        [Obsolete, Browsable(false)] public UObjectTableItem SuperTable => Owner.GetIndexTable(_SuperIndex);
        [Obsolete, Browsable(false)]
        public string SuperName
        {
            get
            {
                var table = SuperTable;
                return table != null ? table.ObjectName : string.Empty;
            }
        }

        [Obsolete, Browsable(false)] public UObjectTableItem ArchetypeTable => Owner.GetIndexTable(_ArchetypeIndex);
        [Obsolete, Browsable(false)]
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
        //public Dictionary<int, int> Components;
        //public List<int> NetObjects;

        public Guid PackageGuid;
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
            stream.Write(OuterIndex);
            stream.Write(ObjectName);
            if (stream.Version >= VArchetype)
            {
                _ArchetypeIndex = stream.ReadInt32();
            }
            stream.Write(stream.Version >= VObjectFlagsToULONG
                ? ObjectFlags
                : (uint)ObjectFlags);
            stream.WriteIndex(SerialSize); // Assumes SerialSize has been updated to @Object's buffer size.
            if (SerialSize > 0 || stream.Version >= VSerialSizeConditionless)
            {
                // SerialOffset has to be set and written after this object has been serialized.
                stream.WriteIndex(SerialOffset); // Assumes the same as @SerialSize comment.
            }

            // TODO: Continue.
            if (stream.Version >= 220)
            {
                throw new NotSupportedException();
            }
        }

        public void Deserialize(IUnrealStream stream)
        {
            _ClassIndex = stream.ReadObjectIndex();
            _SuperIndex = stream.ReadObjectIndex();
            OuterIndex = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.Version >= 132)
            {
                stream.Skip(sizeof(int));
            }
#endif
            ObjectName = stream.ReadNameReference();
            if (stream.Version >= VArchetype)
            {
                _ArchetypeIndex = stream.ReadInt32();
            }
#if BATMAN
            if (stream.Package.Build == BuildGeneration.RSS)
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
            if (stream.Version >= VObjectFlagsToULONG)
            {
                ObjectFlags = (ObjectFlags << 32) | stream.ReadUInt32();
            }

        streamSerialSize:
            SerialSize = stream.ReadIndex();
            if (SerialSize > 0 || stream.Version >= VSerialSizeConditionless)
            {
#if ROCKETLEAGUE
                // FIXME: Can't change SerialOffset to 64bit due UE Explorer.

                if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                    stream.LicenseeVersion >= 22)
                {
                    SerialOffset = stream.ReadIndex();
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
            if (stream.Version < 220)
                return;

            if (stream.Version < 543
#if ALPHAPROTOCOL
                && stream.Package.Build != UnrealPackage.GameBuild.BuildName.AlphaProtocol
#endif
#if TRANSFORMERS
                && (stream.Package.Build != BuildGeneration.HMS ||
                    stream.LicenseeVersion < 37)
#endif
               )
            {
                // NameToObject
                int componentMapCount = stream.ReadInt32();
                stream.Skip(componentMapCount * 12);
            }

            if (stream.Version < 247)
                return;

        streamExportFlags:
            ExportFlags = stream.ReadUInt32();
            if (stream.Version < VNetObjects)
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
                // Array of objects
                int netObjectCount = stream.ReadInt32();
                stream.Skip(netObjectCount * 4);
#if MKKE
            }
#endif
            stream.Skip(16); // Package guid
            if (stream.Version > 486) // 475?  486(> Stargate Worlds)
            {
                stream.Skip(4); // Package flags
            }
        }
        
        [Obsolete]
        private long _ObjectFlagsOffset;

        /// <summary>
        /// Updates the ObjectFlags inside the Stream to the current set ObjectFlags of this Table
        /// </summary>
        [Obsolete]
        public void WriteObjectFlags()
        {
            Owner.Stream.Seek(_ObjectFlagsOffset, SeekOrigin.Begin);
            Owner.Stream.UW.Write((uint)ObjectFlags);
        }

        public override string ToString()
        {
            return $"{ObjectName}({Index}{1})";
        }

        public static explicit operator int(UExportTableItem item)
        {
            return item.Index;
        }
    }
}