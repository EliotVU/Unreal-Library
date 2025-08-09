using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using UELib.Branch;
using UELib.Types;

namespace UELib.Core;

public record struct UPropertyTag : IUnrealSerializableClass
{
    private const byte InfoTypeMask = 0x0F;
    private const byte InfoSizeMask = 0x70;
    private const byte InfoArrayIndexBit = 0x80;

    /// <summary>
    ///     Name of the property.
    /// </summary>
    public UName Name;

    /// <summary>
    ///     Type of the property.
    /// </summary>
    public PropertyType Type;

    private UName _TypeName; // FIXME: Unify, UE3 type.

    /// <summary>
    ///     The storage size of the value.
    /// </summary>
    public int Size;

    /// <summary>
    ///     The element index of the tag e.g. consider this static array: "var Object Elements[2];"
    ///     This defines a static array of 2 elements which would have two tags with this field being the index into that
    ///     array.
    /// </summary>
    public int ArrayIndex = -1;

    public PropertyTypeData TypeData;

    /// <summary>
    ///     The tag value, when the <seealso cref="Type" /> is <see cref="PropertyType.BoolProperty" />
    ///     This is a special case where the value is stored in the tag itself (not at the value position)
    /// </summary>
    public bool BoolValue;

    public UPropertyTag()
    {
    }

    public UPropertyTag(UName name, PropertyType type)
    {
        Name = name;
        Type = type;
        TypeData = new PropertyTypeData(UnrealName.None);

        switch (Type)
        {
            case PropertyType.ByteProperty:
            case PropertyType.StructProperty:
            case PropertyType.ArrayProperty:
                break;
        }
    }

    public UPropertyTag(UProperty property, int arrayIndex = -1)
    {
        Contract.Assert(property != null);

        Name = property.Name;
        Type = property.Type;
        Debug.Assert(property.Class != null);
        _TypeName = property.Class.Name;
        Size = 0; // calculated during serialization.
        ArrayIndex = arrayIndex;

        TypeData = Type switch
        {
            PropertyType.ByteProperty => new PropertyTypeData(((UByteProperty)property).Enum?.Name ?? UnrealName.None),
            PropertyType.StructProperty => new PropertyTypeData(((UStructProperty)property).Struct.Name),
            PropertyType.ArrayProperty => new PropertyTypeData(((UArrayProperty)property).InnerProperty.Name),
            _ => new PropertyTypeData(UnrealName.None)
        };
    }

    public void Deserialize(IUnrealStream stream)
    {
        if (stream.Version < (uint)PackageObjectLegacyVersion.RefactoredPropertyTags)
        {
            DeserializeTagLegacy(stream);

            return;
        }
#if BATMAN
        if (stream.Build == BuildGeneration.RSS &&
            stream.LicenseeVersion > 21)
        {
            DeserializeTagByOffset(stream);

            return;
        }
#endif
        DeserializeTagNew(stream);
    }

    public void Serialize(IUnrealStream stream)
    {
        if (stream.Version < (uint)PackageObjectLegacyVersion.RefactoredPropertyTags)
        {
            SerializeTagLegacy(stream);

            return;
        }
#if BATMAN
        if (stream.Build == BuildGeneration.RSS &&
            stream.LicenseeVersion > 21)
        {
            SerializeTagByOffset(stream);

            return;
        }
#endif
        SerializeTagNew(stream);
    }

    /// <summary>
    ///     Converts the unpacked size to a packed series of bits (0x70)
    /// </summary>
    /// <param name="unpackedSize">the unpacked size</param>
    /// <returns>the packed size bit</returns>
    private static byte ToPackedSize(int unpackedSize)
    {
        return unpackedSize switch
        {
            1 => 0x00,
            2 => 0x10,
            4 => 0x20,
            12 => 0x30,
            16 => 0x40,
            // byte
            <= 255 => 0x50,
            // ushort max value + 1
            <= 65536 => 0x60,
            _ => 0x70
        };
    }

