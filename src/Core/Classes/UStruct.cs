using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Branch.UE2.Eon;
using UELib.Core.Tokens;
using UELib.Flags;
using UELib.Services;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UStruct/Core.Struct
    /// </summary>
    [UnrealRegisterClass]
    public partial class UStruct : UField
    {
        [Obsolete] public const int VInterfaceClass = 222;

        /// <summary>
        ///     The deserialized offset to the script code in storage relative to the offset of the object.
        /// </summary>
        public long ScriptOffset { get; private set; }

        /// <summary>
        ///     The deserialized size of the script in storage.
        /// </summary>
        public int ScriptSize { get; private set; }

        [Obsolete("Construct a new one as needed.", true)]
        public UByteCodeDecompiler? ByteCodeManager => null;

        /// <summary>
        ///     The script for this struct (replication block, function script or state block), if any.
        /// </summary>
        public UByteCodeScript? Script { get; set; }

        #region Serialized Members
#pragma warning disable CA1051

        public UTextBuffer? ScriptText { get; set; }
        public UTextBuffer? ProcessedText { get; set; }
        public UTextBuffer? CppText { get; set; }
        public UName FriendlyName { get; set; } = UnrealName.None;

        public int Line { get; internal set; }
        public int TextPos { get; internal set; }

        public UnrealFlags<StructFlag> StructFlags
        {
            get => _StructFlags;
            set => _StructFlags = value;
        }

        private UField? _Children;
        private UnrealFlags<StructFlag> _StructFlags;

        [Obsolete("Use FindField and EnumerateFields respectively.")]
        protected UField? Children => _Children;

        [Obsolete("Use StorageScriptSize instead.")]
        protected int DataScriptSize => StorageScriptSize;

        /// <summary>
        ///     The serialized size of the byte-code script in storage (on disk)
        /// </summary>
        private int StorageScriptSize { get; set; }

        /// <summary>
        ///     The serialized size of the byte-code script in memory.
        ///     (each index is aligned to 4-bytes and each object to 4 or 8 bytes depending on platform)
        /// </summary>
        private int MemoryScriptSize { get; set; }

#pragma warning restore CA1051
        #endregion

        #region Script Members

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UConst> Constants => EnumerateFields<UConst>();

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UEnum> Enums => EnumerateFields<UEnum>();

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UStruct> Structs => EnumerateFields<UStruct>().Where(obj => obj.IsPureStruct());

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UProperty> Variables => EnumerateFields<UProperty>();

        [Obsolete("Use EnumerateFields")]
        public IEnumerable<UProperty> Locals => EnumerateFields<UProperty>().Where(prop => !prop.IsParm());

        #endregion

        /// <summary>
        ///     The start of the script code in the object's buffer.
        /// </summary>
        public long ScriptOffset { get; private set; }

        /// <summary>
        ///     The size of the script in storage.
        /// </summary>
        public int ScriptSize { get; private set; }

        public UByteCodeDecompiler? ByteCodeManager { get; private set; }

        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.SuperReferenceMovedToUStruct)
            {
                Super = _Buffer.ReadObject<UStruct>();
                Record(nameof(Super), Super);

                // Weird bug in UDK (805 and 810), where the Super is set to Commandlet
                // Set to null to prevent an infinite loop in EnumerateSuper
                if (Super != null && Name == UnrealName.Object && Super.Name == UnrealName.Commandlet)
                {
                    Super = null;
                }
            }
#if BATMAN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                goto skipScriptText;
            }
#endif
#if BORDERLANDS
            // Swapped order...
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands)
            {
                _Children = _Buffer.ReadObject<UField>();
                Record(nameof(_Children), _Children);

                ScriptText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(ScriptText), ScriptText);

                // FIXME: another 2x32 uints here (IsConsoleCooked)

                goto skipChildren;
            }
#endif
#if SWRepublicCommando
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                Super = _Buffer.ReadObject<UStruct>();
                Record(nameof(Super), Super);
            }
#endif
            if (!Package.IsConsoleCooked() && _Buffer.UE4Version < 117)
            {
                ScriptText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(ScriptText), ScriptText);
            }

        skipScriptText:
            _Children = _Buffer.ReadObject<UField>();
            Record(nameof(_Children), _Children);
        skipChildren:
#if BATMAN
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                goto serializeByteCode;
            }
#endif
#if ADVENT
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Advent)
            {
                // Preload fields so we can tinker with NextField.
                for (var field = _Children; field != null; field = field.NextField)
                {
                    if (field is { DeserializationState: 0 })
                    {
                        field.Load();
                    }
                }

                var tail = EnumerateFields().LastOrDefault();
                UProperty? property;

                do
                {
                    property = EonEngineBranch.SerializeFProperty<UProperty>(_Buffer);
                    if (tail == null)
                    {
                        _Children = property;
                    }
                    else
                    {
                        tail.NextField = property;
                    }

                    tail = property;
                } while (property != null);

                if (_Buffer.Version >= 133)
                {
                    goto skipFriendlyName;
                }
            }
