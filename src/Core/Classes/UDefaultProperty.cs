using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UELib.Branch;
using UELib.Flags;
using UELib.IO;
using UELib.ObjectModel.Annotations;
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

        internal long _TagPosition { get; set; }
        internal long _PropertyValuePosition { get; set; }

        private byte _TempFlags { get; set; }

        public UObject TagSource => _TagSource;

        /// <summary>
        ///     The deserialized and decompiled output.
        ///     Serves as a temporary workaround, don't rely on it.
        /// </summary>
        [Obsolete("Will be deprecated when DeserializePropertyValue is fully implemented.")]
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
#if BIOSHOCK
                PropertyType.QwordProperty => typeof(long),
                //PropertyType.XWeakReferenceProperty => typeof(UXWeakReferenceProperty.PropertyValue),
#endif
#if GIGANTIC
                //PropertyType.JsonRefProperty => typeof(UJsonRefProperty.PropertyValue),
#endif
#if MASS_EFFECT
                PropertyType.StringRefProperty => typeof(int),
                PropertyType.BioMask4Property => typeof(byte),
#endif
                PropertyType.InterfaceProperty => typeof(UClass),
                PropertyType.ComponentProperty => typeof(UObject),
#if MKKE
                //PropertyType.MKItemProperty => expr,
                //PropertyType.MkItemNoDestroyProperty => expr,
#endif
#if BORDERLANDS2 || BATTLEBORN
                PropertyType.ByteAttributeProperty => typeof(byte),
                PropertyType.FloatAttributeProperty => typeof(float),
                PropertyType.IntAttributeProperty => typeof(int),
#endif
#if SA2
                PropertyType.Int64Property => typeof(long),
#endif
#if BULLETSTORM
                PropertyType.CppCopyStructProperty => typeof(IUnrealSerializableClass),
#endif
#if BATMAN
                PropertyType.GuidProperty => typeof(UGuid),