    private void SerializeSize(IUnrealStream stream, int packedSize)
    {
        switch (packedSize)
        {
            case 0x50: stream.Write((byte)Size); break;
            case 0x60: stream.Write((ushort)Size); break;
            case 0x70: stream.Write(Size); break;
        }
    }

    private void DeserializeSize(IUnrealStream stream, byte packedSize)
    {
        Size = packedSize switch
        {
            0x00 => 1,
            0x10 => 2,
            0x20 => 4,
            0x30 => 12,
            0x40 => 16,
            0x50 => stream.ReadByte(),
            0x60 => stream.ReadUInt16(),
            0x70 => stream.ReadInt32(),
            // We covered all possible bits for 0x70
            // anything else must be a mixture which is not allowed.
            _ => throw new NotImplementedException($"Corrupt packed size {packedSize}")
        };
    }

    private void SerializeArrayIndexLegacy(IUnrealStream stream, int arrayIndex)
    {
        var b = (byte)(arrayIndex <= 127
            ? arrayIndex
            : arrayIndex <= 16383
                ? (arrayIndex >> 8) + InfoArrayIndexBit
                : (arrayIndex >> 24) + 0xC0);

        stream.Write(b);

        if ((b & InfoArrayIndexBit) == 0)
        {
            return;
        }

        if ((b & 0xC0) != InfoArrayIndexBit)
        {
            stream.Write((byte)(arrayIndex >> 16));
            stream.Write((byte)(arrayIndex >> 8));
        }

        stream.Write((byte)arrayIndex);
    }

    private void DeserializeArrayIndexLegacy(IUnrealStream stream, out int arrayIndex)
    {
        stream.Read(out byte b);

        if ((b & InfoArrayIndexBit) == 0)
        {
            arrayIndex = b;
        }
        else if ((b & 0xC0) == InfoArrayIndexBit)
        {
            stream.Read(out byte c);
            arrayIndex = ((b & 0x7F) << 8) + c;
        }
        else
        {
            stream.Read(out byte c);
            stream.Read(out byte d);
            stream.Read(out byte e);
            arrayIndex = ((b & 0x3F) << 24) + (c << 16) + (d << 8) + e;
        }
    }

    private void SerializeTagLegacy(IUnrealStream stream)
    {
        Debug.Assert((byte)Type <= InfoTypeMask);

        var packedSize = ToPackedSize(Size);
        var info = (byte)(((byte)Type & InfoTypeMask) | packedSize);

        if (Type == PropertyType.BoolProperty)
        {
            Contract.Assert(ArrayIndex == -1, "UE1 and UE2 does not support arrays of type bool.");

            if (BoolValue) info |= InfoArrayIndexBit;
        }
        else
        {
            if (ArrayIndex != -1) info |= InfoArrayIndexBit;
        }

        stream.Write(info);

        switch (Type)
        {
            case PropertyType.StructProperty:
                Debug.Assert(TypeData.StructName.IsNone() == false, "StructName is required");
                stream.Write(TypeData.StructName);
                break;

            case PropertyType.ArrayProperty:
#if DNF
                if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                    stream.Version >= 124)
                {
                    Debug.Assert(TypeData.InnerTypeName.IsNone() == false, "InnerTypeName is required");
                    stream.Write(TypeData.InnerTypeName);
                }
#endif
                break;
        }

        SerializeSize(stream, packedSize);