#endif
#if SWRepublicCommando
            if (Package.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando && _Children != null)
            {
                var tail = _Children;
                while (true)
                {
                    var nextField = _Buffer.ReadObject<UField?>();
                    Record(nameof(nextField), nextField);

                    if (nextField == null)
                    {
                        break;
                    }

                    tail.NextField = nextField;
                    tail = nextField;
                }
            }
#endif
            // Moved to UFunction in UE3
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.MovedFriendlyNameToUFunction)
            {
                FriendlyName = _Buffer.ReadName();
                Record(nameof(FriendlyName), FriendlyName);
            }
        skipFriendlyName:
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (_Buffer.LicenseeVersion >= 17)
                {
                    // Back-ported CppText
                    CppText = _Buffer.ReadObject<UTextBuffer>();
                    Record(nameof(CppText), CppText);

                    var dnfTextObj2 = _Buffer.ReadObject();
                    Record(nameof(dnfTextObj2), dnfTextObj2);

                    _Buffer.ReadArray(out UArray<UObject> dnfIncludeTexts);
                    Record(nameof(dnfIncludeTexts), dnfIncludeTexts);
                }

                if (_Buffer.LicenseeVersion >= 2)
                {
                    // Bool?
                    byte dnfByte = _Buffer.ReadByte();
                    Record(nameof(dnfByte), dnfByte);

                    var dnfName = _Buffer.ReadName();
                    Record(nameof(dnfName), dnfName);
                }

                goto lineData;
            }
#endif
            // Standard, but UT2004' derived games do not include this despite reporting version 128+
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedCppTextToUStruct &&
                _Buffer.UE4Version < 117 &&
                !Package.IsConsoleCooked() &&
                (Package.Build != BuildGeneration.UE2_5 &&
                 Package.Build != BuildGeneration.AGP))
            {
                CppText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(CppText), CppText);
            }
#if VENGEANCE
            // Introduced with BioShock
            if (Package.Build == BuildGeneration.Vengeance &&
                _Buffer.LicenseeVersion >= 29)
            {
                var vengeanceUnknownObject = _Buffer.ReadObject();
                Record(nameof(vengeanceUnknownObject), vengeanceUnknownObject);
            }
#endif
            // UE3 or UE2.5 build, it appears that StructFlags may have been merged from an early UE3 build.
            // UT2004 reports version 26, and BioShock version 2
            if ((Package.Build == BuildGeneration.UE2_5 && _Buffer.LicenseeVersion >= 26) ||
                (Package.Build == BuildGeneration.AGP && _Buffer.LicenseeVersion >= 17) ||
                (Package.Build == BuildGeneration.Vengeance && _Buffer.LicenseeVersion >= 2)
#if SG1
                // Same offset and version check as CppText (120) probably an incorrectly back-ported feature.
                || (Package.Build == UnrealPackage.GameBuild.BuildName.SG1_TA && _Buffer.Version >= 120)
#endif
               )
            {
                _Buffer.Read(out _StructFlags);
                Record(nameof(StructFlags), StructFlags);
            }
#if VENGEANCE
            if (Package.Build == BuildGeneration.Vengeance &&
                _Buffer.LicenseeVersion >= 14)
            {
                ProcessedText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(ProcessedText), ProcessedText);
            }
#endif
        lineData:
            if (!Package.IsConsoleCooked() &&
                _Buffer.UE4Version < 117)
            {
                Line = _Buffer.ReadInt32();
                Record(nameof(Line), Line);
                TextPos = _Buffer.ReadInt32();
                Record(nameof(TextPos), TextPos);
            }

            // FIXME: Version >= 130 (According to SWRepublic && Version < 200 (RoboHordes, EndWar, R6Vegas)
            // Guarded with SWRepublicCommando, because it is the only supported game that has this particular change.
            if (Package.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando &&
                _Buffer.Version >= 130 &&
                _Buffer.Version < 200)
            {
                uint minAlignment = _Buffer.ReadUInt32(); // v60
                Record(nameof(minAlignment), minAlignment);
            }
#if UNREAL2
            if (Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                // Always zero in all the Core.u structs
                int unknownInt32 = _Buffer.ReadInt32();
                Record("Unknown:Unreal2", unknownInt32);
            }
#endif
#if TRANSFORMERS
            if (Package.Build == BuildGeneration.HMS)
            {
                int transformersEndLine = _Buffer.ReadInt32();
                // The line where the struct's code body ends.
                Record(nameof(transformersEndLine), transformersEndLine);
            }
