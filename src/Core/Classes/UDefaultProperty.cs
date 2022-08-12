using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UELib.Annotations;
using UELib.Core.Types;
using UELib.Types;
using UELib.UnrealScript;

namespace UELib.Core
{
    /// <summary>
    /// [Default]Properties values deserializer.
    /// </summary>
    public sealed class UDefaultProperty : IUnrealDecompilable
    {
        [Flags]
        public enum DeserializeFlags : byte
        {
            None                    = 0x00,
            WithinStruct            = 0x01,
            WithinArray             = 0x02,
            Complex                 = WithinStruct | WithinArray,
        }

        private const byte DoNotAppendName = 0x01;
        private const byte ReplaceNameMarker = 0x02;

        private const int V3 = 220;

        // FIXME: Wrong version, naive approach
        private const int VAtomicStructs = V3;
        private const int VEnumName = 633;
        private const int VBoolSizeToOne = 673;

        private IUnrealStream _Buffer => _Container.Buffer;

        private readonly UObject _Container;
        private UStruct _Outer;
        private bool _RecordingEnabled = true;

        internal long _BeginOffset { get; set; }
        private long _ValueOffset { get; set; }

        private byte _TempFlags { get; set; }

        #region Serialized Members

        /// <summary>
        /// Name of the UProperty.
        ///
        /// get and private remain to maintain compatibility with UE Explorer
        /// </summary>
        [PublicAPI]
        public UName Name { get; private set; }

        /// <summary>
        /// Name of the UStruct. If type equals StructProperty.
        /// </summary>
        [PublicAPI] [CanBeNull] public UName ItemName;

        /// <summary>
        /// Name of the UEnum. If Type equals ByteProperty.
        /// </summary>
        [PublicAPI] [CanBeNull] public UName EnumName;
        [PublicAPI] [CanBeNull] public UName InnerTypeName;

        /// <summary>
        /// See PropertyType enum in UnrealFlags.cs
        /// </summary>
        [PublicAPI] public PropertyType Type;

        [PublicAPI] public PropertyType InnerType;

        /// <summary>
        /// The stream size of this DefaultProperty.
        /// </summary>
        private int Size { get; set; }

        /// <summary>
        /// Whether this property is part of an static array, and the index into it
        /// </summary>
        public int ArrayIndex = -1;

        /// <summary>
        /// Value of the UBoolProperty. If Type equals BoolProperty.
        /// </summary>
        public bool? BoolValue;

        /// <summary>
        /// The deserialized and decompiled output.
        ///
        /// Serves as a temporary workaround, don't rely on it.
        /// </summary>
        [PublicAPI]
        public string Value { get; private set; }

        #endregion

        #region Constructors

        public UDefaultProperty(UObject owner, UStruct outer = null)
        {
            _Container = owner;
            _Outer = (outer ?? _Container as UStruct) ?? _Container.Outer as UStruct;
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
                    return _Buffer.ReadInt16();

                case 0x70:
                    return _Buffer.ReadInt32();

                default:
                    throw new NotImplementedException($"Unknown sizePack {sizePack}");
            }
        }

        private const byte ArrayIndexMask = 0x80;

        private int DeserializeTagArrayIndexUE1()
        {
            int arrayIndex;
#if BINARYMETADATA
            long startPos = _Buffer.Position;
#endif
            byte b = _Buffer.ReadByte();
            if ((b & ArrayIndexMask) == 0)
                arrayIndex = b;
            else if ((b & 0xC0) == ArrayIndexMask)
                arrayIndex = ((b & 0x7F) << 8) + _Buffer.ReadByte();
            else
                arrayIndex = ((b & 0x3F) << 24)
                             + (_Buffer.ReadByte() << 16)
                             + (_Buffer.ReadByte() << 8)
                             + _Buffer.ReadByte();
#if BINARYMETADATA
            _Buffer.LastPosition = startPos;
#endif
            return arrayIndex;
        }