        if (ArrayIndex != -1) SerializeArrayIndexLegacy(stream, ArrayIndex);
    }

    private void DeserializeTagLegacy(IUnrealStream stream)
    {
        stream.Read(out byte info);
        stream.Record(nameof(info), info);

        Type = (PropertyType)(byte)(info & InfoTypeMask);
        switch (Type)
        {
            case PropertyType.StructProperty:
                stream.Read(out TypeData.StructName);
                stream.Record(nameof(TypeData.StructName), TypeData.StructName);
                break;

            case PropertyType.ArrayProperty:
            {
#if DNF
                if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                    stream.Version >= 124)
                {
                    stream.Read(out TypeData.InnerTypeName);
                    stream.Record(nameof(TypeData.InnerTypeName), TypeData.InnerTypeName);
                }
#endif
                break;
            }
        }

        DeserializeSize(stream, (byte)(info & InfoSizeMask));
        stream.Record(nameof(Size), Size);

        // TypeData
        switch (Type)
        {
            case PropertyType.BoolProperty:
                BoolValue = (info & InfoArrayIndexBit) != 0;
                break;

            default:
                if ((info & InfoArrayIndexBit) != 0)
                {
                    DeserializeArrayIndexLegacy(stream, out ArrayIndex);
                    stream.Record(nameof(ArrayIndex), ArrayIndex);
                }

                break;
        }
    }

    private void SerializeTagNew(IUnrealStream stream)
    {
        stream.Write(_TypeName);
        stream.Write(Size);
        stream.Write(ArrayIndex);

        SerializeTypeDataNew(stream);
    }

    private void DeserializeTagNew(IUnrealStream stream)
    {
        stream.Read(out UName typeName);
        stream.Record(nameof(typeName), typeName);
        _TypeName = typeName;
        Type = (PropertyType)Enum.Parse(typeof(PropertyType), typeName);

        stream.Read(out Size);
        stream.Record(nameof(Size), Size);

        stream.Read(out ArrayIndex);
        stream.Record(nameof(ArrayIndex), ArrayIndex);

        DeserializeTypeDataNew(stream);
    }

    private void SerializeTypeDataNew(IUnrealStream stream)
    {
        switch (Type)
        {
            case PropertyType.StructProperty:
                Debug.Assert(TypeData.StructName.IsNone() == false, "StructName is required");
                stream.Write(TypeData.StructName);
#if UE4
                if (stream.UE4Version >= 441)
                {
                    throw new NotSupportedException("UE4 v441");
                    stream.Skip(16);
                }
#endif
                break;

            case PropertyType.ByteProperty:
#if BATMAN
                if (stream.Build == BuildGeneration.RSS) break;
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.EnumNameAddedToBytePropertyTag)
                {
                    stream.Write(TypeData.EnumName);
                }

                break;

            case PropertyType.BoolProperty:
#if BORDERLANDS
                // GOTYE didn't apply this upgrade, but did the EnumName update? ...
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands_GOTYE)
                {
                    stream.Write(BoolValue ? (byte)1 : (byte)0);

                    break;
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.BoolValueToByteForBoolPropertyTag)
                {
                    stream.Write(BoolValue ? (byte)1 : (byte)0);
                }
                else
                {
                    stream.Write(BoolValue ? 1 : 0);
                }

                break;

            case PropertyType.ArrayProperty:
#if UE4
                // FIXME: UE4 version
                if (stream.UE4Version > 220)
                {
                    Debug.Assert(TypeData.InnerTypeName.IsNone() == false, "InnerTypeName is required");
                    stream.Write(TypeData.InnerTypeName);
                }
#endif
                break;
        }
    }

    private void DeserializeTypeDataNew(IUnrealStream stream)
    {
        switch (Type)
        {
            case PropertyType.StructProperty:
                stream.Read(out TypeData.StructName);
                stream.Record(nameof(TypeData.StructName), TypeData.StructName);
#if UE4
                if (stream.UE4Version >= 441)
                {
                    stream.Skip(16);
                    stream.ConformRecordPosition();
                }
#endif
                break;

            case PropertyType.ByteProperty:
#if BATMAN
                if (stream.Build == BuildGeneration.RSS) break;
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.EnumNameAddedToBytePropertyTag)
                {
                    stream.Read(out TypeData.EnumName);
                    stream.Record(nameof(TypeData.EnumName), TypeData.EnumName);
                }

                break;

            case PropertyType.BoolProperty:
#if BORDERLANDS
                // GOTYE didn't apply this upgrade, but did the EnumName update? ...
                if (stream.Build == UnrealPackage.GameBuild.BuildName.Borderlands_GOTYE)
                {
                    BoolValue = stream.ReadInt32() > 0;

                    break;
                }
#endif
                if (stream.Version >= (uint)PackageObjectLegacyVersion.BoolValueToByteForBoolPropertyTag)
                {
                    BoolValue = stream.ReadByte() > 0;
                }
                else
                {
                    BoolValue = stream.ReadInt32() > 0;
                }

                stream.Record(nameof(BoolValue), BoolValue);
                break;

            case PropertyType.ArrayProperty:
#if UE4
                // FIXME: UE4 version
                if (stream.UE4Version > 220)
                {
                    stream.Read(out TypeData.InnerTypeName);
                    stream.Record(nameof(TypeData.InnerTypeName), TypeData.InnerTypeName);
                }
#endif
                break;
        }
    }

