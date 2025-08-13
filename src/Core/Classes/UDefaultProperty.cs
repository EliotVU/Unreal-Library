using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UELib.Annotations;
using UELib.Branch;
using UELib.Services;
using UELib.Types;
using UELib.UnrealScript;

namespace UELib.Core
{
    /// <summary>
    ///     [Default]Properties values deserializer.
    /// </summary>
    public sealed class UDefaultProperty : IUnrealDecompilable
    {
        [Flags]
        public enum DeserializeFlags : byte
        {
            None = 0x00,
            WithinStruct = 0x01,
            WithinArray = 0x02
        }

        private const byte InfoTypeMask = 0x0F;
        private const byte InfoSizeMask = 0x70;
        private const byte InfoArrayIndexMask = 0x80;

        private const byte DoNotAppendName = 0x01;
        private const byte ReplaceNameMarker = 0x02;

        private readonly UObject _Container;
        private UStruct _Outer;
        private bool _RecordingEnabled = true;

        // Temporary solution, needed so we can lazy-load the property value.
        private UObjectStream _Buffer { get; set; }

        internal long _TagPosition { get; set; }
        internal long _PropertyValuePosition { get; set; }

        private byte _TempFlags { get; set; }

        /// <summary>
        ///     The deserialized and decompiled output.
        ///     Serves as a temporary workaround, don't rely on it.
        /// </summary>
        public string Value
        {
            get
            {
                if (_Value == null)
                {
                    _Value = DeserializeValue();
                }

                return _Value;
            }
        }

        private string? _Value = null;

        public string Decompile()
        {
            _TempFlags = 0x00;
            string value;
            _Buffer = _Container.LoadBuffer();
            try
            {
                value = DeserializeValue();
            }
            catch (Exception exception)
            {
                value = $"/* ERROR: {exception.GetType()} */";

                LibServices.LogService.SilentException(exception);
            }
            finally
            {
                _Container.MaybeDisposeBuffer();
            }

            // Array or Inlined object
            if ((_TempFlags & DoNotAppendName) != 0)
            // The tag handles the name etc on its own.
            {
                return value;
            }

            string name = Name;
            if (Type == PropertyType.DelegateProperty && _Container.Package.Version >= 100)
            {
                // Re-point the compiler-generated delegate property to the actual delegate function's name
                // e.g. __%delegateName%__Delegate -> %delegateName%
                if (name.EndsWith("__Delegate"))
                {
                    string normalizedDelegateName = name.Substring(2, name.Length - 12);
                    name = $"{normalizedDelegateName}";
                }
            }

            string expr = name;
            return ArrayIndex > 0
                ? $"{expr}{PropertyDisplay.FormatT3DElementAccess(ArrayIndex.ToString(), _Container.Package.Version)}={value}"
                : $"{expr}={value}";
        }

        private T? FindProperty<T>(out UStruct? outer)
            where T : UProperty
        {
            UProperty property = null;
            outer = _Outer ?? (UStruct)_Container.Class;
            Debug.Assert(outer != null, nameof(outer) + " != null");
            foreach (var super in outer.EnumerateSuper(outer))
            {
                foreach (var field in super
                             .EnumerateFields()
                             .OfType<UProperty>())
                {
                    if (field.Name != Name)
                    {
                        continue;
                    }

                    property = field;
                    outer = super;
                    break;
                }

                if (property == null)
                {
                    continue;
                }

                switch (property.Type)
                {
                    case PropertyType.StructProperty:
                        outer = ((UStructProperty)property).Struct;
                        break;

                    case PropertyType.ArrayProperty:
                        var arrayField = (UArrayProperty)property;
                        Debug.Assert(arrayField != null, "arrayField != null");

                        // May be null if deserialization failed
                        var arrayInnerField = arrayField.InnerProperty;
                        if (arrayInnerField?.Type == PropertyType.StructProperty)
                        {
                            outer = ((UStructProperty)arrayInnerField).Struct;
                        }

                        break;
                }

                break;
            }

            return (T)property;
        }

        [Conditional("BINARYMETADATA")]
        private void Record(string varName, object varObject = null)
        {
            if (_RecordingEnabled)
            {
                _Container.Record(varName, varObject);
            }
        }

        #region Serialized Members

        /// <summary>
        ///     Name of the UProperty.
        /// </summary>
        public UName Name;

        /// <summary>
        ///     See PropertyType enum in UnrealFlags.cs
        /// </summary>
        public PropertyType Type;

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct PropertyTypeData
        {
            /// <summary>
            ///     Name of the UStruct. If <see cref="UDefaultProperty.Type" /> is StructProperty.
            /// </summary>
            [FieldOffset(0)] public UName StructName; // Formerly known as "ItemName"

            /// <summary>
            ///     Name of the UEnum. If <see cref="UDefaultProperty.Type" /> is ByteProperty.
            /// </summary>
            [FieldOffset(0)] public UName EnumName;

            /// <summary>
            ///     Name of the UArray's inner type. If <see cref="UDefaultProperty.Type" /> is ArrayProperty.
            /// </summary>
            [FieldOffset(0)] public UName InnerTypeName;
        }

