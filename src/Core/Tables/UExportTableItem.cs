using System;
using System.ComponentModel;
using System.IO;
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
        private UPackageIndex _ClassIndex;

        public UPackageIndex ClassIndex
        {
            get => _ClassIndex;
            set => _ClassIndex = value;
        }

        public UObjectTableItem? Class => Package.IndexToObjectResource(ClassIndex);

        private UPackageIndex _SuperIndex;

        public UPackageIndex SuperIndex
        {
            get => _SuperIndex;
            set => _SuperIndex = value;
        }

        public UObjectTableItem? Super => Package.IndexToObjectResource(_SuperIndex);

        private UPackageIndex _TemplateIndex;

        [BuildGeneration(BuildGeneration.UE4)]
        public UPackageIndex TemplateIndex
        {
            get => _TemplateIndex;
            set => _TemplateIndex = value;
        }

        [BuildGeneration(BuildGeneration.UE4)] public UObjectTableItem? Template => Package.IndexToObjectResource(_TemplateIndex);

        private UPackageIndex _ArchetypeIndex;

        [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
        public UPackageIndex ArchetypeIndex
        {
            get => _ArchetypeIndex;
            set => _ArchetypeIndex = value;
        }

        [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
        public UObjectTableItem? Archetype => Package.IndexToObjectResource(_ArchetypeIndex);

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
        public UMap<UName, UPackageIndex> ComponentMap;

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

        [BuildGeneration(BuildGeneration.UE4)] public int FirstExportDependency;
        [BuildGeneration(BuildGeneration.UE4)] public int SerializationBeforeSerializationDependencies;
        [BuildGeneration(BuildGeneration.UE4)] public int CreateBeforeSerializationDependencies;
        [BuildGeneration(BuildGeneration.UE4)] public int SerializationBeforeCreateDependencies;
        [BuildGeneration(BuildGeneration.UE4)] public int CreateBeforeCreateDependencies;

        /// <summary>
        /// Serializes the export to a stream.
        /// 
        /// For UE4 see: <seealso cref="UELib.Branch.UE4.PackageSerializerUE4.Serialize(IUnrealStream, UExportTableItem)"/>
        /// </summary>
        /// <param name="stream">The output stream</param>
        public void Serialize(IUnrealStream stream)
        {
            stream.Write(_ClassIndex);
            stream.Write(_SuperIndex);
            // version >= 50
            stream.Write((int)_OuterIndex); // Never serialize it as an index, it must be written as 32 bits regardless of build.
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
                stream.Write((int)_ArchetypeIndex);
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
#if LEAD
            if (stream.Package.Build == BuildGeneration.Lead &&
                stream.LicenseeVersion >= 93)
            {
                stream.Write(ObjectFlags);

                goto streamSerialSize;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                if (stream.BigEndianCode)
                {
                    ulong shiftedFlags = (ObjectFlags << 32) | (ObjectFlags >> 32);
                    stream.Write(shiftedFlags);
                }
                else
                {
                    ulong shiftedFlags = (ObjectFlags >> 32) | (ObjectFlags << 32);
                    stream.Write(shiftedFlags);
                }
            }
            else
            {
                stream.Write((uint)ObjectFlags);
            }

        streamSerialSize:
            stream.WriteIndex(SerialSize); // Assumes SerialSize has been updated to @Object's buffer size.
#if ROCKETLEAGUE
            // FIXME: Can't change SerialOffset to 64bit due UE Explorer.

            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 22)
            {
                stream.Write((long)SerialOffset);

                goto streamExportFlags;
            }
#endif
            if (SerialSize > 0 || stream.Version >= (uint)PackageObjectLegacyVersion.SerialSizeConditionRemoved)
            {
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
#if HUXLEY
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.Huxley)
            {
                if (stream.LicenseeVersion >= 22)
                {
                    LibServices.LogService.SilentException(
                        new NotSupportedException("Missing an integer at " + stream.Position));
                    stream.Write(SerialSize);
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
                        stream.Write((int)keyValuePair.Value);
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
            stream.Read(out _ClassIndex);
            stream.Read(out _SuperIndex);
            // version >= 50
            _OuterIndex = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
#if BIOSHOCK
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.BioShock &&
                stream.Version >= 132)
            {
                stream.Skip(sizeof(int));
            }
#endif
            _ObjectName = stream.ReadName();
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
#if LEAD
            if (stream.Package.Build == BuildGeneration.Lead &&
                stream.LicenseeVersion >= 93)
            {
                ObjectFlags = stream.ReadUInt64();

                goto streamSerialSize;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.ObjectFlagsSizeExpandedTo64Bits)
            {
                ObjectFlags = stream.ReadUInt64();
                if (stream.BigEndianCode)
                {
                    ObjectFlags = (ObjectFlags << 32) | (ObjectFlags >> 32);
                }
                else
                {
                    ObjectFlags = (ObjectFlags >> 32) | (ObjectFlags << 32);
                }
            }
            else
            {
                ObjectFlags = stream.ReadUInt32();
            }

        streamSerialSize:
            SerialSize = stream.ReadIndex();
#if ROCKETLEAGUE
            // FIXME: Can't change SerialOffset to 64bit due UE Explorer.

            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 22)
            {
                SerialOffset = (int)stream.ReadInt64();

                goto streamExportFlags;
            }
#endif
            if (SerialSize > 0 || stream.Version >= (uint)PackageObjectLegacyVersion.SerialSizeConditionRemoved)
            {
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
                    stream.Read(out int serialSize2);
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
                stream.Read(out int componentCount);
                ComponentMap = new UMap<UName, UPackageIndex>(componentCount);
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
        public UObjectTableItem ClassTable => Package.IndexToObjectResource(_ClassIndex);

        [Obsolete] protected override int __ClassIndex => _ClassIndex;

        [Obsolete] protected override string __ClassName => Class?.ObjectName ?? "Class";

        [Obsolete("Use Super"), Browsable(false)]
        public UObjectTableItem SuperTable => Package.IndexToObjectResource(_SuperIndex);

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
        public UObjectTableItem ArchetypeTable => Package.IndexToObjectResource(_ArchetypeIndex);

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
            Package.Stream.Seek(_ObjectFlagsOffset, SeekOrigin.Begin);
            Package.Stream.Write((uint)ObjectFlags);
        }
    }
}
