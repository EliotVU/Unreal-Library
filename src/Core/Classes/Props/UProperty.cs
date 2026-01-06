using System;
using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.Flags;
using UELib.IO;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UProperty/Core.Property
    /// </summary>
    [UnrealRegisterClass] // Never instantiated, but, we need to register this so that derived static classes can link correctly.
    public partial class UProperty : UField, IUnrealNetObject
    {
        public PropertyType Type { get; protected set; } = PropertyType.None;

        #region Serialized Members

        /// <summary>
        ///     The array dimension of this property.
        ///
        ///     In UnrealScript all properties are fixed-arrays of a length of 1; so a length greater than 1 indicates that this property is declared as an array.
        /// </summary>
        [StreamRecord]
        public int ArrayDim { get; set; }

        /// <summary>
        /// The property flags, which indicate various modifiers of this property.
        /// </summary>
        [StreamRecord]
        public UnrealFlags<PropertyFlag> PropertyFlags
        {
            get => _PropertyFlags;
            set => _PropertyFlags = value;
        }

        private UnrealFlags<PropertyFlag> _PropertyFlags;
#if XCOM2
        [StreamRecord, Build(UnrealPackage.GameBuild.BuildName.XCOM2WotC)]
        public UName? ConfigName { get; set; }
#endif
        /// <summary>
        ///     The name of the category this property belongs to.
        ///     "None" indicates that this property is not categorized; otherwise if equivalent to 'Outer.Class.Name' then, it is categorized without a custom name.
        /// </summary>
        [StreamRecord]
        public UName CategoryName
        {
            get => _CategoryName;
            set => _CategoryName = value;
        }

        private UName _CategoryName = UnrealName.None;

        /// <summary>
        ///     The enum used to represent the array dimension, if any.
        /// </summary>
        [StreamRecord, BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
        public UEnum? ArrayEnum { get; set; }

        [StreamRecord, BuildGeneration(BuildGeneration.UE4), Build(UnrealPackage.GameBuild.BuildName.DNF)]
        public UName RepNotifyFuncName { get; set; } = UnrealName.None;

        /// <summary>
        /// Stored meta-data in the "option" format (i.e. WebAdmin, and commandline options), used to assist developers in the editor.
        /// e.g. <code>var int MyVariable "PI:Property Two:Game:1:60:Check" ...["SecondOption"]</code>
        /// 
        /// An original terminating \" character is serialized as a \n character, the string will also end with a newline character.
        /// </summary>
        [StreamRecord]
        public string? EditorDataText { get; set; }

        #endregion

        private bool IsArray => ArrayDim > 1;

        /// <summary>
        ///     The element size of this property in memory.
        /// </summary>
        public ushort ElementSize => (ushort)(ArrayDim >> 16);

        /// <summary>
        ///     The offset to the conditional in the replication script of the outer-class.
        /// </summary>
        [StreamRecord]
        public ushort RepOffset { get; set; }

        /// <summary>
        ///     Whether this property is marked with the 'reliable' modifier in the replication block.
        /// </summary>
        public bool RepReliable => HasPropertyFlag(PropertyFlag.Net);

        public uint RepKey => RepOffset | ((uint)Convert.ToByte(RepReliable) << 16);

        public override void Deserialize(IUnrealStream stream)
        {
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent)
            {
                // Serialize FProperty

                ArrayDim = stream.ReadInt32();
                stream.Record(nameof(ArrayDim), ArrayDim);

                ArrayDim = PropertyFlags >> 16;
                uint propertyIndex = PropertyFlags & 0x0000FFFFU;

                PropertyFlags =
                    new UnrealFlags<PropertyFlag>(stream.ReadUInt32(),
                                                  stream.Package.Branch.EnumFlagsMap[typeof(PropertyFlag)]);
                stream.Record(nameof(PropertyFlags), PropertyFlags);

                CategoryName = stream.ReadName();
                stream.Record(nameof(CategoryName), CategoryName);

                if (stream.LicenseeVersion < 6 && (PropertyFlags & 0x20) != 0)
                {
                    RepOffset = stream.ReadUInt16();
                    stream.Record(nameof(RepOffset), RepOffset);
                }

                // Skip base.
                return;
            }
#endif
            base.Deserialize(stream);
#if SPLINTERCELLX
            if (stream.Build == BuildGeneration.SCX &&
                stream.LicenseeVersion >= 15)
            {
                // 32bit => 16bit
                ArrayDim = stream.ReadUInt16();
                stream.Record(nameof(ArrayDim), ArrayDim);

                stream.Read(out _PropertyFlags);
                stream.Record(nameof(PropertyFlags), PropertyFlags);

                stream.Read(out _CategoryName);
                stream.Record(nameof(CategoryName), CategoryName);

                // FIXME: Unknown version, attested without a version check since SC3 and SC4.
                if (stream.Build != UnrealPackage.GameBuild.BuildName.SCPT_Offline &&
                    stream.LicenseeVersion > 17) // 17 = newer than SC1
                {
                    // Music? Some kind of alternative to category name
                    stream.Read(out UName v68);
                    stream.Record(nameof(v68), v68);
                }

                return;
            }
#endif
#if LEAD
            if (stream.Build == BuildGeneration.Lead)
            {
                // 32bit => 16bit
                ArrayDim = stream.ReadUInt16();
                stream.Record(nameof(ArrayDim), ArrayDim);

                stream.Read(out _PropertyFlags);
                stream.Record(nameof(PropertyFlags), PropertyFlags);

                if (stream.LicenseeVersion >= 72)
                {
                    ushort v34 = stream.ReadUInt16();
                    stream.Record(nameof(v34), v34);
                }

                stream.Read(out _CategoryName);
                stream.Record(nameof(CategoryName), CategoryName);

                // not versioned
                var v4c = stream.ReadName();
                stream.Record(nameof(v4c), v4c);

                if (stream.LicenseeVersion >= 4)
                {
                    // CommentString
                    EditorDataText = stream.ReadString(); // v50
                    stream.Record(nameof(EditorDataText), EditorDataText);
                }

                if (stream.LicenseeVersion >= 11)
                {
                    // Usually 0 or 0xAA88FF
                    uint v5c = stream.ReadUInt32();
                    stream.Record(nameof(v5c), v5c);

                    uint v60 = stream.ReadUInt32();
                    stream.Record(nameof(v60), v60);

                    // Display name e.g. SpecularMask = Specular
                    string v64 = stream.ReadString();
                    stream.Record(nameof(v64), v64);
                }

                if (stream.LicenseeVersion >= 101)
                {
                    var v7c = stream.ReadName();
                    stream.Record(nameof(v7c), v7c);
                }

                return;
            }
#endif
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                if (stream.Version < 137)
                {
                    NextField = stream.ReadObject<UField?>();
                    stream.Record(nameof(NextField), NextField);
                }

                if (stream.Version >= 136)
                {
                    // 32bit => 16bit
                    ArrayDim = stream.ReadUInt16();
                    stream.Record(nameof(ArrayDim), ArrayDim);

                    goto skipArrayDim;
                }
            }
#endif
#if AA2
            if (stream.Build == BuildGeneration.AGP &&
                stream.LicenseeVersion >= 8)
            {
                // Always 26125 (hardcoded in the assembly) 
                uint aa2FixedPack = stream.ReadUInt32();
                stream.Record(nameof(aa2FixedPack), aa2FixedPack);
            }
#endif
#if XIII || DNF || MOV
            // TODO: (UE2X) Version 131 ArrayDim size changed from DWORD to WORD
            if (stream.Build == UnrealPackage.GameBuild.BuildName.XIII ||
                stream.Build == UnrealPackage.GameBuild.BuildName.DNF ||
                stream.Build == UnrealPackage.GameBuild.BuildName.MOV)
            {
                ArrayDim = stream.ReadInt16();
                stream.Record(nameof(ArrayDim), ArrayDim);

                goto skipArrayDim;
            }
#endif
            ArrayDim = stream.ReadInt32();
            stream.Record(nameof(ArrayDim), ArrayDim);
        skipArrayDim:
            // Just to verify if this is in use at all.
            //Debug.Assert(ElementSize == 0, $"ElementSize: {ElementSize}");
            // 2048 is the max allowed dimension in the UnrealScript compiler, however some licensees have extended this to a much higher size.
            //Debug.Assert(
            //    (ArrayDim & 0x0000FFFFU) > 0 && (ArrayDim & 0x0000FFFFU) <= 2048, 
            //    $"Bad array dimension {ArrayDim & 0x0000FFFFU} for property ${GetReferencePath()}");

            ulong propertyFlags = stream.Version >= (uint)PackageObjectLegacyVersion.PropertyFlagsSizeExpandedTo64Bits
                ? stream.ReadUInt64()
                : stream.ReadUInt32();
#if BATMAN
            if (stream.Build == BuildGeneration.RSS)
            {
                if (stream.LicenseeVersion >= 101)
                {
                    // DAT_14313fdc0
                    ulong[] flagMasks =
                    [
                        0x0000000000000002, 0x0000000000000004, 0x0000000000000008, 0x0000000000000080,
                        0x0000000000000010, 0x0000000000000100, 0x0000000000000200, 0x0000000000000400,
                        0x0000000000000800, 0x0000000000001000, 0x0000000000002000, 0x0000000000004000,
                        0x0000000000008000, 0x0000000000040000, 0x0000000000080000, 0x0000000000200000,
                        0x0000000000400000, 0x0000000000800000, 0x0000000010000000, 0x0000000200000000,
                        0x0000000400000000, 0x0000000800000000, 0x0000004000000000, 0x0000010000000000,
                        0x0000020000000000, 0x0000000000000020, 0x0000000200000000, 0x0000000010000000,
                        0x0000000000000000, 0x0000000000000000, 0x0000000000000000, 0x0000000000000001,
                        0x0000000000000040, 0x0000000000020000, 0x0000000000200000, 0x0000004000000000,
                        0x0000008000000000, 0x0000000040000000, 0x0000000100000000, 0x0000002000000000,
                        0x0000010000000000, 0x0000001000000000, 0x0000000080000000, 0x0000000100000000,
                        0x0000000400000000, 0x0000000800000000, 0x0000000000000000
                    ];

                    ulong originalFlags = 0;
                    ulong bitMask = 1;

                    foreach (ulong flag in flagMasks)
                    {
                        if ((propertyFlags & bitMask) != 0)
                        {
                            originalFlags |= flag;
                        }

                        bitMask <<= 1;
                    }

                    propertyFlags = originalFlags;
                }
            }
#endif
            PropertyFlags =
                new UnrealFlags<PropertyFlag>(propertyFlags, stream.Package.Branch.EnumFlagsMap[typeof(PropertyFlag)]);
            stream.Record(nameof(PropertyFlags), PropertyFlags);
#if XCOM2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.XCOM2WotC)
            {
                ConfigName = stream.ReadName();
                stream.Record(nameof(ConfigName), ConfigName);
            }
#endif
#if THIEF_DS || DEUSEX_IW
            if (stream.Build == BuildGeneration.Flesh)
            {
                // Property flags like CustomEditor, CustomViewer, ThiefProp, DeusExProp, NoTextExport, NoTravel
                uint deusFlags = stream.ReadUInt32();
                stream.Record(nameof(deusFlags), deusFlags);
            }
#endif
            if (stream.ContainsEditorOnlyData()
#if MASS_EFFECT
                // M1:LE is cooked for "WindowsConsole" yet retains this data.
                || stream.Build == BuildGeneration.SFX
#endif
               )
            {
                // TODO: Not serialized if XENON (UE2X)
                // FIXME: UE4 version
                if (stream.UE4Version < 160)
                {
                    CategoryName = stream.ReadName();
                    stream.Record(nameof(CategoryName), CategoryName);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedArrayEnumToUProperty
#if MIDWAY
                    || stream.Build == UnrealPackage.GameBuild.BuildName.Stranglehold
#endif
                   )
                {
                    ArrayEnum = stream.ReadObject<UEnum?>();
                    stream.Record(nameof(ArrayEnum), ArrayEnum);
                }
            }

#if THIEF_DS || DEUSEX_IW
            if (stream.Build == BuildGeneration.Flesh)
            {
                short deusInheritedOrRuntimeInstantiated = stream.ReadInt16();
                stream.Record(nameof(deusInheritedOrRuntimeInstantiated), deusInheritedOrRuntimeInstantiated);
                short deusUnkInt16 = stream.ReadInt16();
                stream.Record(nameof(deusUnkInt16), deusUnkInt16);
            }
#endif
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 2)
            {
                var va8 = stream.ReadObject();
                stream.Record(nameof(va8), va8);
                var vb0 = stream.ReadObject();
                stream.Record(nameof(vb0), vb0);
            }