        private PropertyTypeData _TypeData;

        public UName? StructName => _TypeData.StructName;
        public UName? EnumName => _TypeData.EnumName;
        public UName? InnerTypeName => _TypeData.InnerTypeName;

        /// <summary>
        ///     The size in bytes of this tag's value.
        /// </summary>
        public int Size;

        /// <summary>
        ///     The element index of this tag e.g. consider this static array: "var Object Elements[2];"
        ///     This defines a static array of 2 elements which would have two tags with this field being the index into that
        ///     array.
        /// </summary>
        public int ArrayIndex = -1;

        /// <summary>
        ///     Value of the UBoolProperty. If Type equals BoolProperty.
        /// </summary>
        public bool? BoolValue;

        #endregion

        #region Constructors

        public UDefaultProperty(UObject owner, UStruct outer = null)
        {
            _Container = owner;
            _Outer = (outer ?? _Container as UStruct) ?? _Container.Outer as UStruct;

            Debug.Assert(owner.Buffer != null);
            _Buffer = owner.Buffer;
        }

        private int DeserializePackedSize(byte sizePack)
        {
            switch (sizePack)
            {
                case 0x00:
                    return 1;

                case 0x10:
                    return 2;

                case 0x20:
                    return 4;

                case 0x30:
                    return 12;

                case 0x40:
                    return 16;

                case 0x50:
                    return _Buffer.ReadByte();

                case 0x60:
                    return _Buffer.ReadUInt16();

                case 0x70:
                    return _Buffer.ReadInt32();

                default:
                    throw new NotImplementedException($"Unknown sizePack {sizePack}");
            }
        }

        private int DeserializeTagArrayIndexUE1()
        {
            int arrayIndex;
            byte b = _Buffer.ReadByte();
            if ((b & InfoArrayIndexMask) == 0)
            {
                arrayIndex = b;
            }
            else if ((b & 0xC0) == InfoArrayIndexMask)
            {
                byte c = _Buffer.ReadByte();
                arrayIndex = ((b & 0x7F) << 8) + c;
            }
            else
            {
                byte c = _Buffer.ReadByte();
                byte d = _Buffer.ReadByte();
                byte e = _Buffer.ReadByte();
                arrayIndex = ((b & 0x3F) << 24) + (c << 16) + (d << 8) + e;
            }

            return arrayIndex;
        }

        /// <returns>True if there are more property tags.</returns>
        public bool Deserialize()
        {
            _TagPosition = _Buffer.Position;
            if (DeserializeNextTag())
            {
                return false;
            }

            // Skip past the actual property value.
            _PropertyValuePosition = _Buffer.Position;
            _Buffer.Position = _PropertyValuePosition + Size;
            _Buffer.ConformRecordPosition();

            return true;
        }

        /// <returns>True if this is the last tag.</returns>
        private bool DeserializeNextTag()
        {
            if (_Buffer.Version < (uint)PackageObjectLegacyVersion.RefactoredPropertyTags)
            {
                return DeserializeTagUE1();
            }
#if BATMAN
            if (_Buffer.Package.Build == BuildGeneration.RSS && _Buffer.LicenseeVersion > 21)
            {
                return DeserializeTagByOffset();
            }
#endif
            return DeserializeTagUE3();
        }

        /// <returns>True if this is the last tag.</returns>
        private bool DeserializeTagUE1()
        {
            Name = _Buffer.ReadNameReference();
            Record(nameof(Name), Name);
            if (Name.IsNone())
            {
                return true;
            }

            byte info = _Buffer.ReadByte();
            Record(nameof(info), info);

            Type = (PropertyType)(byte)(info & InfoTypeMask);
            switch (Type)
            {
                case PropertyType.StructProperty:
                    _Buffer.Read(out _TypeData.StructName);
                    Record(nameof(_TypeData.StructName), _TypeData.StructName);
                    break;

                case PropertyType.ArrayProperty:
                    {
#if DNF
                        if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                            _Buffer.Version >= 124)
                        {
                            _Buffer.Read(out _TypeData.InnerTypeName);
                            Record(nameof(_TypeData.InnerTypeName), _TypeData.InnerTypeName);
                        }
#endif
                        break;
                    }
            }

            Size = DeserializePackedSize((byte)(info & InfoSizeMask));
            Record(nameof(Size), Size);
            Debug.Assert(Size >= 0);

