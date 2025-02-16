using System;
using System.ComponentModel;
using System.IO;
using UELib.Annotations;
using UELib.Branch;
using UELib.Core;
using UELib.Flags;
using UELib.Services;

namespace UELib
{
    /// <summary>
    /// An exported resource to assist with the re-construction of any <see cref="UObject"/>
    /// It describes the name of the object and the objects it's dependent on.
    /// </summary>
    public sealed class UExportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
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

        [BuildGeneration(BuildGeneration.UE4)]
        public int TemplateIndex
        {
            get => _TemplateIndex;
            set => _TemplateIndex = value;
        }

        [CanBeNull] public UObjectTableItem Template => Owner.GetIndexTable(_TemplateIndex);

        private int _ArchetypeIndex;

        [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
        public int ArchetypeIndex
        {
            get => _ArchetypeIndex;
            set => _ArchetypeIndex = value;
        }

        [CanBeNull] public UObjectTableItem Archetype => Owner.GetIndexTable(_ArchetypeIndex);

        /// <summary>
        /// The object flags <see cref="ObjectFlagsLO"/>
        /// </summary>
        public ulong ObjectFlags;

        /// <summary>
        /// The object size in bytes.
        /// </summary>
        public int SerialSize;

        /// <summary>
        /// The object offset in bytes. Starting from the beginning of a file.
        /// </summary>
        public int SerialOffset;

        /// <summary>
        /// The export flags to describe how this object was added to the export table.
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public uint ExportFlags;

        /// <summary>
        /// Always null if version is lower than <see cref="PackageObjectLegacyVersion.ArchetypeAddedToExports"/>
        /// or higher than <see cref="PackageObjectLegacyVersion.ComponentMapDeprecated"/>
        /// </summary>
        [BuildGeneration(BuildGeneration.UE3)]
        public UMap<UName, int> ComponentMap;

        /// <summary>
        /// The count of net objects for each generation <see cref="UGenerationTableItem.NetObjectCount"/> in this (exported) package.
        /// 
        /// Always null if version is lower than <see cref="PackageObjectLegacyVersion.NetObjectCountAdded"/>
        /// </summary>
        [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
        public UArray<int> GenerationNetObjectCount;

        /// <summary>
        /// The <see cref="UnrealPackage.PackageFileSummary.Guid"/> of the package that this object was exported from.
        /// </summary>
        [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
        public UGuid PackageGuid;

        /// <summary>
        /// The <see cref="UnrealPackage.PackageFileSummary.PackageFlags"/> of the package that this object was exported from.
        /// </summary>
        [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
        public uint PackageFlags;
        //public UnrealFlags<PackageFlag> PackageFlags;

        [BuildGeneration(BuildGeneration.UE4)] public bool IsNotForServer;
        [BuildGeneration(BuildGeneration.UE4)] public bool IsNotForClient;
        [BuildGeneration(BuildGeneration.UE4)] public bool IsForcedExport;
        [BuildGeneration(BuildGeneration.UE4)] public bool IsNotForEditorGame;
        [BuildGeneration(BuildGeneration.UE4)] public bool IsAsset;

        /// <summary>
        /// Serializes the export to a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Serialize(IUnrealStream, UExportTableItem)"/>
        /// </summary>
        /// <param name="stream">The output stream</param>
        public void Serialize(IUnrealStream stream)
        {
            stream.WriteIndex(_ClassIndex);
            stream.WriteIndex(_SuperIndex);
            stream.Write(_OuterIndex);
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.Version >= 132)
            {
                LibServices.LogService.SilentException(
                    new NotSupportedException("Missing an integer at " + stream.Position));
                // Assuming we are overriding...
                stream.Skip(sizeof(int));
            }
#endif
            stream.Write(_ObjectName);
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ArchetypeAddedToExports)
            {
                // Not using write index here because it's been deprecated for this version.
                stream.Write(_ArchetypeIndex);
            }
#if BATMAN
            if (stream.Package.Build == BuildGeneration.RSS)
            {
                LibServices.LogService.SilentException(
                    new NotSupportedException("Missing an integer at " + stream.Position));
                // Assuming we are overriding...
                stream.Skip(sizeof(int));
            }
#endif
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.LicenseeVersion >= 40)
            {
                stream.Write(ObjectFlags);

                goto streamSerialSize;
            }
#endif
            stream.Write(stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits
                ? ObjectFlags
                : (uint)ObjectFlags);

        streamSerialSize:
            stream.WriteIndex(SerialSize); // Assumes SerialSize has been updated to @Object's buffer size.
            if (SerialSize > 0 || stream.Version >= (uint)PackageObjectLegacyVersion.SerialSizeConditionRemoved)
            {
#if ROCKETLEAGUE
                // FIXME: Can't change SerialOffset to 64bit due UE Explorer.

                if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                    stream.LicenseeVersion >= 22)
                {
                    stream.Write((long)SerialOffset);

                    goto streamExportFlags;
                }
#endif
                // SerialOffset has to be set and written after this object has been serialized.
                stream.WriteIndex(SerialOffset); // Assumes the same as @SerialSize comment.
            }

#if BIOSHOCK
            // Overlaps with Tribes: Vengeance (130)
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.Version >= 130)
            {
                LibServices.LogService.SilentException(
                    new NotSupportedException("Missing an integer at " + stream.Position));
                stream.Skip(sizeof(int));
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.ArchetypeAddedToExports)
            {
                return;
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.ComponentMapDeprecated
#if ALPHAPROTOCOL
                && stream.Package.Build != UnrealPackage.GameBuild.BuildName.AlphaProtocol
#endif
#if TRANSFORMERS
                && (stream.Package.Build != BuildGeneration.HMS ||
                    stream.LicenseeVersion < 37)
#endif
               )
            {
                if (ComponentMap == null)
                {
                    stream.Write(0);
                }
                else
                {
                    stream.Write(ComponentMap.Count);
                    foreach (var keyValuePair in ComponentMap)
                    {
                        stream.Write(keyValuePair.Key);
                        stream.Write(keyValuePair.Value);
                    }
                }
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.ExportFlagsAddedToExports)
            {
                return;
            }