#endif
#if UE4
            if (stream.IsUE4())
            {
                RepNotifyFuncName = stream.ReadName();
                stream.Record(nameof(RepNotifyFuncName), RepNotifyFuncName);

                return;
            }
#endif
            if (HasPropertyFlag(PropertyFlag.Net))
            {
                RepOffset = stream.ReadUShort();
                stream.Record(nameof(RepOffset), RepOffset);
            }
#if BATTLEBORN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Battleborn
                // NetVersion
                && Package.Summary.EngineVersion >> 16 >= 1046)
            {
                if (PropertyFlags.HasFlag(PropertyFlag.Net) &&
                    PropertyFlags.HasFlag(PropertyFlag.RepNotify))
                {
                    var v78 = stream.ReadObject();
                    stream.Record(nameof(v78), v78);

                    RepNotifyFuncName = v78?.Name ?? UnrealName.None;
                }
            }
#endif
#if HUXLEY
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Huxley)
            {
                // A property linked to the "Core.Object.LazyLoadPropertyInfo" struct.
                var partLoadInfoProperty = stream.ReadObject();
                stream.Record(nameof(partLoadInfoProperty), partLoadInfoProperty);
            }
#endif
#if R6
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
            {
                stream.Read(out string v0c);
                stream.Record(nameof(v0c), v0c);

                EditorDataText = v0c;
            }