#endif
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

        private IntPtr _InternalValuePtr;
        private PropertyValueUnion _ValueRef;

        public UDefaultProperty(UObject tagSource)
        {
            _TagSource = tagSource;
            _PropertySource = tagSource as UStruct ?? (UStruct)tagSource.Class;
            _Tag = new UPropertyTag();
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
        }

        [Obsolete("See overload")]
        public bool Deserialize()
        {
            var stream = _TagSource.LoadBuffer();
            _TagPosition = stream.Position;

            if (Name.IsNone())
            {
                return false;
            }

            _Tag.Deserialize(stream);
            DeserializeProperty(stream);

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
        /// Deserializes a property tag and skips ahead of the value from a stream.
        /// </summary>
        /// <param name="stream">the input stream.</param>
        public void Deserialize(IUnrealStream stream)
        {
            _TagPosition = stream.Position;

            _Tag.Deserialize(stream);

            if (_TagSource == null || _TagSource.InternalFlags.HasFlag(InternalClassFlags.PreloadTaggedProperties))
            {
                DeserializeProperty(stream);
                Debug.Assert(stream.Position == _PropertyValuePosition + Size);

                return;
            }


            // skip the value
            _PropertyValuePosition = stream.Position;
            stream.Seek(_PropertyValuePosition + Size, SeekOrigin.Begin); // lazy load, call DeserializeProperty to deserialize the value
            stream.ConformRecordPosition();
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
                LibServices.Debug($"Couldn't deserialize tagged value for '{Name}' in object '{TagSource.GetPath()}'");
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
            _Value = TryLegacyDeserializeDefaultPropertyValue(stream, type, flags, Property);
        }

        /// <summary>
        ///     Deserialize the value of this UPropertyTag instance.
        ///     Note:
        ///     Only call after the whole package has been deserialized!
        /// </summary>
        /// <returns>The deserialized value if any.</returns>
        public string DeserializeValue(DeserializeFlags deserializeFlags = DeserializeFlags.None)
        {
            if (string.IsNullOrEmpty(_Value) == false && (deserializeFlags & DeserializeFlags.Decompiling) == 0)
            {
                return _Value;
            }

            // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
            var stream = _TagSource.LoadBuffer();
            stream.Seek(_PropertyValuePosition, SeekOrigin.Begin);
            stream.ConformRecordPosition();

            // Temporary, this should be handled in the linking phase during the construction of property tags.
            var tagProperty = Property ?? Type switch
            {
                // Resolve for byte property, so that we can re-construct the enum tag for older package builds.
                PropertyType.ByteProperty => _PropertySource?.FindProperty<UByteProperty?>(Name),
                // Resolve, so that we can check if the property has 'export'
                PropertyType.ObjectProperty => _PropertySource?.FindProperty<UObjectProperty?>(Name),
                PropertyType.StructProperty => _PropertySource?.FindProperty<UStructProperty?>(Name),
                PropertyType.ArrayProperty => _PropertySource?.FindProperty<UArrayProperty?>(Name),
                PropertyType.FixedArrayProperty => _PropertySource?.FindProperty<UFixedArrayProperty?>(Name),
                PropertyType.MapProperty => _PropertySource?.FindProperty<UMapProperty?>(Name),
                _ => null
            };

            string output = TryLegacyDeserializeDefaultPropertyValue(stream, Type, deserializeFlags, tagProperty);

            LibServices.LogService.SilentAssert(stream.Position == _PropertyValuePosition + Size,
                $"PropertyTag value size error for '{TagSource.GetPath()}.{Name}: Expected: {Size}, Actual: {stream.Position - _PropertyValuePosition}");

            return output;
        }

        private string TryLegacyDeserializeDefaultPropertyValue(IUnrealStream stream,
                                                                PropertyType type,
                                                                DeserializeFlags deserializeFlags,
                                                                UProperty? property = null)
        {
            try
            {
                return LegacyDeserializeDefaultPropertyValue(stream, type, deserializeFlags, property);
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
        private string LegacyDeserializeDefaultPropertyValue(IUnrealStream stream,
                                                             PropertyType type,
                                                             DeserializeFlags deserializeFlags,
                                                             UProperty? property = null)
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
                        var jsonObject = stream.ReadObject<UObject?>();

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

                            var byteProperty = property as UByteProperty; // Use 'as', because it is possible for types to mismatch.
                            if (byteProperty?.Enum != null)
                            {
                                var enumTag = byteProperty.Enum.Names.ElementAtOrDefault(value);
                                propertyValue = EnumName.IsNone()
                                                // Could also use Enum.Outer.Name, but older UnrealScript generations do not allow for a qualified enum tag.
                                                ? $"{enumTag}"
                                                : $"{EnumName}.{enumTag}";
                            }
                            else
                            {
                                propertyValue = PropertyDisplay.FormatLiteral(value);
                            }
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
                        var constantObject = stream.ReadObject<UObject?>();
                        stream.Record(nameof(constantObject), constantObject);

                        if (constantObject == null)
                        {
                            // =none
                            propertyValue = "none";
                            break;
                        }

                        Debug.Assert(UDecompilingState.s_inlinedSubObjects != null,
                            "UDecompilingState.s_inlinedSubObjects != null");

                        bool isPendingInline = UDecompilingState
                            .s_inlinedSubObjects
                            .TryGetValue(constantObject, out bool isInlined);
                        bool shouldExport = (property as UObjectProperty)?.HasPropertyFlag(PropertyFlag.ExportObject) == true;
                        // If the object is part of the current container, then it probably was an inlined declaration.
                        bool shouldInline = (constantObject.Outer == _TagSource || shouldExport)
                                            && !isPendingInline
                                            && !isInlined;
                        if (shouldInline && (deserializeFlags & DeserializeFlags.Decompiling) != 0)
                        {
                            // Preload if needed.
                            if (constantObject.DeserializationState == 0)
                            {
                                constantObject.Load();
                            }

                            if ((deserializeFlags & DeserializeFlags.WithinStruct) == 0)
                            {
                                UDecompilingState.s_inlinedSubObjects.TryAdd(constantObject, true);

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
                            UDecompilingState.s_inlinedSubObjects.TryAdd(constantObject, false);
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
                        var classObject = stream.ReadObject<UClass?>();
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
                        var functionOwner = stream.ReadObject<UObject?>();
                        stream.Record(nameof(functionOwner), functionOwner);

                        string functionName = stream.ReadName();
                        stream.Record(nameof(functionName), functionName);

                        // Can be null in UE3 packages
                        propertyValue = functionOwner != null
                            ? $"{functionOwner.Name}.{functionName}"
                            : $"{functionName}";
                        break;
                    }

                case PropertyType.StructProperty:
                    {
                        // For arrays within a struct.
                        var structProperty = (UStructProperty?)property;
                        UStruct? structPropertySource = structProperty?.Struct;

                        deserializeFlags |= DeserializeFlags.WithinStruct;

                        if (UStructProperty.PropertyValueSerializer.CanSerializeStructUsingBinary(stream))
                        {
                            // Some structs are serialized using tags.
                            // Let's find out if this is one of them.
                            Enum.TryParse(StructName,
                                out UStructProperty.PropertyValueSerializer.BinaryStructType binaryStructType);
                            if (UStructProperty.PropertyValueSerializer.IsStructImmutable(stream, structPropertySource,
                                    binaryStructType))
                            {
                                // Deserialize using the atomic serializer.
                                propertyValue +=
                                    LegacyDeserializeDefaultBinaryStructValue(stream, binaryStructType,
                                        deserializeFlags);

                                goto output;
                            }

                            // fall-back to non-atomic struct deserialization; may fail if the struct is an unknown immutable/atomic struct.
                        }

                    nonAtomic:
                        var scriptProperty =
                            UStruct.DeserializeNextScriptProperty(stream, structPropertySource, TagSource);
                        if (scriptProperty == null)
                        {
                            return "()";
                        }

                        propertyValue += DecompileTag(scriptProperty);
                        while ((scriptProperty =
                                   UStruct.DeserializeNextScriptProperty(stream, structPropertySource, TagSource)) !=
                               null)
                        {
                            propertyValue += ",";
                            propertyValue += DecompileTag(scriptProperty);
                        }

                        string DecompileTag(UDefaultProperty scriptProperty)
                        {
                            scriptProperty.Deserialize(stream);

                            UProperty? structMemberProperty = scriptProperty.Type switch
                            {
                                PropertyType.StructProperty => structPropertySource?.FindProperty<UStructProperty?>(
                                    scriptProperty.Name),
                                PropertyType.ArrayProperty => structPropertySource?.FindProperty<UArrayProperty?>(
                                    scriptProperty.Name),
                                PropertyType.FixedArrayProperty => structPropertySource
                                    ?.FindProperty<UFixedArrayProperty?>(scriptProperty.Name),
                                PropertyType.MapProperty => structPropertySource?.FindProperty<UMapProperty?>(
                                    scriptProperty.Name),
                                _ => null
                            };

                            string tagExpr = scriptProperty.Name;
                            if (structMemberProperty?.ElementSize > 1 || scriptProperty.ArrayIndex > 0)
                            {
                                tagExpr += PropertyDisplay.FormatT3DElementAccess(scriptProperty.ArrayIndex.ToString(),
                                    stream.Version);
                            }

                            stream.Seek(scriptProperty._PropertyValuePosition, SeekOrigin.Begin);
                            stream.ConformRecordPosition();
                            string value = scriptProperty.TryLegacyDeserializeDefaultPropertyValue(
                                stream, scriptProperty.Type,
                                deserializeFlags, structMemberProperty);

                            return $"{tagExpr}={value}";
                        }

                    output:
                        propertyValue = $"({propertyValue})";
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

                        var innerArrayType = PropertyType.None;
                        if (!InnerTypeName.IsNone() && !Enum.TryParse(InnerTypeName, out innerArrayType))
                        {
                            throw new Exception(
                                $"Couldn't convert InnerTypeName \"{InnerTypeName}\" to PropertyType");
                        }

                        var arrayProperty = (UArrayProperty?)property;
                        var innerArrayProperty = arrayProperty?.InnerProperty;
                        if (innerArrayType == PropertyType.None)
                        {
                            innerArrayType = innerArrayProperty?.Type ?? PropertyType.None;

                            // If we did not find a reference to the associated property(because of imports)
                            // then try to determine the array's type by scanning the defined array types.
                            if (innerArrayType == PropertyType.None &&
                                UnrealConfig.VariableTypes != null &&
                                UnrealConfig.VariableTypes.TryGetValue(Name, out var varTuple))
                            {
                                innerArrayType = varTuple.Item2;
                            }
                        }

                        if (innerArrayType == PropertyType.None)
                        {
                            LibServices.LogService.Log(
                                $"Couldn't acquire array type for property tag '{Name}' in {TagSource}.");

                            propertyValue = "/* Array type was not detected. */";
                            break;
                        }

                        deserializeFlags |= DeserializeFlags.WithinArray;
                        if ((deserializeFlags & DeserializeFlags.WithinStruct) != 0)
                        {
                            for (var i = 0; i < arraySize; ++i)
                            {
                                propertyValue += LegacyDeserializeDefaultPropertyValue(stream, innerArrayType,
                                                     deserializeFlags, innerArrayProperty)
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
                                    LegacyDeserializeDefaultPropertyValue(stream, innerArrayType, deserializeFlags,
                                        innerArrayProperty);
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

                        var mapProperty = (UMapProperty?)property;
                        if (mapProperty == null)
                        {
                            propertyValue = "// Unable to decompile Map data.";
                            break;
                        }

                        propertyValue = "(";
                        for (int i = 0; i < count; ++i)
                        {
                            propertyValue +=
                                LegacyDeserializeDefaultPropertyValue(stream, mapProperty.ValueProperty.Type,
                                    deserializeFlags, mapProperty.ValueProperty);
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
                        var fixedArrayProperty = (UFixedArrayProperty?)property;
                        if (fixedArrayProperty == null)
                        {
                            propertyValue = "// Unable to decompile FixedArray data.";
                            break;
                        }

                        var innerType = fixedArrayProperty.InnerProperty.Type;
                        propertyValue = "(";
                        for (int i = 0; i < fixedArrayProperty.Count; ++i)
                        {
                            propertyValue += LegacyDeserializeDefaultPropertyValue(stream, innerType, deserializeFlags,
                                fixedArrayProperty.InnerProperty);
                            if (i + 1 != fixedArrayProperty.Count)
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

                // Legacy fall-back for batman etc
                case PropertyType.Vector:
                    {
                        propertyValue = $"({LegacyDeserializeDefaultBinaryStructValue(
                            stream,
                            UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags)})";
                        break;
                    }

                // Legacy fall-back for batman etc
                case PropertyType.Rotator:
                    {
                        propertyValue = $"({LegacyDeserializeDefaultBinaryStructValue(
                            stream,
                            UStructProperty.PropertyValueSerializer.BinaryStructType.Rotator, deserializeFlags)})";
                        break;
                    }
#if BATMAN
                case PropertyType.GuidProperty:
                    {
                        propertyValue = $"({LegacyDeserializeDefaultBinaryStructValue(
                            stream,
                            UStructProperty.PropertyValueSerializer.BinaryStructType.Guid, deserializeFlags)})";
                        break;
                    }
