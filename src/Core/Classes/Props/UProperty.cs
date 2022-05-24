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

        public ushort ArrayDim { get; private set; }

        public ushort ElementSize { get; private set; }

        public ulong PropertyFlags { get; private set; }

#if XCOM2
        [CanBeNull] public UName ConfigName;
#endif

        [CanBeNull] public UName CategoryName;

        [Obsolete("See CategoryName")]
        public int CategoryIndex { get; }

        [CanBeNull] public UEnum ArrayEnum { get; private set; }

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
#if XIII
            if (Package.Build == UnrealPackage.GameBuild.BuildName.XIII)
            {
                ArrayDim = _Buffer.ReadUShort();
                Record("ArrayDim", ArrayDim);
                goto skipInfo;
            }
#endif
#if AA2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.AA2 && Package.LicenseeVersion > 7)
            {
                // Always 26125 (hardcoded in the assembly) 
                uint unknown = _Buffer.ReadUInt32();
                Record("Unknown:AA2", unknown);
            }
#endif
            int info = _Buffer.ReadInt32();
            ArrayDim = (ushort)(info & 0x0000FFFFU);
            Record("ArrayDim", ArrayDim);
            Debug.Assert(ArrayDim <= 2048, "Bad array dim");
            ElementSize = (ushort)(info >> 16);
            Record("ElementSize", ElementSize);
        skipInfo:

            PropertyFlags = Package.Version >= 220
                ? _Buffer.ReadUInt64()
                : _Buffer.ReadUInt32();
            Record("PropertyFlags", PropertyFlags);

#if XCOM2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.XCOM2WotC)
            {
                ConfigName = _Buffer.ReadNameReference();
                Record("ConfigName", ConfigName);
            }
#endif
#if THIEF_DS || DEUSEX_IW
            if (Package.Build.Generation == BuildGeneration.Thief)
            {
                // Property flags like CustomEditor, CustomViewer, ThiefProp, DeusExProp, NoTextExport, NoTravel
                uint deusFlags = _Buffer.ReadUInt32();
                Record(nameof(deusFlags), deusFlags);
            }
#endif
            if (!Package.IsConsoleCooked())
            {
                CategoryName = _Buffer.ReadNameReference();
                Record(nameof(CategoryName), CategoryName);

                if (Package.Version > 400)
                {
                    ArrayEnum = GetIndexObject(_Buffer.ReadObjectIndex()) as UEnum;
                    Record("ArrayEnum", ArrayEnum);
                }
                else
                {
#if THIEF_DS || DEUSEX_IW
                    if (Package.Build.Generation == BuildGeneration.Thief)
                    {
                        short deusInheritedOrRuntimeInstiantiated = _Buffer.ReadInt16();
                        Record(nameof(deusInheritedOrRuntimeInstiantiated), deusInheritedOrRuntimeInstiantiated);
                        short deusUnkInt16= _Buffer.ReadInt16();
                        Record(nameof(deusUnkInt16), deusUnkInt16);
                    }
#endif
                }
            }

            if (HasPropertyFlag(PropertyFlagsLO.Net))
            {
                RepOffset = _Buffer.ReadUShort();
                Record("RepOffset", RepOffset);
            }
#if VENGEANCE
            if (Package.Build.Generation == BuildGeneration.Vengeance)
            {
                var vengeanceEditComboType = _Buffer.ReadNameReference();
                Record(nameof(vengeanceEditComboType), vengeanceEditComboType);
                var vengeanceEditDisplay = _Buffer.ReadNameReference();
                Record(nameof(vengeanceEditDisplay), vengeanceEditDisplay);
            }
#endif
            // Appears to be a UE2X feature, it is not present in UE2 builds with no custom LicenseeVersion
            // Albeit DeusEx indicates otherwise?
            if ((HasPropertyFlag(PropertyFlagsLO.EditorData) && (Package.Build.Generation == BuildGeneration.UE2_5 || Package.Build.Generation == BuildGeneration.Thief))
                // No property flag
                || Package.Build.Generation == BuildGeneration.Vengeance)
            {
                // May represent a tooltip/comment in some games.
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