#endif
#if ROCKETLEAGUE
            // identical to this object's name.
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 11)
            {
                string vb8 = stream.ReadString();
                stream.Record(nameof(vb8), vb8);

                //if (stream.LicenseeVersion == 15)
                //{
                //    var v68 = stream.ReadName();
                //    stream.Record(nameof(v68), v68);
                //}
            }
#endif
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance)
            {
                var vengeanceEditComboType = stream.ReadName();
                stream.Record(nameof(vengeanceEditComboType), vengeanceEditComboType);
                var vengeanceEditDisplay = stream.ReadName();
                stream.Record(nameof(vengeanceEditDisplay), vengeanceEditDisplay);
            }
#endif
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (HasAnyPropertyFlags(0x800000))
                {
                    EditorDataText = stream.ReadString();
                    stream.Record(nameof(EditorDataText), EditorDataText);
                }

                // Same flag as EditorData, but this may merely be a coincidence, see above
                if (stream.Version >= 118 && HasAnyPropertyFlags(0x2000000))
                {
                    // a.k.a NetUpdateName ;)
                    RepNotifyFuncName = stream.ReadName();
                    stream.Record(nameof(RepNotifyFuncName), RepNotifyFuncName);
                }

                return;
            }
#endif
            // Appears to be a UE2.5 feature, it is not present in UE2 builds with no custom LicenseeVersion
            // Albeit DeusEx indicates otherwise?
            if ((HasPropertyFlag(PropertyFlag.CommentString) &&
                 (stream.Build == BuildGeneration.UE2_5
                  || stream.Build == BuildGeneration.AGP
                  || stream.Build == BuildGeneration.Flesh))
                // No property flag check
#if VENGEANCE
                || stream.Build == BuildGeneration.Vengeance
#endif
#if MOV
                // No property flag check
                || stream.Build == UnrealPackage.GameBuild.BuildName.MOV
#endif
#if LSGAME
                // No property flag check
                || (stream.Build == UnrealPackage.GameBuild.BuildName.LSGame &&
                    stream.LicenseeVersion >= 3)
#endif
#if DEVASTATION
                // No property flag check
                || stream.Build == UnrealPackage.GameBuild.BuildName.Devastation
#endif
               )
            {
                // May represent a tooltip/comment in some games. Usually in the form of a quoted string, sometimes as a double-flash comment or both.
                EditorDataText = stream.ReadString();
                stream.Record(nameof(EditorDataText), EditorDataText);
            }
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                if (stream.Version < 157)
                {
                    throw new NotSupportedException("< 157 Spellborn packages are not supported");

                    if (133 < stream.Version)
                    {
                        // idk
                    }

                    if (134 < stream.Version)
                    {
                        int unk32 = stream.ReadInt32();
                        stream.Record("Unknown:Spellborn", unk32);
                    }
                }
                else
                {
                    uint replicationFlags = stream.ReadUInt32();
                    stream.Record(nameof(replicationFlags), replicationFlags);
                }
            }