        /// <returns>True if there are more property tags.</returns>
        public bool Deserialize()
        {
            _BeginOffset = _Buffer.Position;
            if (DeserializeNextTag())
            {
                return false;
            }

            _ValueOffset = _Buffer.Position;
            try
            {
                DeserializeValue();
                _RecordingEnabled = false;
            }
            finally
            {
                // Even if something goes wrong, we can still skip everything and safely deserialize the next property if any!
                // Note: In some builds @Size is not serialized
                _Buffer.Position = _ValueOffset + Size;
            }

            return true;
        }

        /// <returns>True if this is the last tag.</returns>
        private bool DeserializeNextTag()
        {
            if (_Buffer.Version < V3) return DeserializeTagUE1();
#if BATMAN
            if (_Buffer.Package.Build == BuildGeneration.RSS)
                return DeserializeTagByOffset();
#endif
            return DeserializeTagUE3();
        }

        /// <returns>True if this is the last tag.</returns>
        private bool DeserializeTagUE1()
        {
            Name = _Buffer.ReadNameReference();
            Record(nameof(Name), Name);
            if (Name.IsNone()) return true;

            const byte typeMask = 0x0F;
            const byte sizeMask = 0x70;

            // Packed byte
            byte info = _Buffer.ReadByte();
            Record(
                $"Info(Type={(PropertyType)(byte)(info & typeMask)}," +
                $"SizeMask=0x{(byte)(info & sizeMask):X2}," +
                $"ArrayIndexMask=0x{info & ArrayIndexMask:X2})",
                info
            );

            Type = (PropertyType)(byte)(info & typeMask);
            switch (Type)
            {
                case PropertyType.StructProperty:
                    ItemName = _Buffer.ReadNameReference();
                    Record(nameof(ItemName), ItemName);
                    break;
                
                case PropertyType.ArrayProperty:
                {
#if DNF
                    if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                        _Buffer.Version >= 124)
                    {
                        InnerTypeName = _Buffer.ReadNameReference();
                        Record(nameof(InnerTypeName), InnerTypeName);
                        if (!Enum.TryParse(InnerTypeName, out InnerType))
                        {
                            throw new Exception($"Couldn't convert InnerTypeName \"{InnerTypeName}\" to PropertyType");
                        }
                    }
#endif
                    break;
                }
            }

            Size = DeserializePackedSize((byte)(info & sizeMask));
            Record(nameof(Size), Size);

            // TypeData
            switch (Type)
            {
                case PropertyType.BoolProperty:
                    BoolValue = (info & ArrayIndexMask) != 0;
                    break;

                default:
                    if ((info & ArrayIndexMask) != 0)
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
            if (Name.IsNone()) return true;

            string typeName = _Buffer.ReadName();
            Record(nameof(typeName), typeName);
            Type = (PropertyType)Enum.Parse(typeof(PropertyType), typeName);

            Size = _Buffer.ReadInt32();
            Record(nameof(Size), Size);

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
            if (Type == PropertyType.None) return true;

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
                    (Type == PropertyType.BoolProperty && _Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Batman4))
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
                    ItemName = _Buffer.ReadNameReference();
                    Record(nameof(ItemName), ItemName);
#if UE4
                    if (_Buffer.UE4Version >= 441)
                    {
                        _Buffer.Skip(16);
                    }
#endif
                    break;

                case PropertyType.ByteProperty:
                    if (_Buffer.Version >= VEnumName)
                    {
                        EnumName = _Buffer.ReadNameReference();
                        Record(nameof(EnumName), EnumName);
                    }

                    break;

                case PropertyType.BoolProperty:
                    BoolValue = _Buffer.Version >= VBoolSizeToOne
                        ? _Buffer.ReadByte() > 0
                        : _Buffer.ReadInt32() > 0;
                    Record(nameof(BoolValue), BoolValue);
                    break;
                