#endif
#if SPLINTERCELLX
            // Probably a backport mistake, this should appear before Line and TextPos
            if (Package.Build == BuildGeneration.SCX &&
                _Buffer.LicenseeVersion >= 39)
            {
                CppText = _Buffer.ReadObject<UTextBuffer>();
                Record(nameof(CppText), CppText);
            }
#endif
#if LEAD
            // Same as SCX
            if (Package.Build == BuildGeneration.Lead)
            {
                CppText = _Buffer.ReadObject<UTextBuffer>(); // v34
                Record(nameof(CppText), CppText);

                string v64 = _Buffer.ReadString();
                Record(nameof(v64), v64);
            }
#endif
        serializeByteCode:
            MemoryScriptSize = _Buffer.ReadInt32();
            Record(nameof(MemoryScriptSize), MemoryScriptSize);

            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.AddedDataScriptSizeToUStruct)
            {
                StorageScriptSize = _Buffer.ReadInt32();
                Record(nameof(StorageScriptSize), StorageScriptSize);
            }

            ScriptOffset = _Buffer.Position;

            // Code Statements
            if (MemoryScriptSize > 0)
            {
                Script = new UByteCodeScript(this, MemoryScriptSize, StorageScriptSize);

                if (StorageScriptSize > 0)
                {
                    _Buffer.Skip(StorageScriptSize);
                }
                else
                {
                    Script.Deserialize(_Buffer);
                }

                // Fix the recording position
                _Buffer.ConformRecordPosition();
                ScriptSize = (int)(_Buffer.Position - ScriptOffset);

                if (StorageScriptSize > 0)
                {
                    LibServices.LogService.SilentAssert(ScriptSize == StorageScriptSize, "StorageScriptSize mismatch");
                }
                else
                {
                    StorageScriptSize = ScriptSize;
                }
            }
#if DNF
            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                //_Buffer.ReadByte();
            }
#endif
            // StructDefaults in RoboHordes (200)
            if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.UE3
                && GetType() == typeof(UStruct))
            {
                DeserializeProperties(_Buffer);
            }
        }

        protected override bool CanDisposeBuffer()
        {
            return base.CanDisposeBuffer() && ByteCodeManager == null;
        }

        [Obsolete("Deprecated", true)]
        protected void FindChildren()
        {
            throw new NotImplementedException("Use EnumerateFields");
        }

        #endregion

        /// <summary>
        ///     Enumerates all fields in this struct.
        /// </summary>
        /// <returns>the enumerated field.</returns>
        public IEnumerable<UField> EnumerateFields()
        {
            for (var field = _Children; field != null; field = field.NextField)
            {
                yield return field;
            }
        }

        /// <summary>
        ///     Enumerates all fields of a specific type in this struct.
        /// </summary>
        /// <typeparam name="T">the field type to limit the enumeration to.</typeparam>
        /// <returns>the enumerated field.</returns>
        public IEnumerable<T> EnumerateFields<T>()
            where T : UField
        {
            for (var field = _Children; field != null; field = field.NextField)
            {
                if (field is T tField)
                {
                    yield return tField;
                }
            }
        }

        /// <summary>
        ///     Looks for a field with a matching name in this struct and its super structs.
        /// </summary>
        /// <param name="name">the name of the field.</param>
        /// <typeparam name="T">the type to limit the search to.</typeparam>
        /// <returns>the field with a matching name and type.</returns>
        public T? FindField<T>(in UName name) where T : UField
        {
            foreach (var super in EnumerateSuper(this))
            {
                foreach (var field in super.EnumerateFields<T>())
                {
                    if (field.Name == name)
                    {
                        return field;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Looks for a property with a matching name in this struct and its super structs.
        /// </summary>
        /// <param name="name">the name of the property.</param>
        /// <typeparam name="T">the type to limit the search to.</typeparam>
        /// <returns>the property with a matching name and type.</returns>
        public T? FindProperty<T>(in UName name) where T : UProperty
        {
            foreach (var super in EnumerateSuper(this))
            {
                foreach (var field in super.EnumerateFields<T>())
                {
                    if (field.Name == name)
                    {
                        return field;
                    }
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TokenFactory GetTokenFactory()
        {
            return Package.Branch.GetTokenFactory(Package);
        }

        [Obsolete("Use StructFlags directly")]
        public bool HasStructFlag(StructFlags flag)
        {
            return (StructFlags & (uint)flag) != 0;
        }

        internal bool HasStructFlag(StructFlag flagIndex)
        {
            return StructFlags.HasFlag(Package.Branch.EnumFlagsMap[typeof(StructFlag)], flagIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPureStruct()
        {
            return GetType() == typeof(UStruct) || this is UScriptStruct;
        }
    }
}