#endif
        }

        public override void Serialize(IUnrealStream stream)
        {
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent)
            {
                // Serialize FProperty

                stream.Write(ArrayDim);
                // ArrayDim = PropertyFlags >> 16;
                // uint propertyIndex = PropertyFlags & 0x0000FFFFU;
                stream.Write((uint)PropertyFlags);
                stream.Write(CategoryName);

                if (stream.LicenseeVersion < 6 && ((uint)PropertyFlags & 0x20) != 0)
                {
                    stream.Write(RepOffset);
                }

                // Skip base.
                return;
            }
#endif
            base.Serialize(stream);
#if SPLINTERCELLX
            if (stream.Build == BuildGeneration.SCX &&
                stream.LicenseeVersion >= 15)
            {
                stream.Write((ushort)ArrayDim);
                stream.Write(_PropertyFlags);
                stream.Write(_CategoryName);

                if (stream.Build != UnrealPackage.GameBuild.BuildName.SCPT_Offline &&
                    stream.LicenseeVersion > 17)
                {
                    // Music? Some kind of alternative to category name
                    UName v68 = default;
                    stream.Write(v68);
                }

                return;
            }
#endif
#if LEAD
            if (stream.Build == BuildGeneration.Lead)
            {
                stream.Write((ushort)ArrayDim);
                stream.Write(_PropertyFlags);

                if (stream.LicenseeVersion >= 72)
                {
                    ushort v34 = 0;
                    stream.Write(v34);
                }

                stream.Write(_CategoryName);

                var v4c = UnrealName.None;
                stream.Write(v4c);

                if (stream.LicenseeVersion >= 4)
                {
                    stream.Write(EditorDataText ?? string.Empty);
                }

                if (stream.LicenseeVersion >= 11)
                {
                    uint v5c = 0;
                    stream.Write(v5c);

                    uint v60 = 0;
                    stream.Write(v60);

                    string v64 = string.Empty;
                    stream.Write(v64);
                }

                if (stream.LicenseeVersion >= 101)
                {
                    var v7c = UnrealName.None;
                    stream.Write(v7c);
                }

                return;
            }
