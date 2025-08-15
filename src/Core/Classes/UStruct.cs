using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UELib.Branch;
using UELib.Branch.UE2.Eon;
using UELib.Core.Tokens;
using UELib.Flags;
using UELib.ObjectModel.Annotations;
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

        /// <summary>
        ///     The serialized default script properties for this struct (or class), if any.
        /// </summary>
        protected DefaultPropertiesCollection? DefaultProperties { get; set; }

        #region Serialized Members

#pragma warning disable CA1051

        /// <summary>
        ///     The (partially processed) UnrealScript text for this struct (.uc class), if any.
        /// </summary>
        [StreamRecord]
        public UTextBuffer? ScriptText { get; set; }

        /// <summary>
        ///     The processed UnrealScript text for this struct (.uc class), if any.
        /// </summary>
        [StreamRecord]
        public UTextBuffer? ProcessedText { get; set; }

        /// <summary>
        ///     The C++ text for this struct (cpptext or structcpptext), if any.
        /// </summary>
        [StreamRecord]
        public UTextBuffer? CppText { get; set; }

        /// <summary>
        ///     The user-friendly name for this struct (usually the symbolic name of an operator function such as +=)
        /// </summary>
        [StreamRecord]
        public UName FriendlyName { get; set; } = UnrealName.None;

        /// <summary>
        ///     The compiled line number of the struct declaration in the source code.
        /// </summary>
        [StreamRecord]
        public int Line { get; internal set; }

        /// <summary>
        ///     The compiled text position of the struct declaration in the source code.
        /// </summary>
        [StreamRecord]
        public int TextPos { get; internal set; }

        /// <summary>
        ///     The struct flags for this struct.
        /// </summary>
        [StreamRecord]
        public UnrealFlags<StructFlag> StructFlags { get; set; }

        [Obsolete("Use FindField and EnumerateFields respectively.")]
        protected UField? Children => _Children;

        [StreamRecord]
        private UField? _Children;

        [Obsolete("Use StorageScriptSize instead.")]
        protected int DataScriptSize => StorageScriptSize;

        /// <summary>
        ///     The serialized size of the byte-code script in storage (on disk)
        /// </summary>
        [StreamRecord]
        private int StorageScriptSize { get; set; }

        /// <summary>
        ///     The serialized size of the byte-code script in memory.
        ///     (each index is aligned to 4-bytes and each object to 4 or 8 bytes depending on platform)
        /// </summary>
        [StreamRecord]
        private int MemoryScriptSize { get; set; }

#pragma warning restore CA1051

        #endregion

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

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.SuperReferenceMovedToUStruct)
            {
                Super = stream.ReadObject<UStruct>();
                stream.Record(nameof(Super), Super);

                // Weird bug in UDK (805 and 810), where the Super is set to Commandlet
                // Set to null to prevent an infinite loop in EnumerateSuper
                if (Super != null && Name == UnrealName.Object && Super.Name == UnrealName.Commandlet)
                {
                    Super = null;
                }
            }
#if BATMAN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                goto skipScriptText;
            }
#endif
#if BORDERLANDS
            // Swapped order...
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands)
            {
                _Children = stream.ReadObject<UField?>();
                stream.Record(nameof(_Children), _Children);

                ScriptText = stream.ReadObject<UTextBuffer?>();
                stream.Record(nameof(ScriptText), ScriptText);

                // FIXME: another 2x32 uints here (IsConsoleCooked)

                goto skipChildren;
            }
#endif
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                Super = stream.ReadObject<UStruct?>();
                stream.Record(nameof(Super), Super);
            }
#endif
            if (stream.ContainsEditorOnlyData() && stream.UE4Version < 117)
            {
                ScriptText = stream.ReadObject<UTextBuffer?>();
                stream.Record(nameof(ScriptText), ScriptText);
            }

        skipScriptText:
            _Children = stream.ReadObject<UField?>();
            stream.Record(nameof(_Children), _Children);
        skipChildren:
#if BATMAN
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
            {
                goto serializeByteCode;
            }
#endif
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent)
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
                    property = EonEngineBranch.DeserializeFProperty<UProperty>(stream);
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

                if (stream.Version >= 133)
                {
                    goto skipFriendlyName;
                }
            }