            // TypeData
            switch (Type)
            {
                case PropertyType.BoolProperty:
                    BoolValue = (info & InfoArrayIndexMask) != 0;
                    break;

                default:
                    if ((info & InfoArrayIndexMask) != 0)
                    {
                        ArrayIndex = DeserializeTagArrayIndexUE1();
                        Record(nameof(ArrayIndex), ArrayIndex);
                    }

                    break;
            }

            return false;
        }

        /// <returns>True if this is the last tag.</returns>
        private bool DeserializeTagUE3()
        {
            Name = _Buffer.ReadNameReference();
            Record(nameof(Name), Name);
            if (Name.IsNone())
            {
                return true;
            }

            string typeName = _Buffer.ReadName();
            Record(nameof(typeName), typeName);
            Type = (PropertyType)Enum.Parse(typeof(PropertyType), typeName);

            Size = _Buffer.ReadInt32();
            Record(nameof(Size), Size);
            Debug.Assert(Size >= 0);

            ArrayIndex = _Buffer.ReadInt32();
            Record(nameof(ArrayIndex), ArrayIndex);

            DeserializeTypeDataUE3();
            return false;
        }
#if BATMAN
        /// <returns>True if this is the last tag.</returns>
        private bool DeserializeTagByOffset()
        {
            Type = (PropertyType)_Buffer.ReadInt16();
            Record(nameof(Type), Type.ToString());
            if (Type == PropertyType.None)
            {
                return true;
            }

            if (_Buffer.Package.Build != UnrealPackage.GameBuild.BuildName.Batman3MP)
            {
                ushort offset = _Buffer.ReadUInt16();
                Record(nameof(offset), offset);

                // TODO: Incomplete, PropertyTypes' have shifted.
                if ((int)Type == 11)
                {
                    Type = PropertyType.Vector;
                }

                // This may actually be determined by the property's flags, but we don't calculate the offset of properties :/
                // TODO: Incomplete
                if (Type == PropertyType.StrProperty ||
                    Type == PropertyType.NameProperty ||
                    Type == PropertyType.IntProperty ||
                    Type == PropertyType.FloatProperty ||
                    Type == PropertyType.StructProperty ||
                    Type == PropertyType.Vector ||
                    Type == PropertyType.Rotator ||
                    (Type == PropertyType.BoolProperty &&
                     _Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Batman4))
                {
                    switch (Type)
                    {
                        case PropertyType.Vector:
                        case PropertyType.Rotator:
                            Size = 12;
                            break;

                        case PropertyType.IntProperty:
                        case PropertyType.FloatProperty:
                        case PropertyType.StructProperty:
                            //case PropertyType.ObjectProperty:
                            //case PropertyType.InterfaceProperty:
                            //case PropertyType.ComponentProperty:
                            //case PropertyType.ClassProperty:
                            Size = 4;
                            break;

                        case PropertyType.NameProperty:
                            Size = 8;
                            break;

                        case PropertyType.BoolProperty:
                            Size = sizeof(byte);
                            break;
                    }

                    Name = new UName($"self[0x{offset:X3}]");
                    DeserializeTypeDataUE3();
                    return false;
                }
            }

            Name = _Buffer.ReadNameReference();
            Record(nameof(Name), Name);

            Size = _Buffer.ReadInt32();
            Record(nameof(Size), Size);
            Debug.Assert(Size >= 0);

            ArrayIndex = _Buffer.ReadInt32();
            Record(nameof(ArrayIndex), ArrayIndex);

            DeserializeTypeDataUE3();
            return false;
        }
#endif
        private void DeserializeTypeDataUE3()
        {
            switch (Type)
            {
                case PropertyType.StructProperty:
                    _Buffer.Read(out _TypeData.StructName);
                    Record(nameof(_TypeData.StructName), _TypeData.StructName);
#if UE4
                    if (_Buffer.UE4Version >= 441)
                    {
                        _Buffer.Skip(16);
                        _Buffer.ConformRecordPosition();
                    }
#endif
                    break;

                case PropertyType.ByteProperty:
                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.EnumNameAddedToBytePropertyTag
#if BATMAN
                        && (_Buffer.Package.Build.Generation != BuildGeneration.RSS || _Buffer.LicenseeVersion <= 21)
#endif
                       )
                    {
                        _Buffer.Read(out _TypeData.EnumName);
                        Record(nameof(_TypeData.EnumName), _TypeData.EnumName);
                    }

                    break;

                case PropertyType.BoolProperty:
                    if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.BoolValueToByteForBoolPropertyTag
#if BORDERLANDS
                        // GOTYE didn't apply this upgrade, but did the EnumName update? ...
                        && _Buffer.Package.Build != UnrealPackage.GameBuild.BuildName.Borderlands_GOTYE
#endif
                       )
                    {
                        BoolValue = _Buffer.ReadByte() > 0;
                    }
                    else
                    {
                        BoolValue = _Buffer.ReadInt32() > 0;
                    }

                    Record(nameof(BoolValue), BoolValue);
                    break;

