using System;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Flags;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Represents a unreal property.
    /// </summary>
    public partial class UProperty : UField, IUnrealNetObject
    {
        #region PreInitialized Members

        public PropertyType Type { get; protected set; }

        #endregion

        #region Serialized Members

        public int ArrayDim { get; private set; }

        public ushort ElementSize { get; private set; }

        public ulong PropertyFlags { get; private set; }

#if XCOM2
        [CanBeNull] public UName ConfigName;
#endif

        [CanBeNull] public UName CategoryName;

        [Obsolete("See CategoryName")] public int CategoryIndex { get; }

        [CanBeNull] public UEnum ArrayEnum { get; private set; }

        [CanBeNull] public UName RepNotifyFuncName;

        public ushort RepOffset { get; private set; }

        public bool RepReliable => HasPropertyFlag(PropertyFlagsLO.Net);

        public uint RepKey => RepOffset | ((uint)Convert.ToByte(RepReliable) << 16);

        /// <summary>
        /// Stored meta-data in the "option" format (i.e. WebAdmin, and commandline options), used to assist developers in the editor.
        /// e.g. <code>var int MyVariable "PI:Property Two:Game:1:60:Check" ...["SecondOption"]</code>
        /// 
        /// An original terminating \" character is serialized as a \n character, the string will also end with a newline character.
        /// </summary>
        [CanBeNull] public string EditorDataText;

        #endregion

        #region General Members

        private bool _IsArray => ArrayDim > 1;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the UELib.Core.UProperty class.
        /// </summary>
        public UProperty()
        {
            Type = PropertyType.None;
        }

        protected override void Deserialize()
        {
            base.Deserialize();
#if SPLINTERCELL
            if (Package.Build == UnrealPackage.GameBuild.BuildName.SC1 &&
                _Buffer.LicenseeVersion >= 15)
            {
                ArrayDim = _Buffer.ReadUInt16();
                Record(nameof(ArrayDim), ArrayDim);

                PropertyFlags = _Buffer.ReadUInt32();
                Record(nameof(PropertyFlags), PropertyFlags);

                _Buffer.Read(out CategoryName);
                Record(nameof(CategoryName), CategoryName);

                return;
            }
#endif
#if AA2
            if (Package.Build == BuildGeneration.AGP &&
                _Buffer.LicenseeVersion >= 8)
            {
                // Always 26125 (hardcoded in the assembly) 
                uint aa2FixedPack = _Buffer.ReadUInt32();
                Record(nameof(aa2FixedPack), aa2FixedPack);
            }
#endif
#if XIII || DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.XIII ||
                Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                ArrayDim = _Buffer.ReadInt16();
                Record(nameof(ArrayDim), ArrayDim);
                goto skipArrayDim;
            }
#endif
            ArrayDim = _Buffer.ReadInt32();
            Record(nameof(ArrayDim), ArrayDim);
            ElementSize = (ushort)(ArrayDim >> 16);
        skipArrayDim:
            // Just to verify if this is in use at all.
            Debug.Assert(ElementSize == 0, $"ElementSize: {ElementSize}");
            // 2048 is the max allowed dimension in the UnrealScript compiler, however some licensees have extended this to a much higher size.
            //Debug.Assert(
            //    (ArrayDim & 0x0000FFFFU) > 0 && (ArrayDim & 0x0000FFFFU) <= 2048, 
            //    $"Bad array dimension {ArrayDim & 0x0000FFFFU} for property ${GetReferencePath()}");

            PropertyFlags = Package.Version >= 220
                ? _Buffer.ReadUInt64()
                : _Buffer.ReadUInt32();
            Record(nameof(PropertyFlags), PropertyFlags);
#if BATMAN
            if (Package.Build == BuildGeneration.RSS &&
                _Buffer.LicenseeVersion >= 101)
            {
                PropertyFlags = (PropertyFlags & 0xFFFF0000) >> 24;
                Record(nameof(PropertyFlags), (PropertyFlagsLO)PropertyFlags);
            }
#endif
#if XCOM2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.XCOM2WotC)
            {
                ConfigName = _Buffer.ReadNameReference();
                Record(nameof(ConfigName), ConfigName);
            }
#endif
#if THIEF_DS || DEUSEX_IW
            if (Package.Build == BuildGeneration.Flesh)
            {
                // Property flags like CustomEditor, CustomViewer, ThiefProp, DeusExProp, NoTextExport, NoTravel
                uint deusFlags = _Buffer.ReadUInt32();
                Record(nameof(deusFlags), deusFlags);
            }
#endif
            if (!Package.IsConsoleCooked())
            {
                // FIXME: UE4 version
                if (_Buffer.UE4Version < 160)
                {
                    CategoryName = _Buffer.ReadNameReference();
                    Record(nameof(CategoryName), CategoryName);
                }

                if (_Buffer.Version > 400)
                {
                    ArrayEnum = _Buffer.ReadObject<UEnum>();
                    Record(nameof(ArrayEnum), ArrayEnum);
                }
            }

#if THIEF_DS || DEUSEX_IW
            if (Package.Build == BuildGeneration.Flesh)
            {
                short deusInheritedOrRuntimeInstiantiated = _Buffer.ReadInt16();
                Record(nameof(deusInheritedOrRuntimeInstiantiated), deusInheritedOrRuntimeInstiantiated);
                short deusUnkInt16 = _Buffer.ReadInt16();
                Record(nameof(deusUnkInt16), deusUnkInt16);
            }
#endif
#if UE4
            if (_Buffer.UE4Version > 0)
            {
                RepNotifyFuncName = _Buffer.ReadNameReference();
                Record(nameof(RepNotifyFuncName), RepNotifyFuncName);
                return;
            }
#endif
            if (HasPropertyFlag(PropertyFlagsLO.Net))
            {
                RepOffset = _Buffer.ReadUShort();
                Record(nameof(RepOffset), RepOffset);
            }
#if VENGEANCE
            if (Package.Build == BuildGeneration.Vengeance)
            {
                var vengeanceEditComboType = _Buffer.ReadNameReference();
                Record(nameof(vengeanceEditComboType), vengeanceEditComboType);
                var vengeanceEditDisplay = _Buffer.ReadNameReference();
                Record(nameof(vengeanceEditDisplay), vengeanceEditDisplay);
            }
#endif
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (HasPropertyFlag(0x800000))
                {
                    EditorDataText = _Buffer.ReadText();
                    Record(nameof(EditorDataText), EditorDataText);
                }

                // Same flag as EditorData, but this may merely be a coincidence, see above
                if (_Buffer.Version >= 118 && HasPropertyFlag(0x2000000))
                {
                    // a.k.a NetUpdateName ;)
                    RepNotifyFuncName = _Buffer.ReadNameReference();
                    Record(nameof(RepNotifyFuncName), RepNotifyFuncName);
                }

                return;
            }