#endif
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                if (stream.Version < 137)
                {
                    stream.Write(NextField);
                }

                if (stream.Version >= 136)
                {
                    stream.Write((ushort)ArrayDim);

                    goto skipArrayDim;
                }
            }
#endif
#if AA2
            if (stream.Build == BuildGeneration.AGP &&
                stream.LicenseeVersion >= 8)
            {
                uint aa2FixedPack = 26125;
                stream.Write(aa2FixedPack);
            }
#endif
#if XIII || DNF || MOV
            if (stream.Build == UnrealPackage.GameBuild.BuildName.XIII ||
                stream.Build == UnrealPackage.GameBuild.BuildName.DNF ||
                stream.Build == UnrealPackage.GameBuild.BuildName.MOV)
            {
                stream.Write((short)ArrayDim);

                goto skipArrayDim;
            }
#endif
            stream.Write(ArrayDim);
        skipArrayDim:
            ulong propertyFlags = PropertyFlags;
            if (stream.Version >= (uint)PackageObjectLegacyVersion.PropertyFlagsSizeExpandedTo64Bits)
            {
                stream.Write(propertyFlags);
            }
            else
            {
                stream.Write((uint)propertyFlags);
            }
#if XCOM2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.XCOM2WotC)
            {
                stream.Write(ConfigName ?? UnrealName.None);
            }
#endif
#if THIEF_DS || DEUSEX_IW
            if (stream.Build == BuildGeneration.Flesh)
            {
                uint deusFlags = 0;
                stream.Write(deusFlags);
            }
#endif
            if (stream.ContainsEditorOnlyData()
#if MASS_EFFECT
                || stream.Build == BuildGeneration.SFX
#endif
               )
            {
                if (stream.UE4Version < 160)
                {
                    stream.Write(CategoryName);
                }

                if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedArrayEnumToUProperty
#if MIDWAY
                    || stream.Build == UnrealPackage.GameBuild.BuildName.Stranglehold
#endif
                   )
                {
                    stream.Write(ArrayEnum);
                }
            }

#if THIEF_DS || DEUSEX_IW
            if (stream.Build == BuildGeneration.Flesh)
            {
                short deusInheritedOrRuntimeInstantiated = 0;
                stream.Write(deusInheritedOrRuntimeInstantiated);
                short deusUnkInt16 = 0;
                stream.Write(deusUnkInt16);
            }
#endif
#if BORDERLANDS
            if (stream.Build == BuildGeneration.GB &&
                stream.LicenseeVersion >= 2)
            {
                UObject va8 = null;
                stream.Write(va8);
                UObject vb0 = null;
                stream.Write(vb0);
            }