                case PropertyType.ArrayProperty:
#if UE4
                    // FIXME: UE4 version
                    if (_Buffer.UE4Version > 220)
                    {
                        _Buffer.Read(out _TypeData.InnerTypeName);
                        Record(nameof(_TypeData.InnerTypeName), _TypeData.InnerTypeName);
                    }
#endif
                    break;
            }
        }

        /// <summary>
        ///     Deserialize the value of this UPropertyTag instance.
        ///     Note:
        ///     Only call after the whole package has been deserialized!
        /// </summary>
        /// <returns>The deserialized value if any.</returns>
        public string DeserializeValue(DeserializeFlags deserializeFlags = DeserializeFlags.None)
        {
            _Buffer = _Container.LoadBuffer();
            _Buffer.Seek(_PropertyValuePosition, SeekOrigin.Begin);
            string output = TryDeserializeDefaultPropertyValue(Type, deserializeFlags);

            LibServices.LogService.SilentAssert(_Buffer.Position == _PropertyValuePosition + Size,
                $"PropertyTag value size error for '{_Outer?.GetPath()}.{Name}: Expected: {Size}, Actual: {_Buffer.Position - _PropertyValuePosition}");

            return output;
        }

        private string TryDeserializeDefaultPropertyValue(PropertyType type, DeserializeFlags deserializeFlags)
        {
            try
            {
                return LegacyDeserializeDefaultPropertyValue(type, deserializeFlags);
            }
            catch (EndOfStreamException)
            {
                // Abort decompilation
                throw;
            }
            catch (Exception exception)
            {
                LibServices.LogService.SilentException(new DeserializationException($"PropertyTag value deserialization error for '{Name}", exception));

                return $"/* ERROR: {exception.GetType()} */";
            }
        }

        private void AssertFastSerialize(IUnrealArchive archive)
        {
            Debug.Assert(archive.Version >= (uint)PackageObjectLegacyVersion.FastSerializeStructs);
        }

        /// <summary>
        ///     Deserialize a default property value of a specified type.
        /// </summary>
        /// <param name="type">Kind of type to try deserialize.</param>
        /// <returns>The deserialized value if any.</returns>
        private string LegacyDeserializeDefaultPropertyValue(
            PropertyType type,
            DeserializeFlags deserializeFlags)
        {
            var orgOuter = _Outer;
            var propertyValue = string.Empty;

            // Deserialize Value
            switch (type)
            {
                case PropertyType.BoolProperty:
                    {
                        bool value;
                        if (Size == 0)
                        {
                            Debug.Assert(BoolValue != null, nameof(BoolValue) + " != null");
                            value = BoolValue.Value;
                        }
                        else
                        {
                            value = _Buffer.ReadByte() > 0;
                        }

                        Record(nameof(value), value);
                        propertyValue = value ? "true" : "false";
                        break;
                    }

                case PropertyType.StrProperty:
                    {
                        string value = _Buffer.ReadString();
                        Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }

                case PropertyType.NameProperty:
                    {
                        var value = _Buffer.ReadNameReference();
                        Record(nameof(value), value);
                        propertyValue = $"\"{value}\"";
                        break;
                    }
#if GIGANTIC
                case PropertyType.JsonRefProperty:
                    {
                        var jsonObjectName = _Buffer.ReadNameReference();
                        var jsonObject = _Buffer.ReadObject<UObject>();

                        if (jsonObject == null)
                        {
                            propertyValue = "none";
                            break;
                        }

                        // !!! Could be null for imports
                        //Contract.Assert(jsonObject.Class != null);
                        propertyValue = $"JsonRef<{jsonObject.GetClassName()}>'{jsonObjectName}'";
                        break;
                    }
#endif
#if MASS_EFFECT
                case PropertyType.StringRefProperty:
                    {
                        _Buffer.Read(out int index);
                        Record(nameof(index), index);

                        propertyValue = PropertyDisplay.FormatLiteral(index);
                        break;
                    }

                case PropertyType.BioMask4Property:
                    {
                        _Buffer.Read(out byte value);
                        Record(nameof(value), value);

                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }
#endif
                case PropertyType.IntProperty:
                    {
                        int value = _Buffer.ReadInt32();
                        Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }
#if BIOSHOCK
                case PropertyType.QwordProperty:
                    {
                        long value = _Buffer.ReadInt64();
                        Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }

                case PropertyType.XWeakReferenceProperty:
                    propertyValue = "/* XWeakReference: (?=" + _Buffer.ReadName() + ",?=" + _Buffer.ReadName() +
                                    ",?=" + _Buffer.ReadByte() + ",?=" + _Buffer.ReadName() + ") */";
                    break;
#endif
                case PropertyType.FloatProperty:
                    {
                        float value = _Buffer.ReadFloat();
                        Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }

                case PropertyType.ByteProperty:
                    {
                        if (_Buffer.Version >= (uint)PackageObjectLegacyVersion.EnumTagNameAddedToBytePropertyTag
                            && Type == PropertyType.ByteProperty
                            // Cannot compare size with 1 because this byte value may be part of a struct.
                            && _Buffer.Position + 1 != _PropertyValuePosition + Size)
                        {
                            string enumTagName = _Buffer.ReadName();
                            Record(nameof(enumTagName), enumTagName);
                            propertyValue = _TypeData.EnumName != null
                                ? $"{_TypeData.EnumName}.{enumTagName}"
                                : enumTagName;
                        }
                        else
                        {
                            byte value = _Buffer.ReadByte();
                            Record(nameof(value), value);
                            propertyValue = PropertyDisplay.FormatLiteral(value);
                        }

                        break;
                    }

                case PropertyType.InterfaceProperty:
                    {
                        var interfaceClass = _Buffer.ReadObject();
                        Record(nameof(interfaceClass), interfaceClass);
                        propertyValue = PropertyDisplay.FormatLiteral(interfaceClass);
                        break;
                    }

                case PropertyType.ComponentProperty:
                case PropertyType.ObjectProperty:
                    {
                        var constantObject = _Buffer.ReadObject();
                        Record(nameof(constantObject), constantObject);
                        if (constantObject == null)
                        {
                            // =none
                            propertyValue = "none";
                            break;
                        }

                        Debug.Assert(UDecompilingState.s_inlinedSubObjects != null,
                            "UDecompilingState.s_inlinedSubObjects != null");

                        bool isPendingInline =
                            UDecompilingState.s_inlinedSubObjects.TryGetValue(constantObject, out bool isInlined);
                        // If the object is part of the current container, then it probably was an inlined declaration.
                        bool shouldInline = constantObject.Outer == _Container
                                            && !isPendingInline
                                            && !isInlined;
                        if (shouldInline)
                        {
                            if ((deserializeFlags & DeserializeFlags.WithinStruct) == 0)
                            {
                                UDecompilingState.s_inlinedSubObjects.Add(constantObject, true);

                                // Unknown objects are only deserialized on demand.
                                constantObject.Load();

                                propertyValue = constantObject.Decompile() + "\r\n";

                                _TempFlags |= DoNotAppendName;
                                if ((deserializeFlags & DeserializeFlags.WithinArray) != 0)
                                {
                                    _TempFlags |= ReplaceNameMarker;
                                    propertyValue += $"{UDecompilingState.Tabs}%ARRAYNAME%={constantObject.Name}";

                                    break;
                                }

                                propertyValue += $"{UDecompilingState.Tabs}{Name}={constantObject.Name}";

                                break;
                            }

                            // Within a struct, to be inlined later on!
                            UDecompilingState.s_inlinedSubObjects.Add(constantObject, false);
                            propertyValue = $"{Name}={constantObject.Name}";

                            break;
                        }

                        // Use shorthand for inlined objects.
                        propertyValue = isInlined
                            ? constantObject.Name
                            : PropertyDisplay.FormatLiteral(constantObject);

                        break;
                    }

                case PropertyType.ClassProperty:
                    {
                        var classObject = _Buffer.ReadObject<UClass>();
                        Record(nameof(classObject), classObject);
                        propertyValue = PropertyDisplay.FormatLiteral(classObject);
                        break;
                    }

                // Old StringProperty with a fixed size and null termination.
                case PropertyType.StringProperty when _Buffer.Version < 100:
                    {
                        string str = _Buffer.ReadAnsiNullString();
                        propertyValue = str;
                        break;
                    }

                case PropertyType.DelegateProperty when _Buffer.Version >= 100:
                    {
                        // Can by any object, usually a class.
                        var functionOwner = _Buffer.ReadObject();
                        Record(nameof(functionOwner), functionOwner);

                        string functionName = _Buffer.ReadName();
                        Record(nameof(functionName), functionName);

                        // Can be null in UE3 packages
                        propertyValue = functionOwner != null
                            ? $"{functionOwner.Name}.{functionName}"
                            : $"{functionName}";
                        break;
                    }

                case PropertyType.StructProperty:
                    {
                        deserializeFlags |= DeserializeFlags.WithinStruct;

                        if (UStructProperty.PropertyValueSerializer.CanSerializeStructUsingBinary(_Buffer))
                        {
                            // Some structs are serialized using tags.
                            // Let's find out if this is one of them.
                            Enum.TryParse(_TypeData.StructName, out UStructProperty.PropertyValueSerializer.BinaryStructType binaryStructType);
                            if (UStructProperty.PropertyValueSerializer.IsStructImmutable(_Buffer, _Outer, binaryStructType))
                            {
                                // Deserialize using the atomic serializer.
                                propertyValue += LegacyDeserializeDefaultBinaryStructValue(binaryStructType, deserializeFlags);

                                goto output;
                            }

                            // fall-back to non-atomic struct deserialization; may fail if the struct is an unknown immutable/atomic struct.
                        }

                    nonAtomic:
                        FindProperty<UProperty>(out var propertySource);
                        var tag = new UDefaultProperty(_Container, propertySource);
                        if (!tag.Deserialize())
                        {
                            return "()";
                        }

                        propertyValue += DecompileTag(tag);
                        while (tag.Deserialize())
                        {
                            propertyValue += ",";
                            propertyValue += DecompileTag(tag);
                        }

                        string DecompileTag(UDefaultProperty tag)
                        {
                            string tagExpr = tag.Name;
                            if (tag.ArrayIndex > 0)
                            {
                                tagExpr += PropertyDisplay.FormatT3DElementAccess(tag.ArrayIndex.ToString(), _Buffer.Version);
                            }

                            _Buffer.Seek(tag._PropertyValuePosition, SeekOrigin.Begin);
                            string value = tag.TryDeserializeDefaultPropertyValue(tag.Type, deserializeFlags);

                            return $"{tagExpr}={value}";
                        }

                    output:
                        propertyValue = $"({propertyValue})";
                        break;
                    }

                case PropertyType.ArrayProperty:
                    {
                        int arraySize = _Buffer.ReadIndex();
                        Record(nameof(arraySize), arraySize);
                        if (arraySize == 0)
                        {
                            propertyValue = "none";
                            break;
                        }

                        var arrayType = PropertyType.None;
                        if (_TypeData.InnerTypeName != null && !Enum.TryParse(_TypeData.InnerTypeName, out arrayType))
                        {
                            throw new Exception(
                                $"Couldn't convert InnerTypeName \"{_TypeData.InnerTypeName}\" to PropertyType");
                        }

                        // Find the property within the outer/owner or its inheritances.
                        // If found it has to modify the outer so structs within this array can find their array variables.
                        // Additionally we need to know the property to determine the array's type.
                        if (arrayType == PropertyType.None)
                        {
                            var property = FindProperty<UArrayProperty>(out _Outer);
                            if (property?.InnerProperty != null)
                            {
                                arrayType = property.InnerProperty.Type;
                            }
                            // If we did not find a reference to the associated property(because of imports)
                            // then try to determine the array's type by scanning the defined array types.
                            else if (UnrealConfig.VariableTypes != null && UnrealConfig.VariableTypes.ContainsKey(Name))
                            {
                                var varTuple = UnrealConfig.VariableTypes[Name];
                                if (varTuple != null)
                                {
                                    arrayType = varTuple.Item2;
                                }
                            }
                        }

                        // Hardcoded fix for InterpCurve and InterpCurvePoint.
                        if (arrayType == PropertyType.None
                            && (deserializeFlags & DeserializeFlags.WithinStruct) != 0
                            && string.Compare(Name, "Points", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            arrayType = PropertyType.StructProperty;
                        }

                        if (arrayType == PropertyType.None)
                        {
                            LibServices.LogService.Log($"Couldn't acquire array type for property tag '{Name}' in {_Outer.GetReferencePath()}.");

                            propertyValue = "/* Array type was not detected. */";
                            break;
                        }

                        deserializeFlags |= DeserializeFlags.WithinArray;
                        if ((deserializeFlags & DeserializeFlags.WithinStruct) != 0)
                        {

                            for (var i = 0; i < arraySize; ++i)
                            {
                                propertyValue += LegacyDeserializeDefaultPropertyValue(arrayType, deserializeFlags)
                                                 + (i != arraySize - 1 ? "," : string.Empty);
                            }

                            propertyValue = $"({propertyValue})";
                        }
                        else
                        {
                            for (var i = 0; i < arraySize; ++i)
                            {
                                string elementAccessText =
                                    PropertyDisplay.FormatT3DElementAccess(i.ToString(), _Buffer.Version);
                                string elementValue = LegacyDeserializeDefaultPropertyValue(arrayType, deserializeFlags);
                                if ((_TempFlags & ReplaceNameMarker) != 0)
                                {
                                    propertyValue += elementValue.Replace("%ARRAYNAME%", $"{Name}{elementAccessText}");
                                    _TempFlags = 0x00;
                                }
                                else
                                {
                                    propertyValue += $"{Name}{elementAccessText}={elementValue}";
                                }

                                if (i != arraySize - 1)
                                {
                                    propertyValue += "\r\n" + UDecompilingState.Tabs;
                                }
                            }
                        }

                        _TempFlags |= DoNotAppendName;
                        break;
                    }

                case PropertyType.MapProperty:
                    {
                        if (Size == 0) break;

                        int count = _Buffer.ReadIndex();
                        Record(nameof(count), count);

                        var property = FindProperty<UMapProperty>(out _Outer);
                        if (property == null)
                        {
                            propertyValue = "// Unable to decompile Map data.";
                            break;
                        }

                        propertyValue = "(";
                        for (int i = 0; i < count; ++i)
                        {
                            propertyValue +=
                                LegacyDeserializeDefaultPropertyValue(property.ValueProperty.Type, deserializeFlags);
                            if (i + 1 != count)
                            {
                                propertyValue += ",";
                            }
                        }

                        propertyValue += ")";
                        break;
                    }

                case PropertyType.FixedArrayProperty:
                    {
                        // We require the InnerProperty to properly deserialize this data type.
                        var property = FindProperty<UFixedArrayProperty>(out _Outer);
                        if (property == null)
                        {
                            propertyValue = "// Unable to decompile FixedArray data.";
                            break;
                        }

                        var innerType = property.InnerProperty.Type;
                        propertyValue = "(";
                        for (int i = 0; i < property.Count; ++i)
                        {
                            propertyValue += LegacyDeserializeDefaultPropertyValue(innerType, deserializeFlags);
                            if (i + 1 != property.Count)
                            {
                                propertyValue += ",";
                            }
                        }

                        propertyValue += ")";
                        break;
                    }

                // Note: We don't have to verify the package's version here.
                case PropertyType.PointerProperty:
                    {
                        int offset = _Buffer.ReadInt32();
                        propertyValue = PropertyDisplay.FormatLiteral(offset);
                        break;
                    }

                // Legacy fall-back for batman etc
                case PropertyType.Vector:
                    {
                        propertyValue = LegacyDeserializeDefaultBinaryStructValue(
                            UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        break;
                    }

                // Legacy fall-back for batman etc
                case PropertyType.Rotator:
                    {
                        propertyValue = LegacyDeserializeDefaultBinaryStructValue(
                            UStructProperty.PropertyValueSerializer.BinaryStructType.Rotator, deserializeFlags);
                        break;
                    }

#if BORDERLANDS2 || BATTLEBORN
                case PropertyType.ByteAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(PropertyType.ByteProperty, deserializeFlags);

                case PropertyType.IntAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(PropertyType.IntProperty, deserializeFlags);

                case PropertyType.FloatAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(PropertyType.FloatProperty, deserializeFlags);
#endif
#if BULLETSTORM
                case PropertyType.CppCopyStructProperty:
                    return LegacyDeserializeDefaultPropertyValue(PropertyType.StructProperty, deserializeFlags);
#endif
                default:
                    throw new Exception($"Unsupported property tag type {Type}");
            }

            _Outer = orgOuter;
            return propertyValue;
        }

        private string LegacyDeserializeDefaultBinaryStructValue(
            UStructProperty.PropertyValueSerializer.BinaryStructType structType,
            DeserializeFlags deserializeFlags)
        {
            string propertyValue = string.Empty;

            switch (structType)
            {
                case UStructProperty.PropertyValueSerializer.BinaryStructType.Color:
                    {
                        _Buffer.ReadStructMarshal(out UColor color);
                        if (_Buffer.Version > 69) // FIXME: Version, may need adjustments for UE1
                        {
                            propertyValue += $"R={PropertyDisplay.FormatLiteral(color.R)}," +
                                             $"G={PropertyDisplay.FormatLiteral(color.G)}," +
                                             $"B={PropertyDisplay.FormatLiteral(color.B)}," +
                                             $"A={PropertyDisplay.FormatLiteral(color.A)}";
                        }
                        else
                        {
                            // old UE1 order swap B and R
                            propertyValue += $"R={PropertyDisplay.FormatLiteral(color.B)}," +
                                             $"G={PropertyDisplay.FormatLiteral(color.G)}," +
                                             $"B={PropertyDisplay.FormatLiteral(color.R)}," +
                                             $"A={PropertyDisplay.FormatLiteral(color.A)}";
                        }
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Coords:
                    {
                        string origin = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string xAxis = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string yAxis = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string zAxis = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);

                        propertyValue += $"Origin=({origin})," +
                                         $"XAxis=({xAxis})," +
                                         $"YAxis=({yAxis})," +
                                         $"ZAxis=({zAxis})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.LinearColor:
                    {
                        _Buffer.ReadStructMarshal(out ULinearColor color);
                        propertyValue += $"R={PropertyDisplay.FormatLiteral(color.R)}," +
                                         $"G={PropertyDisplay.FormatLiteral(color.G)}," +
                                         $"B={PropertyDisplay.FormatLiteral(color.B)}," +
                                         $"A={PropertyDisplay.FormatLiteral(color.A)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Vector2D:
                    {
                        _Buffer.ReadStructMarshal(out UVector2D vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Vector:
                    {
                        _Buffer.ReadStructMarshal(out UVector vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(vector.Z)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Vector4:
                    {
                        _Buffer.ReadStructMarshal(out UVector4 vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(vector.Z)}," +
                                         $"W={PropertyDisplay.FormatLiteral(vector.W)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.TwoVectors:
                    {
                        string v1 = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string v2 = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        propertyValue += $"v1=({v1})," +
                                         $"v2=({v2})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Rotator:
                    {
                        _Buffer.ReadStructMarshal(out URotator rotator);
                        propertyValue += $"Pitch={rotator.Pitch}," +
                                         $"Yaw={rotator.Yaw}," +
                                         $"Roll={rotator.Roll}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Guid:
                    {
                        _Buffer.ReadStructMarshal(out UGuid guid);
                        propertyValue += $"A={guid.A}," +
                                         $"B={guid.B}," +
                                         $"C={guid.C}," +
                                         $"D={guid.D}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Sphere:
                    {
                        _Buffer.ReadStructMarshal(out USphere sphere);
                        propertyValue += $"W={PropertyDisplay.FormatLiteral(sphere.W)}," +
                                         $"X={PropertyDisplay.FormatLiteral(sphere.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(sphere.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(sphere.Z)}";

                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Plane:
                    {
                        _Buffer.ReadStructMarshal(out UPlane plane);
                        propertyValue += $"W={PropertyDisplay.FormatLiteral(plane.W)}," +
                                         $"X={PropertyDisplay.FormatLiteral(plane.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(plane.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(plane.Z)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Scale:
                    {
                        _Buffer.ReadStructMarshal(out UScale scale);
                        propertyValue += "Scale=(" +
                                         $"X={PropertyDisplay.FormatLiteral(scale.Scale.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(scale.Scale.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(scale.Scale.Z)})," +
                                         $"SheerRate={PropertyDisplay.FormatLiteral(scale.SheerRate)}," +
                                         $"SheerAxis={scale.SheerAxis}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Box:
                    {
                        string min = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string max = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string isValid = LegacyDeserializeDefaultPropertyValue(PropertyType.ByteProperty, deserializeFlags);
                        propertyValue += $"Min=({min})," +
                                         $"Max=({max})," +
                                         $"IsValid={isValid}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Quat:
                    {
                        _Buffer.ReadStructMarshal(out UQuat quat);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(quat.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(quat.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(quat.Z)}," +
                                         $"W={PropertyDisplay.FormatLiteral(quat.W)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Range:
                    {
                        string min = LegacyDeserializeDefaultPropertyValue(PropertyType.FloatProperty, deserializeFlags);
                        string max = LegacyDeserializeDefaultPropertyValue(PropertyType.FloatProperty, deserializeFlags);
                        propertyValue += $"A={min},B={max}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.RangeVector:
                    {
                        string x = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Range, deserializeFlags);
                        string y = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Range, deserializeFlags);
                        string z = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Range, deserializeFlags);
                        propertyValue += $"X=({x}),Y=({y}),Z=({z})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Matrix:
                    {
                        //_Buffer.ReadAtomicStruct(out UMatrix matrix);
                        string xPlane = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        string yPlane = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        string zPlane = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        string wPlane = LegacyDeserializeDefaultBinaryStructValue(UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        propertyValue += $"XPlane=({xPlane}),YPlane=({yPlane}),ZPlane=({zPlane}),WPlane=({wPlane})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.IntPoint:
                    {
                        string x = LegacyDeserializeDefaultPropertyValue(PropertyType.IntProperty, deserializeFlags);
                        string y = LegacyDeserializeDefaultPropertyValue(PropertyType.IntProperty, deserializeFlags);
                        propertyValue += $"X={x},Y={y}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.PointRegion:
                    {
                        string zone = LegacyDeserializeDefaultPropertyValue(PropertyType.ObjectProperty, deserializeFlags);
                        string iLeaf = LegacyDeserializeDefaultPropertyValue(PropertyType.IntProperty, deserializeFlags);
                        string zoneNumber = LegacyDeserializeDefaultPropertyValue(PropertyType.ByteProperty, deserializeFlags);
                        propertyValue += $"Zone={zone},iLeaf={iLeaf},ZoneNumber={zoneNumber}";
                        break;
                    }
            }

            return propertyValue;
        }

        #endregion

        public static PropertyType ResolvePropertyType(ushort propertyType)
        {
            return (PropertyType)propertyType;
        }

        public static string ResolvePropertyTypeName(PropertyType propertyType)
        {
            return Enum.GetName(typeof(PropertyType), propertyType);
        }
    }

    [ComVisible(false)]
    public sealed class DefaultPropertiesCollection : List<UDefaultProperty>
    {
        public UDefaultProperty? Find(string name)
        {
            return Find(prop => prop.Name == name);
        }

        public UDefaultProperty? Find(UName name)
        {
            return Find(prop => prop.Name == name);
        }

        public bool Contains(string name)
        {
            return Find(name) != null;
        }

        public bool Contains(UName name)
        {
            return Find(name) != null;
        }
    }
}
