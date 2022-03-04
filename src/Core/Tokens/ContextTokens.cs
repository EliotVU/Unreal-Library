namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public class ContextToken : Token
            {
                // Definitely not in UT3(512), APB, CrimeCraft, GoW2, MoonBase and Singularity.
                // Greater or Equal than
                private const ushort VSizeByteMoved = 588;

                public override void Deserialize(IUnrealStream stream)
                {
                    bool propertyAdded = stream.Version >= VSizeByteMoved
#if TERA
                                         && stream.Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
#if TRANSFORMERS
                                         && stream.Package.Build != UnrealPackage.GameBuild.BuildName.Transformers
#endif
                        ;

                    // A.?
                    DeserializeNext();

                    // SkipSize
                    stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));

                    // Doesn't seem to exist in APB
                    if (propertyAdded)
                    {
                        // Property
                        stream.ReadObjectIndex();
                        Decompiler.AlignObjectSize();
                    }

                    // PropertyType
                    stream.ReadByte();
                    Decompiler.AlignSize(sizeof(byte));

                    // Additional byte in APB?
                    if (stream.Version > 512 && !propertyAdded)
                    {
                        stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }

                    // ?.B
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return $"{DecompileNext()}.{DecompileNext()}";
                }
            }

            public class ClassContextToken : ContextToken
            {
                public override string Decompile()
                {
                    Decompiler._IsWithinClassContext = true;
                    string output = base.Decompile();
                    Decompiler._IsWithinClassContext = false;
                    return output;
                }
            }

            public class InterfaceContextToken : Token
            {
                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return DecompileNext();
                }
            }

            public class StructMemberToken : Token
            {
                public UField MemberProperty;

                public override void Deserialize(IUnrealStream stream)
                {
                    // Property index
                    MemberProperty = Decompiler._Container.TryGetIndexObject(stream.ReadObjectIndex()) as UField;
                    Decompiler.AlignObjectSize();

                    // TODO: Corrigate version. Definitely didn't exist in Roboblitz(369)
                    if (stream.Version > 369)
                    {
                        // Struct index
                        stream.ReadObjectIndex();
                        Decompiler.AlignObjectSize();
#if MKKE
                        if (Package.Build != UnrealPackage.GameBuild.BuildName.MKKE)
                        {
#endif
                            stream.Position++;
                            Decompiler.AlignSize(sizeof(byte));
#if MKKE
                        }
#endif

                        // TODO: Corrigate version. Definitely didn't exist in MKKE(472), first seen in SWG(486).
                        if (stream.Version > 472)
                        {
                            stream.Position++;
                            Decompiler.AlignSize(sizeof(byte));
                        }
                    }

                    // Pre-Context
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return $"{DecompileNext()}.{MemberProperty.Name}";
                }
            }
        }
    }
}