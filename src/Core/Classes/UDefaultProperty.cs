using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UELib.Branch;
using UELib.IO;
using UELib.Services;
using UELib.Types;
using UELib.UnrealScript;

namespace UELib.Core
{
    /// <summary>
    ///     A script property with a tag and an associated value, to help with the re-construction of a <see cref="UProperty"/>'s value.
    /// </summary>
    public sealed class UDefaultProperty : IUnrealDecompilable, IUnrealSerializableClass
    {
        [Flags]
        public enum DeserializeFlags : byte
        {
            None = 0x00,
            WithinStruct = 0x01,
            WithinArray = 0x02,
            Decompiling = 0x04,
        }

        private const byte DoNotAppendName = 0x01;
        private const byte ReplaceNameMarker = 0x02;

        private readonly UObject _TagSource;
        private UStruct? _PropertySource;

        // Temporary solution, needed so we can lazy-load the property value.
        private IUnrealStream _Buffer { get; set; }

        internal long _TagPosition { get; set; }
        internal long _PropertyValuePosition { get; set; }

        private byte _TempFlags { get; set; }

        public UObject TagSource => _TagSource;

        /// <summary>
        ///     The deserialized and decompiled output.
        ///     Serves as a temporary workaround, don't rely on it.
        /// </summary>
        [Obsolete("Will be deprecated when DeserializePropertyValue is fully implemented.")]
        public string Value { get; private set; }

        /// <summary>
        /// A <see cref="UProperty"/> containing the deserialized property's value.
        ///
        /// Null if the value has not been deserialized.
        /// </summary>
        public UProperty? Property => _Property;

        private UProperty? _Property;

        /// <summary>
        /// .NET type of the property value.
        /// </summary>
        private Type _InternalValueType = typeof(void);

        private static Type ToInternalValueType(PropertyType type)
        {
            return type switch
            {
                PropertyType.ByteProperty => typeof(byte),
                PropertyType.IntProperty => typeof(int),
                PropertyType.BoolProperty => typeof(bool),
                PropertyType.FloatProperty => typeof(float),
                PropertyType.ObjectProperty => typeof(UObject),
                PropertyType.NameProperty => typeof(UName),
                PropertyType.StringProperty => typeof(string),
                PropertyType.ClassProperty => typeof(UClass),
                PropertyType.ArrayProperty => typeof(UArray<>),
                PropertyType.StructProperty => typeof(IUnrealSerializableClass),
                PropertyType.StrProperty => typeof(string),
                PropertyType.MapProperty => typeof(UMap<,>),
                PropertyType.FixedArrayProperty => typeof(Array),
                PropertyType.PointerProperty => typeof(IntPtr),
                PropertyType.QwordProperty => typeof(long),
                //PropertyType.XWeakReferenceProperty => typeof(UXWeakReferenceProperty.PropertyValue),
                //PropertyType.JsonRefProperty => typeof(UJsonRefProperty.PropertyValue),
                PropertyType.StringRefProperty => typeof(int),
                PropertyType.BioMask4Property => typeof(byte),
                PropertyType.InterfaceProperty => typeof(UClass),
                PropertyType.ComponentProperty => typeof(UObject),
                //PropertyType.MKItemProperty => expr,
                //PropertyType.MkItemNoDestroyProperty => expr,
                PropertyType.ByteAttributeProperty => typeof(byte),
                PropertyType.FloatAttributeProperty => typeof(float),
                PropertyType.IntAttributeProperty => typeof(int),
                PropertyType.CppCopyStructProperty => typeof(IUnrealSerializableClass),
                PropertyType.None => throw new NotSupportedException("Cannot handle property tag type 'None'"),
                _ => throw new NotImplementedException("Unrecognized property tag type")
            };
        }

        public static explicit operator Type(UDefaultProperty scriptProperty) => scriptProperty._InternalValueType;

        public UPropertyTag Tag => _Tag;
        private UPropertyTag _Tag;

        public UName Name => _Tag.Name;
        public int ArrayIndex => _Tag.ArrayIndex;
        public int Size => _Tag.Size;
        public PropertyType Type => _Tag.Type;

        public UName StructName => _Tag.TypeData.StructName;
        public UName EnumName => _Tag.TypeData.EnumName;
        public UName InnerTypeName => _Tag.TypeData.InnerTypeName;

