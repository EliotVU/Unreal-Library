using System;
using System.Diagnostics;
using System.Linq;
using UELib.Core;
using UELib.Core.Tokens;
using UELib.Flags;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE2.Eon;

public class EonEngineBranch(BuildGeneration generation) : DefaultEngineBranch(generation)
{
    protected override void SetupEnumPropertyFlags(UnrealPackage linker)
    {
        base.SetupEnumPropertyFlags(linker);

        PropertyFlags[(int)PropertyFlag.DuplicateTransient] = 0x00; // 0x00200000U = Saved
    }

    protected override void SetupSerializer(UnrealPackage linker) => SetupSerializer<EonPackageSerializer>();

    protected override TokenMap BuildTokenMap(UnrealPackage linker)
    {
        var tokenMap = base.BuildTokenMap(linker);

        tokenMap[0x00] = typeof(LocalVariableToken);
        tokenMap[0x01] = typeof(InstanceVariableToken);
        tokenMap[0x02] = typeof(DefaultVariableToken);
        tokenMap[0x29] = typeof(NativeParameterToken);
        tokenMap[0x36] = typeof(StructMemberToken);
        tokenMap[0x48] = typeof(OutVariableToken);

        return tokenMap;
    }

    internal static T? SerializeFProperty<T>(IUnrealStream stream) where T : UProperty
    {
        byte type = stream.ReadByte();
        stream.Record(nameof(type), type);

        if (type == 0)
        {
            return null;
        }

        var property = ConstructProperty(type);
        SerializeProperty(stream, property);

        return (T)property;
    }

    private static void SerializeProperty(IUnrealStream stream, UProperty property)
    {
        property.Package = stream.Package;

        // FProperty serialization
        var propertyName = stream.ReadName();
        stream.Record(nameof(propertyName), propertyName);
        property.Name = propertyName;

        var outer = stream.ReadObject<UObject>();
        stream.Record(nameof(outer), outer);
        property.Outer = outer;

        // Maybe ObjectFlags? (To remove)
        ushort unknown = stream.ReadUInt16();
        stream.Record(nameof(unknown), unknown);

        property.ObjectFlags = new UnrealFlags<ObjectFlag>(
            (ulong)(ObjectFlagsLO.Public |
             ObjectFlagsLO.LoadForClient |
             ObjectFlagsLO.LoadForEdit |
             ObjectFlagsLO.LoadForServer
             ) & ~(ulong)unknown, property.Package.Branch.EnumFlagsMap[typeof(ObjectFlag)]);

        byte propertyIndex = stream.ReadByte(); // v04
        stream.Record(nameof(propertyIndex), propertyIndex);

        // UProperty serialization
        property.Deserialize(stream);
    }

    private static UProperty ConstructProperty(byte type) =>
        type switch
        {
            2 => new UByteProperty(),
            3 => new UIntProperty(),
            4 => new UPointerProperty(),
            5 => new UBoolProperty(),
            6 => new UFloatProperty(),
            7 => new UObjectProperty(),
            8 => new UClassProperty(),
            9 => new UNameProperty(),
            10 => new UStrProperty(),
            11 => new UFixedArrayProperty(),
            12 => new UArrayProperty(),
            13 => new UMapProperty(),
            14 => new UStructProperty(),
            15 => new UDelegateProperty(),
            _ => throw new NotSupportedException($"Unsupported property type: {type}")
        };

    public abstract class FieldToken : UStruct.UByteCodeDecompiler.Token
    {
        public UStruct PropertyContainer;
        public int PropertyIndex;

        public override void Deserialize(IUnrealStream stream)
        {
            int property = stream.ReadIndex();
            Script.AlignSize(4);

            PropertyContainer = stream.Package.IndexToObject<UStruct>((short)(property & 0xFFFF));
            PropertyIndex = property >> 16;

            Debug.Assert(PropertyContainer != null);
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            int index = 0;
            var property = PropertyContainer
                .EnumerateFields<UProperty>()
                .FirstOrDefault(_ => index++ == PropertyIndex);

            if (property != null)
            {
                return property.Name;
            }

            decompiler.PreComment = $"// Unresolved import {PropertyContainer.Name} property #[{PropertyIndex}]";
            return $"[{PropertyIndex}]";
        }
    }

    [ExprToken(ExprToken.NativeParm)]
    public class NativeParameterToken : LocalVariableToken
    {
        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
#if DEBUG
            decompiler.MarkSemicolon();
            decompiler.MarkCommentStatement();
            return $"native.{base.Decompile(decompiler)}";
#else
            return string.Empty;
#endif
        }
    }

    [ExprToken(ExprToken.InstanceVariable)]
    public class InstanceVariableToken : FieldToken;

    [ExprToken(ExprToken.LocalVariable)]
    public class LocalVariableToken : FieldToken;

    [ExprToken(ExprToken.OutVariable)]
    public class OutVariableToken : UStruct.UByteCodeDecompiler.Token
    {
        public override void Deserialize(IUnrealStream stream) => Script.DeserializeNextToken(stream);
        public override string Decompile(UStruct.UByteCodeDecompiler decompiler) => DecompileNext(decompiler);
    }

    [ExprToken(ExprToken.DefaultVariable)]
    public class DefaultVariableToken : InstanceVariableToken;

    [ExprToken(ExprToken.StructMember)]
    public class StructMemberToken : UStruct.UByteCodeDecompiler.Token
    {
        public UStruct PropertyContainer;
        public int PropertyIndex;

        public override void Deserialize(IUnrealStream stream)
        {
            int property = stream.ReadIndex();
            Script.AlignSize(4);

            PropertyContainer = stream.Package.IndexToObject<UStruct>((short)(property & 0xFFFF));
            PropertyIndex = property >> 16;

            Debug.Assert(PropertyContainer != null);

            // Pre-Context
            Script.DeserializeNextToken(stream);
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            int index = 0;
            var property = PropertyContainer
                .EnumerateFields<UProperty>()
                .FirstOrDefault(_ => index++ == PropertyIndex);

            if (property != null)
            {
                return $"{DecompileNext(decompiler)}.{property.Name}";
            }

            decompiler.PreComment = $"// Unresolved import {PropertyContainer.Name} property #[{PropertyIndex}]";
            return $"{DecompileNext(decompiler)}[{index}]";
        }
    }
}