#endif
#if UE4
            if (stream.IsUE4())
            {
                stream.Write(RepNotifyFuncName);

                return;
            }
#endif
            if (HasPropertyFlag(PropertyFlag.Net))
            {
                stream.Write(RepOffset);
            }
#if BATTLEBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Battleborn
                // NetVersion
                && Package.Summary.EngineVersion >> 16 >= 1046)
            {
                if (PropertyFlags.HasFlag(PropertyFlag.Net) &&
                    PropertyFlags.HasFlag(PropertyFlag.RepNotify))
                {
                    // TODO: Look up by name, or, preserve the rep function object.
                    Contract.Assert(RepNotifyFuncName.IsNone() == false);
                    stream.WriteObject<UObject>(null); // v78
                }
            }
#endif
#if HUXLEY
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Huxley)
            {
                UObject obj = null;
                stream.Write(obj);
            }
#endif
#if R6
            if (stream.Build == UnrealPackage.GameBuild.BuildName.R6Vegas)
            {
                string v0c = EditorDataText ?? string.Empty;
                stream.Write(v0c);
            }
#endif
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 11)
            {
                string vb8 = string.Empty;
                stream.Write(vb8);
            }
#endif
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance)
            {
                var vengeanceEditComboType = UnrealName.None;
                stream.Write(vengeanceEditComboType);
                var vengeanceEditDisplay = UnrealName.None;
                stream.Write(vengeanceEditDisplay);
            }
#endif
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (HasAnyPropertyFlags(0x800000))
                {
                    stream.Write(EditorDataText ?? string.Empty);
                }

                if (stream.Version >= 118 && HasAnyPropertyFlags(0x2000000))
                {
                    stream.Write(RepNotifyFuncName);
                }

                return;
            }
#endif
            if ((HasPropertyFlag(PropertyFlag.CommentString) &&
                 (stream.Build == BuildGeneration.UE2_5
                  || stream.Build == BuildGeneration.AGP
                  || stream.Build == BuildGeneration.Flesh))
#if VENGEANCE
                || stream.Build == BuildGeneration.Vengeance
#endif
#if MOV
                || stream.Build == UnrealPackage.GameBuild.BuildName.MOV
#endif
#if LSGAME
                || (stream.Build == UnrealPackage.GameBuild.BuildName.LSGame &&
                    stream.LicenseeVersion >= 3)
#endif
#if DEVASTATION
                || stream.Build == UnrealPackage.GameBuild.BuildName.Devastation
#endif
               )
            {
                stream.Write(EditorDataText ?? string.Empty);
            }
#if SPELLBORN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                if (stream.Version < 157)
                {
                    throw new NotSupportedException("< 157 Spellborn packages are not supported");

                    if (133 < stream.Version)
                    {
                        // idk
                    }

                    if (134 < stream.Version)
                    {
                        int unk32 = 0;
                        stream.Write(unk32);
                        ;
                    }
                }
                else
                {
                    uint replicationFlags = 0;
                    stream.Write(replicationFlags);
                }
            }
#endif
        }

        protected override bool CanDisposeBuffer()
        {
            return true;
        }

        [Obsolete("Use HasAnyPropertyFlags")]
        public bool HasPropertyFlag(uint flag)
        {
            return ((uint)PropertyFlags & flag) != 0;
        }

        [Obsolete("Use HasAnyPropertyFlags or HasPropertyFlag")]
        public bool HasPropertyFlag(PropertyFlagsLO flag)
        {
            return ((uint)(PropertyFlags & 0x00000000FFFFFFFFU) & (uint)flag) != 0;
        }

        [Obsolete("Use HasAnyPropertyFlags or HasPropertyFlag")]
        public bool HasPropertyFlag(PropertyFlagsHO flag)
        {
            return (PropertyFlags & ((ulong)flag << 32)) != 0;
        }

        internal bool HasPropertyFlag(PropertyFlag flagIndex)
        {
            return PropertyFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(PropertyFlag)], flagIndex);
        }

        public bool HasAnyPropertyFlags(ulong flag)
        {
            return (PropertyFlags & flag) != 0;
        }

        public bool IsParm()
        {
            return PropertyFlags.HasFlag(PropertyFlag.Parm);
        }

        public virtual string GetFriendlyInnerType()
        {
            return string.Empty;
        }

        [Obsolete("See CategoryName", true)] public int CategoryIndex { get; }
    }
}