#if BATMAN
    private void SerializeTagByOffset(IUnrealStream stream)
    {
        if (stream.Build != UnrealPackage.GameBuild.BuildName.Batman3MP)
        {
            throw new NotSupportedException("Cannot serialize property tags by offset.");
            //stream.Write(ushort offset);

            // No serialized size for these types (and possible more)
            if (Type == PropertyType.StrProperty ||
                Type == PropertyType.NameProperty ||
                Type == PropertyType.IntProperty ||
                Type == PropertyType.FloatProperty ||
                Type == PropertyType.StructProperty ||
                Type == PropertyType.Vector ||
                Type == PropertyType.Rotator ||
                (Type == PropertyType.BoolProperty &&
                 stream.Build == UnrealPackage.GameBuild.BuildName.Batman4))
            {
                SerializeTypeDataNew(stream);

                return;
            }
        }

        stream.Write(Size);
        stream.Write(ArrayIndex);

        SerializeTypeDataNew(stream);
    }

    private void DeserializeTagByOffset(IUnrealStream stream)
    {
        if (stream.Build != UnrealPackage.GameBuild.BuildName.Batman3MP)
        {
            stream.Read(out ushort offset);
            stream.Record(nameof(offset), offset);

            // TODO: Incomplete, PropertyTypes' have shifted.
            if ((int)Type == 11) Type = PropertyType.Vector;

            // No serialized size for these types (and possible more)
            if (Type == PropertyType.StrProperty ||
                Type == PropertyType.NameProperty ||
                Type == PropertyType.IntProperty ||
                Type == PropertyType.FloatProperty ||
                Type == PropertyType.StructProperty ||
                Type == PropertyType.Vector ||
                Type == PropertyType.Rotator ||
                (Type == PropertyType.BoolProperty &&
                 stream.Build == UnrealPackage.GameBuild.BuildName.Batman4))
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
                DeserializeTypeDataNew(stream);

                return;
            }
        }

        stream.Read(out Size);
        stream.Record(nameof(Size), Size);

        stream.Read(out ArrayIndex);
        stream.Record(nameof(ArrayIndex), ArrayIndex);

        DeserializeTypeDataNew(stream);
    }
#endif
    [StructLayout(LayoutKind.Explicit)]
    public struct PropertyTypeData(in UName defaultName)
    {
        [FieldOffset(0)] private UName Name = defaultName;

        /// <summary>
        ///     Name of the <see cref="UStruct"/>, when <see cref="UDefaultProperty.Type" /> is <see cref="PropertyType.StructProperty" />.
        /// </summary>
        [FieldOffset(0)] public UName StructName; // Formerly known as "ItemName"

        /// <summary>
        ///     Name of the <see cref="UEnum"/>, when <see cref="UDefaultProperty.Type" /> is <see cref="PropertyType.ByteProperty" />.
        /// </summary>
        [FieldOffset(0)] public UName EnumName;

        /// <summary>
        ///     Name of the <see cref="UArrayProperty.InnerProperty"/>, when <see cref="UDefaultProperty.Type" /> is <see cref="PropertyType.ArrayProperty" />.
        /// </summary>
        [FieldOffset(0)] public UName InnerTypeName;
    }
}