#endif
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando && _Children != null)
            {
                var tail = _Children;
                while (true)
                {
                    var nextField = stream.ReadObject<UField?>();
                    stream.Record(nameof(nextField), nextField);

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
            if (stream.Version < (uint)PackageObjectLegacyVersion.MovedFriendlyNameToUFunction)
            {
                FriendlyName = stream.ReadName();
                stream.Record(nameof(FriendlyName), FriendlyName);

                // Debug.Assert here, because we can work without a FriendlyName, but it is not expected.
                Debug.Assert(FriendlyName.IsNone() == false, "FriendlyName should not be 'None'");
            }

        skipFriendlyName:
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (stream.LicenseeVersion >= 17)
                {
                    // Back-ported CppText
                    CppText = stream.ReadObject<UTextBuffer?>();
                    stream.Record(nameof(CppText), CppText);

                    var dnfTextObj2 = stream.ReadObject();
                    stream.Record(nameof(dnfTextObj2), dnfTextObj2);

                    stream.ReadArray(out UArray<UObject> dnfIncludeTexts);
                    stream.Record(nameof(dnfIncludeTexts), dnfIncludeTexts);
                }

                if (stream.LicenseeVersion >= 2)
                {
                    // Bool?
                    byte dnfByte = stream.ReadByte();
                    stream.Record(nameof(dnfByte), dnfByte);

                    var dnfName = stream.ReadName();
                    stream.Record(nameof(dnfName), dnfName);
                }

                goto lineData;
            }
#endif
            // Standard, but UT2004' derived games do not include this despite reporting version 128+
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedCppTextToUStruct &&
                stream.UE4Version < 117 &&
                stream.ContainsEditorOnlyData() &&
                stream.Build != BuildGeneration.UE2_5 &&
                stream.Build != BuildGeneration.AGP)
            {
                CppText = stream.ReadObject<UTextBuffer?>();
                stream.Record(nameof(CppText), CppText);
            }
#if VENGEANCE
            // Introduced with BioShock
            if (stream.Build == BuildGeneration.Vengeance &&
                stream.LicenseeVersion >= 29)
            {
                var vengeanceUnknownObject = stream.ReadObject();
                stream.Record(nameof(vengeanceUnknownObject), vengeanceUnknownObject);
                if (vengeanceUnknownObject != null)
                {
                    LibServices.Debug(GetReferencePath() + ":vengeanceUnknownObject", vengeanceUnknownObject);
                }
            }
#endif
            // UE3 or UE2.5 build, it appears that StructFlags may have been merged from an early UE3 build.
            // UT2004 reports version 26, and BioShock version 2
            if ((stream.Build == BuildGeneration.UE2_5 && stream.LicenseeVersion >= 26) ||
                (stream.Build == BuildGeneration.AGP && stream.LicenseeVersion >= 17) ||
                (stream.Build == BuildGeneration.Vengeance && stream.LicenseeVersion >= 2)
#if SG1
                // Same offset and version check as CppText (120) probably an incorrectly back-ported feature.
                || (stream.Build == UnrealPackage.GameBuild.BuildName.SG1_TA && stream.Version >= 120)
#endif
               )
            {
                StructFlags = stream.ReadFlags32<StructFlag>();
                stream.Record(nameof(StructFlags), StructFlags);
            }
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance &&
                stream.LicenseeVersion >= 14)
            {
                ProcessedText = stream.ReadObject<UTextBuffer?>();
                stream.Record(nameof(ProcessedText), ProcessedText);
            }
#endif
        lineData:
            if (stream.ContainsEditorOnlyData() &&
                stream.UE4Version < 117)
            {
                Line = stream.ReadInt32();
                stream.Record(nameof(Line), Line);
                TextPos = stream.ReadInt32();
                stream.Record(nameof(TextPos), TextPos);
            }

            // FIXME: Version >= 130 (According to SWRepublic && Version < 200 (RoboHordes, EndWar, R6Vegas)
            // Guarded with SWRepublicCommando, because it is the only supported game that has this particular change.
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando &&
                stream.Version >= 130 &&
                stream.Version < (uint)PackageObjectLegacyVersion.RemovedMinAlignmentFromUStruct)
            {
                uint minAlignment = stream.ReadUInt32(); // v60
                stream.Record(nameof(minAlignment), minAlignment);
            }