                case PropertyType.ArrayProperty:
#if UE4
                    // FIXME: UE4 version
                    if (_Buffer.UE4Version > 220)
                    {
                        InnerTypeName = _Buffer.ReadNameReference();
                        Record(nameof(InnerTypeName), InnerTypeName);
                        InnerType = (PropertyType)Enum.Parse(typeof(PropertyType), InnerTypeName);
                    }
#endif
                    break;
            }
        }

        /// <summary>
        /// Deserialize the value of this UPropertyTag instance.
        ///
        /// Note:
        ///     Only call after the whole package has been deserialized!
        /// </summary>
        /// <returns>The deserialized value if any.</returns>
        [PublicAPI]
        public string DeserializeValue(DeserializeFlags deserializeFlags = DeserializeFlags.None)
        {
            if (_Buffer == null)
            {
                _Container.EnsureBuffer();
                if (_Buffer == null) throw new DeserializationException("_Buffer is not initialized!");
            }

            _Buffer.Seek(_ValueOffset, SeekOrigin.Begin);
            return TryDeserializeDefaultPropertyValue(Type, ref deserializeFlags);
        }

        private string TryDeserializeDefaultPropertyValue(PropertyType type, ref DeserializeFlags deserializeFlags)
        {
            try
            {
                return DeserializeDefaultPropertyValue(type, ref deserializeFlags);
            }
            catch (EndOfStreamException e)
            {
                // Abort decompilation
                throw;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception thrown: {e} in {nameof(DeserializeDefaultPropertyValue)}");
                return $"/*ERROR: {e}*/";
            }
        }

        /// <summary>
        /// Deserialize a default property value of a specified type.
        /// </summary>
        /// <param name="type">Kind of type to try deserialize.</param>
        /// <returns>The deserialized value if any.</returns>
        private string DeserializeDefaultPropertyValue(PropertyType type, ref DeserializeFlags deserializeFlags)
        {
            var orgOuter = _Outer;
            var propertyValue = string.Empty;
            
            // Deserialize Value
            switch (type)
            {
                case PropertyType.BoolProperty:
                {
                    Debug.Assert(BoolValue != null, nameof(BoolValue) + " != null");
                    bool value = BoolValue.Value;
                    if (Size == 1 && _Buffer.Version < V3)
                    {
                        value = _Buffer.ReadByte() > 0;
                        Record(nameof(value), value);
                    }

                    propertyValue = value ? "true" : "false";
                    break;
                }

                case PropertyType.StrProperty:
                {
                    string value = _Buffer.ReadText();
                    Record(nameof(value), value);
                    propertyValue = PropertyDisplay.FormatLiteral(value);
                    break;
                }

                case PropertyType.NameProperty:
                {
                    var value = _Buffer.ReadNameReference();
                    Record(nameof(value), value);
                    propertyValue = value;
                    break;
                }

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
                    if (_Buffer.Version >= V3 && Size == 8)
                    {
                        string value = _Buffer.ReadName();
                        Record(nameof(value), value);
                        propertyValue = value;
                        if (_Buffer.Version >= VEnumName) propertyValue = $"{EnumName}.{propertyValue}";
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
                case PropertyType.ComponentProperty:
                case PropertyType.ObjectProperty:
                {
                    var constantObject = _Buffer.ReadObject();
                    Record(nameof(constantObject), constantObject);
                    if (constantObject != null)
                    {
                        var inline = false;
                        // If true, object is an archetype or subobject.
                        if (constantObject.Outer == _Container &&
                            (deserializeFlags & DeserializeFlags.WithinStruct) == 0)
                        {
                            // Unknown objects are only deserialized on demand.
                            constantObject.BeginDeserializing();
                            if (constantObject.Properties != null && constantObject.Properties.Count > 0)
                            {
                                inline = true;
                                propertyValue = constantObject.Decompile() + "\r\n" + UDecompilingState.Tabs;

                                _TempFlags |= DoNotAppendName;
                                if ((deserializeFlags & DeserializeFlags.WithinArray) != 0)
                                {
                                    _TempFlags |= ReplaceNameMarker;
                                    propertyValue += $"%ARRAYNAME%={constantObject.Name}";
                                }
                                else
                                {
                                    propertyValue += $"{Name}={constantObject.Name}";
                                }
                            }
                        }

                        if (!inline)
                            // =CLASS'Package.Group(s)+.Name'
                            propertyValue = $"{constantObject.GetClassName()}\'{constantObject.GetOuterGroup()}\'";
                    }
                    else
                    {
                        // =none
                        propertyValue = "none";
                    }

                    break;
                }

                case PropertyType.ClassProperty:
                {
                    var classObject = _Buffer.ReadObject();
                    Record(nameof(classObject), classObject);
                    propertyValue = classObject != null
                        ? $"class'{classObject.Name}'"
                        : "none";
                    break;
                }

                case PropertyType.DelegateProperty:
                {
                    _TempFlags |= DoNotAppendName;

                    var outerObj = _Buffer.ReadObject(); // Where the assigned delegate property exists.
                    Record(nameof(outerObj), outerObj);

                    string delegateName = _Buffer.ReadName();
                    Record(nameof(delegateName), delegateName);

                    // Strip __%delegateName%__Delegate
                    string normalizedDelegateName = ((string)Name).Substring(2, Name.Length - 12);
                    propertyValue = $"{normalizedDelegateName}={delegateName}";
                    break;
                }

                #region HardCoded Struct Types

                case PropertyType.Color:
                {
                    _Buffer.ReadAtomicStruct(out UColor color);
                    propertyValue += PropertyDisplay.FormatLiteral(color);
                    break;
                }

                case PropertyType.LinearColor:
                {
                    string r = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string g = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string b = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string a = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);

                    propertyValue += $"R={r},G={g},B={b},A={a}";
                    break;
                }

                case PropertyType.Vector:
                {
                    string x = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string y = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string z = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);

                    propertyValue += $"X={x},Y={y},Z={z}";
                    break;
                }

                case PropertyType.TwoVectors:
                {
                    string v1 = DeserializeDefaultPropertyValue(PropertyType.Vector, ref deserializeFlags);
                    string v2 = DeserializeDefaultPropertyValue(PropertyType.Vector, ref deserializeFlags);
                    propertyValue += $"v1=({v1}),v2=({v2})";
                    break;
                }

                case PropertyType.Vector4:
                {
                    string x = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string y = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string z = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string w = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);

                    propertyValue += $"X={x},Y={y},Z={z},W={w}";
                    break;
                }

                case PropertyType.Vector2D:
                {
                    string x = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string y = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    propertyValue += $"X={x},Y={y}";
                    break;
                }

                case PropertyType.Rotator:
                {
                    string pitch = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    string yaw = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    string roll = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    propertyValue += $"Pitch={pitch},Yaw={yaw},Roll={roll}";
                    break;
                }

                case PropertyType.Guid:
                {
                    string a = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    string b = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    string c = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    string d = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    propertyValue += $"A={a},B={b},C={c},D={d}";
                    break;
                }

                case PropertyType.Sphere:
                case PropertyType.Plane:
                {
                    if (_Buffer.Version < VAtomicStructs)
                    {
                        throw new NotSupportedException("Not atomic");
                    }

                    string w = DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string v = DeserializeDefaultPropertyValue(PropertyType.Vector, ref deserializeFlags);
                    propertyValue += $"W={w},{v}";
                    break;
                }

                case PropertyType.Scale:
                {
                    string scale = DeserializeDefaultPropertyValue(PropertyType.Vector, ref deserializeFlags);
                    string sheerRate =
                        DeserializeDefaultPropertyValue(PropertyType.FloatProperty, ref deserializeFlags);
                    string sheerAxis =
                        DeserializeDefaultPropertyValue(PropertyType.ByteProperty, ref deserializeFlags);
                    propertyValue += $"Scale=({scale}),SheerRate={sheerRate},SheerAxis={sheerAxis}";
                    break;
                }

                case PropertyType.Box:
                {
                    if (_Buffer.Version < VAtomicStructs)
                    {
                        throw new NotSupportedException("Not atomic");
                    }

                    string min = DeserializeDefaultPropertyValue(PropertyType.Vector, ref deserializeFlags);
                    string max = DeserializeDefaultPropertyValue(PropertyType.Vector, ref deserializeFlags);
                    string isValid =
                        DeserializeDefaultPropertyValue(PropertyType.ByteProperty, ref deserializeFlags);
                    propertyValue += $"Min=({min}),Max=({max}),IsValid={isValid}";
                    break;
                }

                case PropertyType.Quat:
                {
                    propertyValue += DeserializeDefaultPropertyValue(PropertyType.Plane, ref deserializeFlags);
                    break;
                }

                case PropertyType.Matrix:
                {
                    if (_Buffer.Version < VAtomicStructs)
                    {
                        throw new NotSupportedException("Not atomic");
                    }

                    string xPlane = DeserializeDefaultPropertyValue(PropertyType.Plane, ref deserializeFlags);
                    string yPlane = DeserializeDefaultPropertyValue(PropertyType.Plane, ref deserializeFlags);
                    string zPlane = DeserializeDefaultPropertyValue(PropertyType.Plane, ref deserializeFlags);
                    string wPlane = DeserializeDefaultPropertyValue(PropertyType.Plane, ref deserializeFlags);
                    propertyValue += $"XPlane=({xPlane}),YPlane=({yPlane}),ZPlane=({zPlane}),WPlane=({wPlane})";
                    break;
                }

                case PropertyType.IntPoint:
                {
                    string x = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    string y = DeserializeDefaultPropertyValue(PropertyType.IntProperty, ref deserializeFlags);
                    propertyValue += $"X={x},Y={y}";
                    break;
                }

                #endregion

                case PropertyType.PointerProperty:
                case PropertyType.StructProperty:
                {
                    deserializeFlags |= DeserializeFlags.WithinStruct;
                    var isHardCoded = false;
                    var hardcodedStructs = (PropertyType[])Enum.GetValues(typeof(PropertyType));
                    for (var i = (byte)PropertyType.StructOffset; i < hardcodedStructs.Length; ++i)
                    {
                        string structType = Enum.GetName(typeof(PropertyType), (byte)hardcodedStructs[i]);
                        if (string.Compare(ItemName, structType, StringComparison.OrdinalIgnoreCase) != 0)
                            continue;

                        // Not atomic if <=UE2,
                        // TODO: Figure out all non-atomic structs
                        if (_Buffer.Version < VAtomicStructs)
                            switch (hardcodedStructs[i])
                            {
                                case PropertyType.Matrix:
                                case PropertyType.Box:
                                case PropertyType.Plane:
                                    goto nonAtomic;
                            }

                        isHardCoded = true;
                        propertyValue += DeserializeDefaultPropertyValue(hardcodedStructs[i], ref deserializeFlags);
                        break;
                    }

                nonAtomic:
                    if (!isHardCoded)
                    {
                        // We have to modify the outer so that dynamic arrays within this struct
                        // will be able to find its variables to determine the array type.
                        FindProperty(out _Outer);
                        while (true)
                        {
                            var tag = new UDefaultProperty(_Container, _Outer);
                            if (tag.Deserialize())
                            {
                                propertyValue += tag.Name +
                                                 (tag.ArrayIndex > 0 && tag.Type != PropertyType.BoolProperty
                                                     ? $"[{tag.ArrayIndex}]"
                                                     : string.Empty) +
                                                 "=" + tag.DeserializeValue(deserializeFlags) + ",";
                            }
                            else
                            {
                                if (propertyValue.EndsWith(","))
                                    propertyValue = propertyValue.Remove(propertyValue.Length - 1, 1);

                                break;
                            }
                        }
                    }

                    propertyValue = propertyValue.Length != 0
                        ? $"({propertyValue})"
                        : "none";
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

                    // Find the property within the outer/owner or its inheritances.
                    // If found it has to modify the outer so structs within this array can find their array variables.
                    // Additionally we need to know the property to determine the array's type.
                    var arrayType = PropertyType.None;
                    if (InnerType != PropertyType.None)
                    {
                        arrayType = InnerType;
                    }
                    else
                    {
                        var property = FindProperty(out _Outer) as UArrayProperty;
                        if (property?.InnerProperty != null)
                        {
                            arrayType = property.InnerProperty.Type;
                        }
                        // If we did not find a reference to the associated property(because of imports)
                        // then try to determine the array's type by scanning the defined array types.
                        else if (UnrealConfig.VariableTypes != null && UnrealConfig.VariableTypes.ContainsKey(Name))
                        {
                            var varTuple = UnrealConfig.VariableTypes[Name];
                            if (varTuple != null) arrayType = varTuple.Item2;
                        }
                    }

                    if (arrayType == PropertyType.None)
                    {
                        propertyValue = "/* Array type was not detected. */";
                        break;
                    }

                    deserializeFlags |= DeserializeFlags.WithinArray;
                    if ((deserializeFlags & DeserializeFlags.WithinStruct) != 0)
                    {
                        // Hardcoded fix for InterpCurve and InterpCurvePoint.
                        if (string.Compare(Name, "Points", StringComparison.OrdinalIgnoreCase) == 0)
                            arrayType = PropertyType.StructProperty;

                        for (var i = 0; i < arraySize; ++i)
                            propertyValue += DeserializeDefaultPropertyValue(arrayType, ref deserializeFlags)
                                             + (i != arraySize - 1 ? "," : string.Empty);

                        propertyValue = $"({propertyValue})";
                    }
                    else
                    {
                        for (var i = 0; i < arraySize; ++i)
                        {
                            string elementValue = DeserializeDefaultPropertyValue(arrayType, ref deserializeFlags);
                            if ((_TempFlags & ReplaceNameMarker) != 0)
                            {
                                propertyValue += elementValue.Replace("%ARRAYNAME%", $"{Name}({i})");
                                _TempFlags = 0x00;
                            }
                            else
                            {
                                propertyValue += $"{Name}({i})={elementValue}";
                            }

                            if (i != arraySize - 1) propertyValue += "\r\n" + UDecompilingState.Tabs;
                        }
                    }

                    _TempFlags |= DoNotAppendName;
                    break;
                }

                default:
                    throw new Exception($"Unsupported property tag type {Type}");
            }
            _Outer = orgOuter;
            return propertyValue;
        }

        #endregion

        #region Decompilation

        public string Decompile()
        {
            _TempFlags = 0x00;
            string value;
            _Container.EnsureBuffer();
            try
            {
                value = DeserializeValue();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Exception thrown: {e} in {nameof(Decompile)}");
                value = $"/*ERROR: {e}*/";
            }
            finally
            {
                _Container.MaybeDisposeBuffer();
            }

            // Array or Inlined object
            if ((_TempFlags & DoNotAppendName) != 0)
                // The tag handles the name etc on its own.
                return value;

            var arrayIndex = string.Empty;
            if (ArrayIndex > 0 && Type != PropertyType.BoolProperty) arrayIndex += $"[{ArrayIndex}]";

            return $"{Name}{arrayIndex}={value}";
        }

        #endregion

        #region Methods

        private UProperty FindProperty(out UStruct outer)
        {
            UProperty property = null;
            outer = _Outer ?? _Container.Class as UStruct;
            for (var structField = outer; structField != null; structField = structField.Super as UStruct)
            {
                if (structField.Variables == null || !structField.Variables.Any())
                    continue;

                property = structField.Variables.Find(i => i.Table.ObjectName == Name);
                if (property == null)
                    continue;

                switch (property.Type)
                {
                    case PropertyType.StructProperty:
                        outer = ((UStructProperty)property).StructObject;
                        break;

                    case PropertyType.ArrayProperty:
                        var arrayField = property as UArrayProperty;
                        Debug.Assert(arrayField != null, "arrayField != null");
                        var arrayInnerField = arrayField.InnerProperty;
                        if (arrayInnerField.Type == PropertyType.StructProperty)
                            _Outer = ((UStructProperty)arrayInnerField).StructObject;

                        break;

                    default:
                        outer = structField;
                        break;
                }

                break;
            }

            return property;
        }

        #endregion

        [Conditional("BINARYMETADATA")]
        private void Record(string varName, object varObject = null)
        {
            if (_RecordingEnabled) _Container.Record(varName, varObject);
        }
    }

    [System.Runtime.InteropServices.ComVisible(false)]
    public sealed class DefaultPropertiesCollection : List<UDefaultProperty>
    {
        [CanBeNull]
        public UDefaultProperty Find(string name)
        {
            return Find(prop => prop.Name == name);
        }

        [CanBeNull]
        public UDefaultProperty Find(UName name)
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