        public bool BoolValue
        {
            get
            {
                AssertType(PropertyType.BoolProperty);
                return _Tag.BoolValue;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertType(PropertyType type) => Contract.Assert(Type == type);

        [StructLayout(LayoutKind.Explicit)]
        private struct PropertyValueUnion
        {
            [FieldOffset(0)] internal object Any;
            //[FieldOffset(0)] public ValueHost<byte> Byte;
            //[FieldOffset(0)] public ValueHost<UName> Name;
            //[FieldOffset(0)] public ValueHost<int> Int;
            //[FieldOffset(0)] public ValueHost<bool> Bool;
            //[FieldOffset(0)] public ValueHost<long> Long;
            //[FieldOffset(0)] public ValueHost<float> Float;
            //[FieldOffset(0)] public ValueHost<string> String;
            //[FieldOffset(0)] public ValueHost<UObject> Object;
            //[FieldOffset(0)] public ValueHost<UDelegateProperty.PropertyValue> Delegate;
        }

        public string Decompile()
        {
            _TempFlags = 0x00;
            string value;
            try
            {
                value = DeserializeValue(DeserializeFlags.Decompiling);
            }
            catch (Exception exception)
            {
                value = $"/* ERROR: {exception.GetType()} */";

                LibServices.LogService.SilentException(exception);
            }

            // Array or Inlined object
            if ((_TempFlags & DoNotAppendName) != 0)
            // The tag handles the name etc on its own.
            {
                return value;
            }

            string name = Name;
            if (Type == PropertyType.DelegateProperty && _TagSource.Package.Version >= 100)
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
                ? $"{expr}{PropertyDisplay.FormatT3DElementAccess(ArrayIndex.ToString(), _TagSource.Package.Version)}={value}"
                : $"{expr}={value}";
        }

        private T? FindProperty<T>(out UStruct? outer)
            where T : UProperty
        {
            UProperty property = null;
            outer = _PropertySource ?? (UStruct)_TagSource.Class ?? (UClass)_TagSource;
            Debug.Assert(outer != null, nameof(outer) + " != null");
            foreach (var super in UField.EnumerateSuper(outer))
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

            return (T?)property;
        }

        private IntPtr _InternalValuePtr;
        private PropertyValueUnion _ValueRef;

        public UDefaultProperty(UObject tagSource)
        {
            _TagSource = tagSource;
            _PropertySource = tagSource as UStruct ?? (UStruct)tagSource.Class;
            _Tag = new UPropertyTag();

            _Buffer = tagSource.Buffer;
        }

        public UDefaultProperty(
            UObject tagSource,
            UStruct? propertySource,
            ref UPropertyTag tag,
            // ReSharper disable once PreferConcreteValueOverDefault
            IntPtr internalValueDestPtr = default
        )
        {
            _TagSource = tagSource;
            _PropertySource = propertySource;
            _Tag = tag;
            _Property = null;

            _Buffer = tagSource.Buffer;
        }

        // FIXME: Re-do when we merge UELib 2.0 UObject construction, where we have proper object references.
        public UDefaultProperty(
            UObject tagSource,
            UProperty? property,
            ref UPropertyTag tag,
            // ReSharper disable once PreferConcreteValueOverDefault
            IntPtr internalValueDestPtr = default
        )
        {
            _Tag = tag;
            _TagSource = tagSource;
            _PropertySource = (UStruct)property?.Outer;
            _Property = property;
            _InternalValuePtr = internalValueDestPtr;
            if (tag.Type != PropertyType.None) _InternalValueType = ToInternalValueType(tag.Type);

            _Buffer = null;
        }

        [Obsolete("See overload")]
        public bool Deserialize()
        {
            _TagPosition = _Buffer.Position;

            if (Name.IsNone())
            {
                return false;
            }

            _Tag.Deserialize(_Buffer);
            DeserializeProperty(_Buffer);

            return true;
        }

        /// <summary>
        /// Serializes a property tag and value to a stream.
        /// </summary>
        /// <param name="stream">the output stream.</param>
        public void Serialize(IUnrealStream stream)
        {
            _TagPosition = stream.Position;

            _Tag.Serialize(stream);
            SerializeProperty(stream);
            Debug.Assert(stream.Position == _PropertyValuePosition + Size);
        }

        /// <summary>
        /// Deserializes a property tag and value from a stream.
        /// </summary>
        /// <param name="stream">the input stream.</param>
        public void Deserialize(IUnrealStream stream)
        {
            _TagPosition = stream.Position;

            _Tag.Deserialize(stream);
            DeserializeProperty(stream);
            Debug.Assert(stream.Position == _PropertyValuePosition + Size);
        }

        /// <summary>
        /// Serializes the property value against a known <see cref="UProperty"/>
        /// </summary>
        /// <param name="stream">the output stream.</param>
        /// <param name="property">the linked property.</param>
        public void SerializeProperty(IUnrealStream stream, UProperty property)
        {
            Contract.Assert(property != null);
            Contract.Assert(Type == property.Type);

            _Property = property;
            SerializeProperty(stream, property.Type);
        }

        /// <summary>
        /// Deserializes the property value against a known <see cref="UProperty"/>
        /// </summary>
        /// <param name="stream">the input stream.</param>
        /// <param name="property">the linked property.</param>
        public void DeserializeProperty(IUnrealStream stream, UProperty property)
        {
            Contract.Assert(property != null);
            Contract.Assert(Type == property.Type);

            _Property = property;
            DeserializeProperty(stream, property.Type);
        }

        /// <summary>
        /// Serializes the property value against a known <see cref="UProperty"/> type.
        /// </summary>
        /// <param name="stream">the output stream.</param>
        /// <param name="type">the property type.</param>
        public void SerializeProperty(IUnrealStream stream, PropertyType type = default)
        {
            // false for inner values such as those of an array.
            if (type == default)
            {
                type = Type;
                _PropertyValuePosition = stream.Position;
            }

            SerializePropertyValue(stream, type);
        }

        /// <summary>
        /// Deserializes the property value against a known <see cref="UProperty"/> type.
        /// </summary>
        /// <param name="stream">the input stream.</param>
        /// <param name="type">the property type.</param>
        public void DeserializeProperty(IUnrealStream stream, PropertyType type = default)
        {
            // false for inner values such as those of an array.
            if (type == default)
            {
                type = Type;
                _PropertyValuePosition = stream.Position;
            }

            try
            {
                DeserializePropertyValue(stream, type);
            }
            catch (Exception exception)
            {
                LibServices.Debug($"Couldn't deserialize tagged value for '{Name}'");
                //LibServices.LogService.SilentException(
                //new Exception($"Couldn't deserialize tagged value for '{Name}'", exception));
            }
            finally
            {
                // Even if something goes wrong, we can still skip everything and safely deserialize the next property if any!
                // Note: In some builds @Size is not serialized
                stream.Position = _PropertyValuePosition + Size;
                stream.ConformRecordPosition();
            }
        }

        private void SerializePropertyValue(IUnrealStream stream, PropertyType type)
        {
            stream.Write(new byte[Size]);
            //throw new NotImplementedException(
            //    $"SerializePropertyValue for {type} is not implemented yet.");
        }

        private void DeserializePropertyValue(IUnrealStream stream, PropertyType type)
        {
            var flags = DeserializeFlags.None;
            Value = TryLegacyDeserializeDefaultPropertyValue(stream, type, ref flags);
        }

        /// <summary>
        ///     Deserialize the value of this UPropertyTag instance.
        ///     Note:
        ///     Only call after the whole package has been deserialized!
        /// </summary>
        /// <returns>The deserialized value if any.</returns>
        public string DeserializeValue(DeserializeFlags deserializeFlags = DeserializeFlags.None)
        {
            if (string.IsNullOrEmpty(Value) == false && (deserializeFlags & DeserializeFlags.Decompiling) == 0)
            {
                return Value;
            }

            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            _Buffer ??= _TagSource.LoadBuffer();
            _Buffer.Seek(_PropertyValuePosition, SeekOrigin.Begin);
            string output = TryLegacyDeserializeDefaultPropertyValue(_Buffer, Type, ref deserializeFlags);

            LibServices.LogService.SilentAssert(_Buffer.Position == _PropertyValuePosition + Size,
                                                $"PropertyTag value deserialization error for '{Name}: {_Buffer.Position} != {_PropertyValuePosition + Size}");

            return output;
        }

        private string TryLegacyDeserializeDefaultPropertyValue(IUnrealStream stream, PropertyType type,
                                                                ref DeserializeFlags deserializeFlags)
        {
            try
            {
                return LegacyDeserializeDefaultPropertyValue(stream, type, ref deserializeFlags);
            }
            catch (EndOfStreamException)
            {
                // Abort decompilation
                throw;
            }
            catch (Exception exception)
            {
                LibServices.LogService.SilentException(
                    new DeserializationException($"PropertyTag value deserialization error for '{Name}", exception));

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
        /// <param name="type">Kind of type to deserialize.</param>
        /// <returns>The deserialized value if any.</returns>
        private string LegacyDeserializeDefaultPropertyValue(IUnrealStream stream, PropertyType type,
                                                       ref DeserializeFlags deserializeFlags)
        {
            var orgOuter = _PropertySource;
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
                            value = BoolValue;
                        }
                        else
                        {
                            value = stream.ReadByte() > 0;
                        }

                        stream.Record(nameof(value), value);
                        propertyValue = value ? "true" : "false";
                        break;
                    }

                case PropertyType.StrProperty:
                    {
                        string value = stream.ReadString();
                        stream.Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }

                case PropertyType.NameProperty:
                    {
                        var value = stream.ReadName();
                        stream.Record(nameof(value), value);
                        propertyValue = $"\"{value}\"";
                        break;
                    }
#if GIGANTIC
                case PropertyType.JsonRefProperty:
                    {
                        var jsonObjectName = stream.ReadName();
                        var jsonObject = stream.ReadObject<UObject>();

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
                        stream.Read(out int index);
                        stream.Record(nameof(index), index);

                        propertyValue = PropertyDisplay.FormatLiteral(index);
                        break;
                    }

                case PropertyType.BioMask4Property:
                    {
                        stream.Read(out byte value);
                        stream.Record(nameof(value), value);

                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }
#endif
                case PropertyType.IntProperty:
                    {
                        int value = stream.ReadInt32();
                        stream.Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }
#if BIOSHOCK
                case PropertyType.QwordProperty:
                    {
                        long value = stream.ReadInt64();
                        stream.Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }

                case PropertyType.XWeakReferenceProperty:
                    propertyValue = "/* XWeakReference: (?=" + stream.ReadName() + ",?=" + stream.ReadName() +
                                    ",?=" + stream.ReadByte() + ",?=" + stream.ReadName() + ") */";
                    break;
#endif
                case PropertyType.FloatProperty:
                    {
                        float value = stream.ReadFloat();
                        stream.Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }

                case PropertyType.ByteProperty:
                    {
                        if (stream.Version >= (uint)PackageObjectLegacyVersion.EnumTagNameAddedToBytePropertyTag
                            && Type == PropertyType.ByteProperty
                            // Cannot compare size with 1 because this byte value may be part of a struct.
                            && stream.Position + 1 != _PropertyValuePosition + Size)
                        {
                            string enumTagName = stream.ReadName();
                            stream.Record(nameof(enumTagName), enumTagName);
                            propertyValue = EnumName.IsNone()
                                ? enumTagName
                                : $"{EnumName}.{enumTagName}";
                        }
                        else
                        {
                            byte value = stream.ReadByte();
                            stream.Record(nameof(value), value);
                            propertyValue = PropertyDisplay.FormatLiteral(value);
                        }

                        break;
                    }

                case PropertyType.InterfaceProperty:
                    {
                        var interfaceClass = stream.ReadObject();
                        stream.Record(nameof(interfaceClass), interfaceClass);
                        propertyValue = PropertyDisplay.FormatLiteral(interfaceClass);
                        break;
                    }

                case PropertyType.ComponentProperty:
                case PropertyType.ObjectProperty:
                    {
                        var constantObject = stream.ReadObject();
                        stream.Record(nameof(constantObject), constantObject);
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
                        bool shouldInline = constantObject.Outer == _TagSource
                                            && !isPendingInline
                                            && !isInlined;
                        if (shouldInline && (deserializeFlags & DeserializeFlags.Decompiling) != 0)
                        {
                            if ((deserializeFlags & DeserializeFlags.WithinStruct) == 0)
                            {
                                UDecompilingState.s_inlinedSubObjects.Add(constantObject, true);

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
                        var classObject = stream.ReadObject<UClass>();
                        stream.Record(nameof(classObject), classObject);
                        propertyValue = PropertyDisplay.FormatLiteral(classObject);
                        break;
                    }

                // Old StringProperty with a fixed size and null termination.
                case PropertyType.StringProperty when stream.Version < 100:
                    {
                        string str = stream.ReadAnsiNullString();
                        propertyValue = str;
                        break;
                    }

                case PropertyType.DelegateProperty when stream.Version >= 100:
                    {
                        // Can by any object, usually a class.
                        var functionOwner = stream.ReadObject();
                        stream.Record(nameof(functionOwner), functionOwner);

                        string functionName = stream.ReadName();
                        stream.Record(nameof(functionName), functionName);

                        // Can be null in UE3 packages
                        propertyValue = functionOwner != null
                            ? $"{functionOwner.Name}.{functionName}"
                            : $"{functionName}";
                        break;
                    }

                #region HardCoded Struct Types

                case PropertyType.Color:
                    {
                        stream.ReadStructMarshal(out UColor color);
                        propertyValue += $"R={PropertyDisplay.FormatLiteral(color.R)}," +
                                         $"G={PropertyDisplay.FormatLiteral(color.G)}," +
                                         $"B={PropertyDisplay.FormatLiteral(color.B)}," +
                                         $"A={PropertyDisplay.FormatLiteral(color.A)}";
                        break;
                    }

                case PropertyType.LinearColor:
                    {
                        stream.ReadStructMarshal(out ULinearColor color);
                        propertyValue += $"R={PropertyDisplay.FormatLiteral(color.R)}," +
                                         $"G={PropertyDisplay.FormatLiteral(color.G)}," +
                                         $"B={PropertyDisplay.FormatLiteral(color.B)}," +
                                         $"A={PropertyDisplay.FormatLiteral(color.A)}";
                        break;
                    }

                case PropertyType.Vector2D:
                    {
                        stream.ReadStructMarshal(out UVector2D vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}";
                        break;
                    }

                case PropertyType.Vector:
                    {
                        stream.ReadStructMarshal(out UVector vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(vector.Z)}";
                        break;
                    }

                case PropertyType.Vector4:
                    {
                        stream.ReadStructMarshal(out UVector4 vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(vector.Z)}," +
                                         $"W={PropertyDisplay.FormatLiteral(vector.W)}";
                        break;
                    }

                case PropertyType.TwoVectors:
                    {
                        string v1 = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Vector, ref deserializeFlags);
                        string v2 = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Vector, ref deserializeFlags);
                        propertyValue += $"v1=({v1})," +
                                         $"v2=({v2})";
                        break;
                    }

                case PropertyType.Rotator:
                    {
                        stream.ReadStructMarshal(out URotator rotator);
                        propertyValue += $"Pitch={rotator.Pitch}," +
                                         $"Yaw={rotator.Yaw}," +
                                         $"Roll={rotator.Roll}";
                        break;
                    }

                case PropertyType.Guid:
                    {
                        stream.ReadStructMarshal(out UGuid guid);
                        propertyValue += $"A={guid.A}," +
                                         $"B={guid.B}," +
                                         $"C={guid.C}," +
                                         $"D={guid.D}";
                        break;
                    }

                case PropertyType.Sphere:
                    {
                        AssertFastSerialize(stream);
                        stream.ReadStructMarshal(out USphere sphere);
                        propertyValue += $"W={PropertyDisplay.FormatLiteral(sphere.W)}," +
                                         $"X={PropertyDisplay.FormatLiteral(sphere.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(sphere.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(sphere.Z)}";

                        break;
                    }

                case PropertyType.Plane:
                    {
                        AssertFastSerialize(stream);
                        stream.ReadStructMarshal(out UPlane plane);
                        propertyValue += $"W={PropertyDisplay.FormatLiteral(plane.W)}," +
                                         $"X={PropertyDisplay.FormatLiteral(plane.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(plane.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(plane.Z)}";
                        break;
                    }

                case PropertyType.Scale:
                    {
                        stream.ReadStructMarshal(out UScale scale);
                        propertyValue += "Scale=(" +
                                         $"X={PropertyDisplay.FormatLiteral(scale.Scale.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(scale.Scale.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(scale.Scale.Z)})," +
                                         $"SheerRate={PropertyDisplay.FormatLiteral(scale.SheerRate)}," +
                                         $"SheerAxis={scale.SheerAxis}";
                        break;
                    }

                case PropertyType.Box:
                    {
                        AssertFastSerialize(stream);
                        string min = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Vector, ref deserializeFlags);
                        string max = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Vector, ref deserializeFlags);
                        string isValid =
                            LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ByteProperty, ref deserializeFlags);
                        propertyValue += $"Min=({min})," +
                                         $"Max=({max})," +
                                         $"IsValid={isValid}";
                        break;
                    }