        streamExportFlags:
            stream.Write(ExportFlags);

            if (stream.Version < (uint)PackageObjectLegacyVersion.NetObjectCountAdded)
            {
                return;
            }

#if TRANSFORMERS
            if (stream.Package.Build == BuildGeneration.HMS &&
                stream.LicenseeVersion >= 116)
            {
                LibServices.LogService.SilentException(
                    new NotSupportedException("Missing a byte at " + stream.Position));

                const byte flag = 0;
                stream.Write(flag);

                if (flag == 0)
                {
                    return;
                }
            }
#endif
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Bioshock_Infinite)
            {
                LibServices.LogService.SilentException(new NotSupportedException("Missing data at " + stream.Position));

                uint unk = 0;
                stream.Write(unk);

                if (unk != 1)
                {
                    return;
                }

                uint flags = 0;
                stream.Write(flags);

                if ((flags & 1) != 0x0)
                {
                    // Perhaps GenerationNetObjectCount?
                    stream.Write((uint)0);
                }

                // Some kind of guid, maybe package?
                stream.Write(ref PackageGuid);
                stream.Write((uint)PackageFlags); // 01000020

                return;
            }
#endif
#if MKKE
            if (stream.Package.Build != UnrealPackage.GameBuild.BuildName.MKKE)
            {
#endif
                stream.WriteArray(GenerationNetObjectCount);
#if MKKE
            }
#endif
            stream.Write(ref PackageGuid);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.PackageFlagsAddedToExports)
            {
                stream.Write((uint)PackageFlags);
            }
        }

        /// <summary>
        /// Deserializes the export from a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Serialize(IUnrealStream, UExportTableItem)"/>
        /// </summary>
        /// <param name="stream">The input stream</param>
        public void Deserialize(IUnrealStream stream)
        {
            _ClassIndex = stream.ReadIndex();
            _SuperIndex = stream.ReadIndex();
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
            if (stream.Version < (uint)PackageObjectLegacyVersion.ArchetypeAddedToExports)
            {
                return;
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.ComponentMapDeprecated
#if ALPHAPROTOCOL
                && stream.Package.Build != UnrealPackage.GameBuild.BuildName.AlphaProtocol
#endif
#if TRANSFORMERS
                && (stream.Package.Build != BuildGeneration.HMS ||
                    stream.LicenseeVersion < 37)
#endif
               )
            {
                stream.Read(out int componentCount);
                ComponentMap = new UMap<UName, int>(componentCount);
                for (int i = 0; i < componentCount; ++i)
                {
                    stream.Read(out UName key);
                    stream.Read(out int value);
                    ComponentMap.Add(key, value);
                }
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.ExportFlagsAddedToExports)
            {
                return;
            }

        streamExportFlags:
            ExportFlags = stream.ReadUInt32();
            if (stream.Version < (uint)PackageObjectLegacyVersion.NetObjectCountAdded)
            {
                return;
            }

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
                if (unk != 1)
                {
                    return;
                }

                uint flags = stream.ReadUInt32();
                if ((flags & 1) != 0x0)
                {
                    // NetObjectCount?
                    stream.ReadUInt32();
                }

                // Some kind of guid, maybe package?
                stream.ReadStruct(out PackageGuid);
                stream.Read(out PackageFlags); // 01000020

                return;
            }
#endif
#if MKKE
            if (stream.Package.Build != UnrealPackage.GameBuild.BuildName.MKKE)
            {
#endif
                stream.ReadArray(out GenerationNetObjectCount);
#if MKKE
            }
#endif
            stream.ReadStruct(out PackageGuid);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.PackageFlagsAddedToExports)
            {
                stream.Read(out PackageFlags);
            }
        }

        public override string GetReferencePath()
        {
            return Class != null
                ? $"{Class.ObjectName}'{GetPath()}'"
                : $"Class'{GetPath()}'";
        }
        
        public static explicit operator int(UExportTableItem item)
        {
            return item.Index + 1;
        }
        
        public override string ToString()
        {
            return $"{ObjectName}({Index + 1})";
        }

        /// <summary>
        /// Displaced with <see cref="PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits"/>
        /// </summary>
        [Obsolete] public const int VObjectFlagsToULONG = 195;

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

        [Obsolete("Use ToString()")]
        public string ToString(bool v)
        {
            return ToString();
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
    }
}