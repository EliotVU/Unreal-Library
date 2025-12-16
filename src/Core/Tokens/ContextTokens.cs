using System.Diagnostics;
using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.Context)]
            public class ContextToken : Token
            {
                public Token ContextExpression;

                public ushort SkipSize { get; protected set; }

                public UObject? Property;

                /// <summary>
                /// The <see cref="UELib.Types.PropertyType"/> of the property when the <see cref="Property"/> is null.
                /// </summary>
                public byte PropertyType;

                public Token MemberExpression;

                // FIXME: Figure out the EngineVersion for this.
                private static bool HasProperty(IUnrealStream stream)
                {
                    // Doesn't seem to exist in APB
                    // Definitely not in UT3(512), APB, CrimeCraft, GoW2, MoonBase and Singularity.
                    return stream.Version >= 588
#if TERA
                           && stream.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                        ;
                }

                // FIXME: Figure out the EngineVersion for this.
                private static bool Has2BytePropertyType(IUnrealStream stream)
                {
                    // FIXME: Thinking of it... this appears to be identical to the changes found in SwitchToken, but the existing versions are different?.
                    // Not attested with UT3(512), first attested with Mirrors Edge (536)
                    return (stream.Version > 512 && !HasProperty(stream))
#if DNF
                           || stream.Build == UnrealPackage.GameBuild.BuildName.DNF
#endif
                        ;
                }

                public override void Deserialize(IUnrealStream stream)
                {
                    // A.?
                    ContextExpression = Script.DeserializeNextToken(stream);

                    SkipSize = stream.ReadUInt16();
                    Script.AlignSize(sizeof(ushort));

                    if (HasProperty(stream))
                    {
                        stream.Read(out Property);
                        Script.AlignObjectSize();
                    }

                    if (Has2BytePropertyType(stream))
                    {
                        PropertyType = (byte)stream.ReadUInt16();
                        Script.AlignSize(sizeof(ushort));
                    }
                    else
                    {
                        PropertyType = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                    }

                    // ?.B
                    MemberExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(ContextExpression != null);
                    Script.SerializeToken(stream, ContextExpression);

                    // SkipSize
                    long skipSizePeek = stream.Position;
                    stream.Write((ushort)0);
                    Script.AlignSize(sizeof(ushort));

                    if (HasProperty(stream))
                    {
                        stream.WriteObject(Property);
                        Script.AlignObjectSize();
                    }

                    if (Has2BytePropertyType(stream))
                    {
                        stream.Write((ushort)PropertyType);
                        Script.AlignSize(sizeof(ushort));
                    }
                    else
                    {
                        stream.Write(PropertyType);
                        Script.AlignSize(sizeof(byte));
                    }

                    int memorySize = Script.MemorySize;
                    Contract.Assert(MemberExpression != null);
                    Script.SerializeToken(stream, MemberExpression);

                    using (stream.Peek(skipSizePeek))
                    {
                        SkipSize = (ushort)(Script.MemorySize - memorySize);
                        stream.Write(SkipSize);
                    }
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    if (Property is UConst uConst)
                    {
                        decompiler.PreComment = $"Const:{uConst.Value}";

                        return $"{DecompileNext(decompiler)}.{Property.Name}";
                    }

                    return $"{DecompileNext(decompiler)}.{DecompileNext(decompiler)}";
                }
            }

            [ExprToken(ExprToken.ClassContext)]
            public class ClassContextToken : ContextToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.Context.IsStatic = true;
                    string output = base.Decompile(decompiler);
                    decompiler.Context.IsStatic = false;

                    return output;
                }
            }

            [ExprToken(ExprToken.InterfaceContext)]
            public class InterfaceContextToken : Token
            {
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return DecompileNext(decompiler);
                }
            }

            [ExprToken(ExprToken.StructMember)]
            public class StructMemberToken : Token
            {
                public UField Property;

                /// <summary>
                /// Will be null if not deserialized (<see cref="Version"/> &lt; <see cref="PackageObjectLegacyVersion.StructReferenceAddedToStructMember"/>)
                /// </summary>
                public UStruct? Struct;

                public byte? IsCopy;
                public byte? IsModification;

                public Token ContextExpression;

                public override void Deserialize(IUnrealStream stream)
                {
                    Property = stream.ReadObject<UField>();
                    Debug.Assert(Property != null);
                    Script.AlignObjectSize();
#if BIOSHOCK
                    if (stream.Build == UnrealPackage.GameBuild.BuildName.BioShock)
                    {
                        Struct = stream.ReadObject<UStruct>();
                        Debug.Assert(Struct != null);
                        Script.AlignObjectSize();
                    }
#endif
                    if (stream.Version >= (int)PackageObjectLegacyVersion.StructReferenceAddedToStructMember)
                    {
                        Struct = stream.ReadObject<UStruct>();
                        Debug.Assert(Struct != null);
                        Script.AlignObjectSize();
                    }
#if MKKE
                    if (stream.Build == UnrealPackage.GameBuild.BuildName.MKKE)
                    {
                        goto skipToNext;
                    }
#endif
                    if (stream.Version >= (int)PackageObjectLegacyVersion.IsCopyAddedToStructMember)
                    {
                        IsCopy = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                    }

                    if (stream.Version >= (int)PackageObjectLegacyVersion.IsModificationAddedToStructMember)
                    {
                        IsModification = stream.ReadByte();
                        Script.AlignSize(sizeof(byte));
                    }

                skipToNext:

                    ContextExpression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    Contract.Assert(Property != null);
                    stream.WriteObject(Property);
                    Script.AlignObjectSize();
#if BIOSHOCK
                    if (stream.Build == UnrealPackage.GameBuild.BuildName.BioShock)
                    {
                        Contract.Assert(Struct != null);
                        stream.WriteObject(Struct);
                        Script.AlignObjectSize();
                    }
#endif
                    if (stream.Version >= (int)PackageObjectLegacyVersion.StructReferenceAddedToStructMember)
                    {
                        Contract.Assert(Struct != null);
                        stream.WriteObject(Struct);
                        Script.AlignObjectSize();
                    }
#if MKKE
                    if (stream.Build == UnrealPackage.GameBuild.BuildName.MKKE)
                    {
                        goto skipToNext;
                    }
#endif
                    if (stream.Version >= (int)PackageObjectLegacyVersion.IsCopyAddedToStructMember)
                    {
                        stream.Write(IsCopy.Value);
                        Script.AlignSize(sizeof(byte));
                    }

                    if (stream.Version >= (int)PackageObjectLegacyVersion.IsModificationAddedToStructMember)
                    {
                        stream.Write(IsModification.Value);
                        Script.AlignSize(sizeof(byte));
                    }

                skipToNext:

                    Contract.Assert(ContextExpression != null);
                    Script.SerializeToken(stream, ContextExpression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
#if DEBUG_HIDDENTOKENS
                    if (Struct != null)
                    {
                        Decompiler.PreComment = $"Struct:{Struct.GetOuterGroup()}";
                    }
#endif
                    return $"{DecompileNext(decompiler)}.{Property.Name}";
                }
            }
        }
    }
}