                case PropertyType.Quat:
                    {
                        AssertFastSerialize(stream);
                        stream.ReadStructMarshal(out UQuat quat);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(quat.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(quat.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(quat.Z)}," +
                                         $"W={PropertyDisplay.FormatLiteral(quat.W)}";
                        break;
                    }

                case PropertyType.Matrix:
                    {
                        AssertFastSerialize(stream);
                        //stream.ReadAtomicStruct(out UMatrix matrix);
                        string xPlane = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Plane, ref deserializeFlags);
                        string yPlane = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Plane, ref deserializeFlags);
                        string zPlane = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Plane, ref deserializeFlags);
                        string wPlane = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.Plane, ref deserializeFlags);
                        propertyValue += $"XPlane=({xPlane}),YPlane=({yPlane}),ZPlane=({zPlane}),WPlane=({wPlane})";
                        break;
                    }

                case PropertyType.IntPoint:
                    {
                        string x = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, ref deserializeFlags);
                        string y = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, ref deserializeFlags);
                        propertyValue += $"X={x},Y={y}";
                        break;
                    }

                case PropertyType.PointRegion:
                    {
                        string zone =
                            LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ObjectProperty, ref deserializeFlags);
                        string iLeaf =
                            LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, ref deserializeFlags);
                        string zoneNumber =
                            LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ByteProperty, ref deserializeFlags);
                        propertyValue += $"Zone={zone},iLeaf={iLeaf},ZoneNumber={zoneNumber}";
                        break;
                    }

                #endregion

                case PropertyType.StructProperty:
                    {
                        deserializeFlags |= DeserializeFlags.WithinStruct;
#if DNF
                        if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                        {
                            goto nonAtomic;
                        }
#endif
                        // Ugly hack, but this will do for now until this entire function gets "rewritten" :D
                        if (Enum.TryParse(StructName, out PropertyType structPropertyType))
                        {
                            // Not atomic if <=UE2,
                            // TODO: Figure out all non-atomic structs
                            if (stream.Version < (uint)PackageObjectLegacyVersion.FastSerializeStructs)
                            {
                                switch (structPropertyType)
                                {
                                    case PropertyType.Quat:
                                    case PropertyType.Scale: // not available in UE3
                                    case PropertyType.Matrix:
                                    case PropertyType.Box:
                                    case PropertyType.Plane:
                                        goto nonAtomic;

                                    // None of these exist in older packages (UE2 or older). 
                                    case PropertyType.LinearColor:
                                    case PropertyType.Vector2D:
                                    case PropertyType.Vector4:
                                        goto nonAtomic;
                                }
                            }
                            else
                            {
                                switch (structPropertyType)
                                {
                                    //case PropertyType.Coords:
                                    //case PropertyType.Range:
                                    // Deprecated in UDK
                                    case PropertyType.PointRegion:
                                        goto nonAtomic;
                                }
                            }

                            propertyValue +=
                                LegacyDeserializeDefaultPropertyValue(stream, structPropertyType, ref deserializeFlags);
                            goto output;
                        }

                    nonAtomic:
                        // We have to modify the outer so that dynamic arrays within this struct
                        // will be able to find its variables to determine the array type.
                        FindProperty<UProperty>(out var structPropertySource);
                        var structProperties = UStruct.DeserializeScriptProperties(stream, structPropertySource, _TagSource);
                        foreach (var tag in structProperties)
                        {
                            string tagExpr = tag.Name;
                            if (tag.ArrayIndex > 0)
                            {
                                tagExpr += PropertyDisplay.FormatT3DElementAccess(tag.ArrayIndex.ToString(),
                                                                                  stream.Version);
                            }

                            propertyValue += $"{tagExpr}={tag.Value}";

                            if (tag != structProperties.Last())
                            {
                                propertyValue += ",";
                            }
                        }

                    output:
                        propertyValue = propertyValue.Length != 0
                            ? $"({propertyValue})"
                            : "none";
                        break;
                    }

                case PropertyType.ArrayProperty:
                    {
                        int arraySize = stream.ReadLength();
                        stream.Record(nameof(arraySize), arraySize);
                        if (arraySize == 0)
                        {
                            propertyValue = "none";
                            break;
                        }

                        var arrayType = PropertyType.None;
                        if (!InnerTypeName.IsNone() && !Enum.TryParse(InnerTypeName, out arrayType))
                        {
                            throw new Exception(
                                $"Couldn't convert InnerTypeName \"{InnerTypeName}\" to PropertyType");
                        }

                        // Find the property within the outer/owner or its inheritances.
                        // If found it has to modify the outer so structs within this array can find their array variables.
                        // Additionally, we need to know the property to determine the array's type.
                        if (arrayType == PropertyType.None)
                        {
                            var property = FindProperty<UArrayProperty>(out var arrayPropertySource);
                            if (property?.InnerProperty != null)
                            {
                                arrayType = property.InnerProperty.Type;

                                if (arrayType == PropertyType.StructProperty)
                                {
                                    _Tag.TypeData =
                                        new UPropertyTag.PropertyTypeData(
                                            ((UStructProperty)property.InnerProperty).Struct.Name);
                                }
                            }
                            // If we did not find a reference to the associated property(because of imports)
                            // then try to determine the array's type by scanning the defined array types.
                            else if (UnrealConfig.VariableTypes != null && UnrealConfig.VariableTypes.TryGetValue(Name, out var varTuple))
                            {
                                arrayType = varTuple.Item2;
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
                            propertyValue = "/* Array type was not detected. */";
                            break;
                        }

                        deserializeFlags |= DeserializeFlags.WithinArray;
                        if ((deserializeFlags & DeserializeFlags.WithinStruct) != 0)
                        {
                            for (var i = 0; i < arraySize; ++i)
                            {
                                propertyValue += LegacyDeserializeDefaultPropertyValue(stream, arrayType, ref deserializeFlags)
                                                 + (i != arraySize - 1 ? "," : string.Empty);
                            }

                            propertyValue = $"({propertyValue})";
                        }
                        else
                        {
                            for (var i = 0; i < arraySize; ++i)
                            {
                                string elementAccessText =
                                    PropertyDisplay.FormatT3DElementAccess(i.ToString(), stream.Version);
                                string elementValue =
                                    LegacyDeserializeDefaultPropertyValue(stream, arrayType, ref deserializeFlags);
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

                        int count = stream.ReadLength();
                        stream.Record(nameof(count), count);

                        var property = FindProperty<UMapProperty>(out var mapPropertySource);
                        if (property == null)
                        {
                            propertyValue = "// Unable to decompile Map data.";
                            break;
                        }

                        propertyValue = "(";
                        for (int i = 0; i < count; ++i)
                        {
                            propertyValue +=
                                LegacyDeserializeDefaultPropertyValue(stream, property.ValueProperty.Type, ref deserializeFlags);
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
                        var property = FindProperty<UFixedArrayProperty>(out var fixedPropertySource);
                        if (property == null)
                        {
                            propertyValue = "// Unable to decompile FixedArray data.";
                            break;
                        }

                        var innerType = property.InnerProperty.Type;
                        propertyValue = "(";
                        for (int i = 0; i < property.Count; ++i)
                        {
                            propertyValue += LegacyDeserializeDefaultPropertyValue(stream, innerType, ref deserializeFlags);
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
                        int offset = stream.ReadInt32();
                        propertyValue = PropertyDisplay.FormatLiteral(offset);
                        break;
                    }
#if BORDERLANDS2 || BATTLEBORN
                case PropertyType.ByteAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ByteProperty, ref deserializeFlags);

                case PropertyType.IntAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, ref deserializeFlags);

                case PropertyType.FloatAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.FloatProperty, ref deserializeFlags);
#endif
#if BULLETSTORM
                case PropertyType.CppCopyStructProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.StructProperty, ref deserializeFlags);
#endif
                default:
                    throw new Exception($"Unsupported property tag type {type}");
            }

            _PropertySource = orgOuter;
            return propertyValue;
        }

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