#if UNREAL2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                // Always zero in all the Core.u structs
                int unknownInt32 = stream.ReadInt32();
                stream.Record("Unknown:Unreal2", unknownInt32);
                if (unknownInt32 != 0) LibServices.Debug(GetReferencePath() + ":unknownInt32", unknownInt32);
            }
#endif
#if TRANSFORMERS
            if (stream.Build == BuildGeneration.HMS)
            {
                int transformersEndLine = stream.ReadInt32();
                stream.Record(nameof(transformersEndLine), transformersEndLine);
            }
#endif
#if SPLINTERCELLX
            // Probably a backport mistake, this should appear before Line and TextPos
            if (stream.Build == BuildGeneration.SCX &&
                stream.LicenseeVersion >= 39)
            {
                CppText = stream.ReadObject<UTextBuffer?>();
                stream.Record(nameof(CppText), CppText);
            }
#endif
#if LEAD
            // Same as SCX
            if (stream.Build == BuildGeneration.Lead)
            {
                CppText = stream.ReadObject<UTextBuffer?>(); // v34
                stream.Record(nameof(CppText), CppText);

                string v64 = stream.ReadString();
                stream.Record(nameof(v64), v64);
                if (v64 != string.Empty) LibServices.Debug(GetReferencePath() + ":v64", v64);
            }
#endif
        serializeByteCode:
            MemoryScriptSize = stream.ReadInt32();
            stream.Record(nameof(MemoryScriptSize), MemoryScriptSize);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDataScriptSizeToUStruct)
            {
                StorageScriptSize = stream.ReadInt32();
                stream.Record(nameof(StorageScriptSize), StorageScriptSize);
            }

            ScriptOffset = stream.Position;

            // Code Statements
            if (MemoryScriptSize > 0)
            {
                Script = new UByteCodeScript(this, MemoryScriptSize, StorageScriptSize);

                if (StorageScriptSize > 0)
                {
                    stream.Skip(StorageScriptSize);
                }
                else
                {
                    Script.Deserialize(stream);
                }

                // Fix the recording position
                stream.ConformRecordPosition();
                ScriptSize = (int)(stream.Position - ScriptOffset);

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
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                //_Buffer.ReadByte();
            }
#endif
            // StructDefaults in RoboHordes (200)
            if (stream.Version >= (uint)PackageObjectLegacyVersion.UE3
                && GetType() == typeof(UStruct))
            {
                DefaultProperties = DeserializeScriptProperties(stream, this);
                Properties = DefaultProperties;
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.SuperReferenceMovedToUStruct)
            {
                stream.WriteObject(Super);
            }
#if BORDERLANDS
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands)
            {
                stream.WriteObject(_Children);
                stream.WriteObject(ScriptText);

                // Skipping 2x32 uints (IsConsoleCooked)
                if (stream.ContainsEditorOnlyData())
                {
                    throw new NotSupportedException("This package version is not supported!");
                }

                return;
            }
#endif
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando)
            {
                stream.WriteObject(Super);
            }
#endif
            if (stream.ContainsEditorOnlyData() && stream.UE4Version < 117)
            {
                stream.WriteObject(ScriptText);
            }

            stream.WriteObject(_Children);
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent)
            {
                foreach (var property in EnumerateFields<UProperty>())
                {
                    EonEngineBranch.SerializeFProperty(stream, property);
                }

                // End
                stream.WriteObject<UObject>(null);

                if (stream.Version >= 133)
                {
                    goto skipFriendlyName;
                }
            }
#endif
#if SWRepublicCommando
            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando && _Children != null)
            {
                var tail = _Children;
                while (true)
                {
                    var nextField = tail.NextField;
                    stream.WriteObject(nextField);

                    if (nextField == null)
                    {
                        break;
                    }

                    tail = nextField;
                }
            }