#endif
            // Appears to be a UE2.5 feature, it is not present in UE2 builds with no custom LicenseeVersion
            // Albeit DeusEx indicates otherwise?
            if ((HasPropertyFlag(PropertyFlagsLO.EditorData) &&
                 (Package.Build == BuildGeneration.UE2_5
                  || Package.Build == BuildGeneration.AGP
                  || Package.Build == BuildGeneration.Flesh))
                // No property flag
                || Package.Build == BuildGeneration.Vengeance
#if LSGAME
                || (Package.Build == UnrealPackage.GameBuild.BuildName.LSGame &&
                    Package.LicenseeVersion >= 3)
#endif
#if DEVASTATION
                || Package.Build == UnrealPackage.GameBuild.BuildName.Devastation
#endif
               )
            {
                // May represent a tooltip/comment in some games. Usually in the form of a quoted string, sometimes as a double-flash comment or both.
                EditorDataText = _Buffer.ReadText();
                Record(nameof(EditorDataText), EditorDataText);
            }
#if SPELLBORN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Spellborn)
            {
                if (_Buffer.Version < 157)
                {
                    throw new NotSupportedException("< 157 Spellborn packages are not supported");

                    if (133 < _Buffer.Version)
                    {
                        // idk
                    }

                    if (134 < _Buffer.Version)
                    {
                        int unk32 = _Buffer.ReadInt32();
                        Record("Unknown:Spellborn", unk32);
                    }
                }
                else
                {
                    uint replicationFlags = _Buffer.ReadUInt32();
                    Record(nameof(replicationFlags), replicationFlags);
                }
            }
#endif
        }

        protected override bool CanDisposeBuffer()
        {
            return true;
        }

        #endregion

        #region Methods

        public bool HasPropertyFlag(uint flag)
        {
            return ((uint)PropertyFlags & flag) != 0;
        }

        public bool HasPropertyFlag(PropertyFlagsLO flag)
        {
            return ((uint)(PropertyFlags & 0x00000000FFFFFFFFU) & (uint)flag) != 0;
        }

        public bool HasPropertyFlag(PropertyFlagsHO flag)
        {
            return ((PropertyFlags >> 32) & (uint)flag) != 0;
        }

        public bool IsParm()
        {
            return HasPropertyFlag(PropertyFlagsLO.Parm);
        }

        public virtual string GetFriendlyInnerType()
        {
            return string.Empty;
        }

        #endregion
    }
}