#endif
#if BORDERLANDS2 || BATTLEBORN
                case PropertyType.ByteAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ByteProperty, deserializeFlags,
                        property);

                case PropertyType.IntAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, deserializeFlags,
                        property);

                case PropertyType.FloatAttributeProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.FloatProperty, deserializeFlags,
                        property);
#endif
#if BULLETSTORM
                case PropertyType.CppCopyStructProperty:
                    return LegacyDeserializeDefaultPropertyValue(stream, PropertyType.StructProperty, deserializeFlags,
                        property);
#endif
#if SA2
                case PropertyType.Int64Property:
                    {
                        long value = stream.ReadInt64();
                        stream.Record(nameof(value), value);
                        propertyValue = PropertyDisplay.FormatLiteral(value);
                        break;
                    }
#endif
                default:
                    throw new Exception($"Unsupported property tag type {type}");
            }

            _PropertySource = orgOuter;
            return propertyValue;
        }

        private string LegacyDeserializeDefaultBinaryStructValue(
            IUnrealStream stream,
            UStructProperty.PropertyValueSerializer.BinaryStructType structType,
            DeserializeFlags deserializeFlags)
        {
            string propertyValue = string.Empty;

            switch (structType)
            {
                case UStructProperty.PropertyValueSerializer.BinaryStructType.Color:
                    {
                        stream.ReadStructMarshal(out UColor color);
                        if (stream.Version > 69) // FIXME: Version, may need adjustments for UE1
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
                        string origin = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string xAxis = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string yAxis = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string zAxis = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);

                        propertyValue += $"Origin=({origin})," +
                                         $"XAxis=({xAxis})," +
                                         $"YAxis=({yAxis})," +
                                         $"ZAxis=({zAxis})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.LinearColor:
                    {
                        stream.ReadStructMarshal(out ULinearColor color);
                        propertyValue += $"R={PropertyDisplay.FormatLiteral(color.R)}," +
                                         $"G={PropertyDisplay.FormatLiteral(color.G)}," +
                                         $"B={PropertyDisplay.FormatLiteral(color.B)}," +
                                         $"A={PropertyDisplay.FormatLiteral(color.A)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Vector2D:
                    {
                        stream.ReadStructMarshal(out UVector2D vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Vector:
                    {
                        stream.ReadStructMarshal(out UVector vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(vector.Z)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Vector4:
                    {
                        stream.ReadStructMarshal(out UVector4 vector);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(vector.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(vector.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(vector.Z)}," +
                                         $"W={PropertyDisplay.FormatLiteral(vector.W)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.TwoVectors:
                    {
                        string v1 = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string v2 = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        propertyValue += $"v1=({v1})," +
                                         $"v2=({v2})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Rotator:
                    {
                        stream.ReadStructMarshal(out URotator rotator);
                        propertyValue += $"Pitch={rotator.Pitch}," +
                                         $"Yaw={rotator.Yaw}," +
                                         $"Roll={rotator.Roll}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Guid:
                    {
                        stream.ReadStructMarshal(out UGuid guid);
                        propertyValue += $"A={guid.A}," +
                                         $"B={guid.B}," +
                                         $"C={guid.C}," +
                                         $"D={guid.D}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Sphere:
                    {
                        stream.ReadStructMarshal(out USphere sphere);
                        propertyValue += $"W={PropertyDisplay.FormatLiteral(sphere.W)}," +
                                         $"X={PropertyDisplay.FormatLiteral(sphere.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(sphere.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(sphere.Z)}";

                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Plane:
                    {
                        stream.ReadStructMarshal(out UPlane plane);
                        propertyValue += $"W={PropertyDisplay.FormatLiteral(plane.W)}," +
                                         $"X={PropertyDisplay.FormatLiteral(plane.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(plane.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(plane.Z)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Scale:
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

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Box:
                    {
                        string min = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string max = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Vector, deserializeFlags);
                        string isValid = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ByteProperty, deserializeFlags);
                        propertyValue += $"Min=({min})," +
                                         $"Max=({max})," +
                                         $"IsValid={isValid}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Quat:
                    {
                        stream.ReadStructMarshal(out UQuat quat);
                        propertyValue += $"X={PropertyDisplay.FormatLiteral(quat.X)}," +
                                         $"Y={PropertyDisplay.FormatLiteral(quat.Y)}," +
                                         $"Z={PropertyDisplay.FormatLiteral(quat.Z)}," +
                                         $"W={PropertyDisplay.FormatLiteral(quat.W)}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Range:
                    {
                        string min = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.FloatProperty, deserializeFlags);
                        string max = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.FloatProperty, deserializeFlags);
                        propertyValue += $"A={min},B={max}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.RangeVector:
                    {
                        string x = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Range, deserializeFlags);
                        string y = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Range, deserializeFlags);
                        string z = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Range, deserializeFlags);
                        propertyValue += $"X=({x}),Y=({y}),Z=({z})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.Matrix:
                    {
                        //stream.ReadAtomicStruct(out UMatrix matrix);
                        string xPlane = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        string yPlane = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        string zPlane = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        string wPlane = LegacyDeserializeDefaultBinaryStructValue(stream, UStructProperty.PropertyValueSerializer.BinaryStructType.Plane, deserializeFlags);
                        propertyValue += $"XPlane=({xPlane}),YPlane=({yPlane}),ZPlane=({zPlane}),WPlane=({wPlane})";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.IntPoint:
                    {
                        string x = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, deserializeFlags);
                        string y = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, deserializeFlags);
                        propertyValue += $"X={x},Y={y}";
                        break;
                    }

                case UStructProperty.PropertyValueSerializer.BinaryStructType.PointRegion:
                    {
                        string zone = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ObjectProperty, deserializeFlags);
                        string iLeaf = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.IntProperty, deserializeFlags);
                        string zoneNumber = LegacyDeserializeDefaultPropertyValue(stream, PropertyType.ByteProperty, deserializeFlags);
                        propertyValue += $"Zone={zone},iLeaf={iLeaf},ZoneNumber={zoneNumber}";
                        break;
                    }
            }

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