#endif
            if (stream.Version < (uint)PackageObjectLegacyVersion.MovedFriendlyNameToUFunction)
            {
                Contract.Assert(FriendlyName.IsNone() == false, "FriendlyName should not be 'None'");
                stream.WriteName(FriendlyName);
            }

        skipFriendlyName:
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                if (stream.LicenseeVersion >= 17)
                {
                    stream.WriteObject(CppText);
                    // dnfTextObj2 and dnfIncludeTexts omitted for brevity
                    throw new NotSupportedException("This package version is not supported!");
                }

                if (stream.LicenseeVersion >= 2)
                {
                    // dnfByte and dnfName omitted for brevity
                    throw new NotSupportedException("This package version is not supported!");
                }

                // No further serialization
                return;
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedCppTextToUStruct &&
                stream.UE4Version < 117 &&
                stream.ContainsEditorOnlyData() &&
                stream.Build != BuildGeneration.UE2_5 &&
                stream.Build != BuildGeneration.AGP)
            {
                stream.WriteObject(CppText);
            }
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance &&
                stream.LicenseeVersion >= 29)
            {
                // vengeanceUnknownObject omitted for brevity
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
            if ((stream.Build == BuildGeneration.UE2_5 && stream.LicenseeVersion >= 26) ||
                (stream.Build == BuildGeneration.AGP && stream.LicenseeVersion >= 17) ||
                (stream.Build == BuildGeneration.Vengeance && stream.LicenseeVersion >= 2)
#if SG1
                || (stream.Build == UnrealPackage.GameBuild.BuildName.SG1_TA && stream.Version >= 120)
#endif
               )
            {
                stream.Write((uint)StructFlags);
            }
#if VENGEANCE
            if (stream.Build == BuildGeneration.Vengeance &&
                stream.LicenseeVersion >= 14)
            {
                stream.WriteObject(ProcessedText);
            }
#endif
            if (stream.ContainsEditorOnlyData() &&
                stream.UE4Version < 117)
            {
                stream.Write(Line);
                stream.Write(TextPos);
            }

            if (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando &&
                stream.Version >= 130 &&
                stream.Version < (uint)PackageObjectLegacyVersion.RemovedMinAlignmentFromUStruct)
            {
                // minAlignment omitted for brevity
                throw new NotSupportedException("This package version is not supported!");
            }
#if UNREAL2
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Unreal2)
            {
                // unknownInt32 omitted for brevity
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
#if TRANSFORMERS
            if (stream.Build == BuildGeneration.HMS)
            {
                stream.Write(0); // transformersEndLine
            }
#endif
#if SPLINTERCELLX
            if (stream.Build == BuildGeneration.SCX &&
                stream.LicenseeVersion >= 39)
            {
                stream.WriteObject(CppText);
            }
#endif
#if LEAD
            if (stream.Build == BuildGeneration.Lead)
            {
                stream.WriteObject(CppText);
                stream.WriteString(""); // v64 omitted for brevity
            }
#endif
            long storageScriptPosition = stream.Position;
            stream.Write(0); // MemoryScriptSize
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDataScriptSizeToUStruct)
            {
                stream.Write(0); // StorageScriptSize
            }

            ScriptOffset = stream.Position;
            if (Script != null)
            {
                Script.Serialize(stream);

                // Go back and write down the correct sizes.
                using (stream.Peek(storageScriptPosition))
                {
                    stream.Write(Script.MemorySize);
                    if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedDataScriptSizeToUStruct)
                    {
                        stream.Write(Script.StorageSize);
                    }
                }
            }
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                // _Buffer.ReadByte();
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.UE3
                && GetType() == typeof(UStruct))
            {
                SerializeScriptProperties(stream, this, DefaultProperties);
                Properties = DefaultProperties;
            }
        }

        protected override bool CanDisposeBuffer()
        {
            return base.CanDisposeBuffer() && Script == null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DefaultPropertiesCollection DeserializeScriptProperties(
            IUnrealStream stream,
            UObject? tagSource
        )
        {
            return DeserializeScriptProperties(stream, this, tagSource);
        }

        public static DefaultPropertiesCollection DeserializeScriptProperties(
            IUnrealStream stream,
            UStruct? propertySource,
            UObject? tagSource
        )
        {
            DefaultPropertiesCollection properties = [];

            while (true)
            {
                var scriptProperty = DeserializeNextScriptProperty(stream, propertySource, tagSource);
                if (scriptProperty == null)
                {
                    break;
                }

                properties.Add(scriptProperty);

                scriptProperty.Deserialize(stream); // tag
            }

            return properties;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SerializeScriptProperties(
            IUnrealStream stream,
            UObject? tagSource,
            DefaultPropertiesCollection? properties
        )
        {
            SerializeScriptProperties(stream, this, tagSource, properties);
        }

        public static void SerializeScriptProperties(
            IUnrealStream stream,
            UStruct? propertySource,
            UObject? tagSource,
            DefaultPropertiesCollection? properties
        )
        {
            if (properties != null)
            {
                foreach (var tag in properties)
                {
                    SerializeNextScriptProperty(stream, tag, propertySource, tagSource);
                    tag.Serialize(stream); // tag and value
                }
            }

            SerializeNextScriptProperty(stream, null, propertySource, tagSource);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SerializeNextScriptProperty(
            IUnrealStream stream,
            UDefaultProperty? scriptProperty,
            UStruct? propertySource,
            UObject? tagSource)
        {
#if BATMAN
            if (stream.Build == BuildGeneration.RSS)
            {
                if (scriptProperty != null)
                {
                    var transformedType = scriptProperty.Type;
                    if (transformedType == PropertyType.Vector)
                    {
                        transformedType = (PropertyType)11;
                    }

                    stream.Write((uint)transformedType);
                }
                else
                {
                    stream.Write((uint)PropertyType.None);
                }

                return;
            }
#endif
            stream.Write(scriptProperty?.Name ?? UnrealName.None);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static UDefaultProperty? DeserializeNextScriptProperty(
            IUnrealStream stream,
            UStruct? propertySource,
            UObject? tagSource
        )
        {
            UDefaultProperty scriptProperty;
#if BATMAN
            if (stream.Build == BuildGeneration.RSS)
            {
                var type = (PropertyType)stream.ReadInt16();
                stream.Record(nameof(type), type.ToString());

                return type != PropertyType.None
                    ? CreateScriptProperty(UnrealName.None, type)
                    : null;
            }
#endif
            // Read the property name here, instead of in the script property, so that, we can skip the tag if it's not needed.
            var name = stream.ReadName();
            stream.Record(nameof(name), name);

            return !name.IsNone()
                ? CreateScriptProperty(name, PropertyType.None)
                : null;

            UDefaultProperty CreateScriptProperty(UName propertyName, PropertyType propertyType)
            {
                UProperty property = null;
                var destPtr = IntPtr.Zero;

                UPropertyTag? tag = null;

                if (tagSource != null && tagSource.InternalFlags.HasFlag(InternalClassFlags.LinkTaggedProperties))
                {
                    property = propertySource?.FindProperty<UProperty>(propertyName);
                    if (property != null)
                    {
                        tag = new UPropertyTag(property);
                    }
                }

                if (tagSource != null && tagSource.InternalFlags.HasFlag(InternalClassFlags.LinkAttributedProperties))
                {
                    // TODO: Source generator, map by name offsets.

                    var internalClassType = tagSource.GetType();
                    var unrealMembers = internalClassType.FindMembers(
                        MemberTypes.Field | MemberTypes.Property,
                        BindingFlags.GetField,
                        (info, criteria) =>
                        {
                            var attr = info.GetCustomAttribute<UnrealPropertyAttribute>();
                            if (attr == null)
                            {
                                return false;
                            }

                            var scriptName = attr.Name ?? info.Name;
                            return criteria == scriptName;
                        },
                        propertyName
                    );

                    var fieldMember = unrealMembers.FirstOrDefault();
                    destPtr = fieldMember != null
                        ? Marshal.UnsafeAddrOfPinnedArrayElement([fieldMember], 0)
                        : IntPtr.Zero;
                }

                UPropertyTag propertyTag = tag ?? new UPropertyTag(propertyName, propertyType);

                return property == null
                    ? new UDefaultProperty(tagSource, propertySource, ref propertyTag, destPtr)
                    : new UDefaultProperty(tagSource, property, ref propertyTag, destPtr);
            }
        }

        [Obsolete("Deprecated", true)]
        protected void FindChildren()
        {
            throw new NotImplementedException("Use EnumerateFields");
        }

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
        public bool HasAnyStructFlags(ulong flag) => (StructFlags & flag) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPureStruct()
        {
            return GetType() == typeof(UStruct) || this is UScriptStruct;
        }
    }
}
