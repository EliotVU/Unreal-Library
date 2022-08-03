using System.Diagnostics;
using UELib.Annotations;
using UELib.Types;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public class ContextToken : Token
            {
                public ushort PropertyType;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    // A.?
                    DeserializeNext();

                    // SkipSize
                    stream.ReadUInt16();
                    Decompiler.AlignSize(sizeof(ushort));
                    

                    // Doesn't seem to exist in APB
                    // Definitely not in UT3(512), APB, CrimeCraft, GoW2, MoonBase and Singularity.
                    bool propertyAdded = stream.Version >= 588
#if TERA
                                         && stream.Package.Build != UnrealPackage.GameBuild.BuildName.Tera
#endif
                        ;
                    if (propertyAdded)
                    {
                        // Property
                        stream.ReadObjectIndex();
                        Decompiler.AlignObjectSize();
                    }

                    // FIXME: Thinking of it... this appears to be identical to the changes found in SwitchToken, but the existing versions are different?.
                    if ((stream.Version >= 512 && !propertyAdded)
#if DNF
                        || stream.Package.Build == UnrealPackage.GameBuild.BuildName.DNF
#endif
                       )
                    {
                        PropertyType = stream.ReadUInt16();
                        Decompiler.AlignSize(sizeof(ushort));
                    }
                    else
                    {
                        PropertyType = stream.ReadByte();
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
                public UField Property;
                [CanBeNull] public UStruct Struct;

                public override void Deserialize(IUnrealStream stream)
                {
                    Property = stream.ReadObject<UField>();
                    Decompiler.AlignObjectSize();
                    Debug.Assert(Property != null);
#if BIOSHOCK
                    if (Package.Build == UnrealPackage.GameBuild.BuildName.BioShock)
                    {
                        Struct = stream.ReadObject<UStruct>();
                        Decompiler.AlignObjectSize();
                        Debug.Assert(Struct != null);
                    }
#endif
                    // TODO: Corrigate version. Definitely didn't exist in Roboblitz(369)
                    if (stream.Version > 369)
                    {
                        Struct = stream.ReadObject<UStruct>();
                        Decompiler.AlignObjectSize();
                        Debug.Assert(Struct != null);
#if MKKE
                        if (Package.Build != UnrealPackage.GameBuild.BuildName.MKKE)
                        {
#endif
                            // Copy?
                            stream.ReadByte();
                            Decompiler.AlignSize(sizeof(byte));
#if MKKE
                        }
#endif
                    }
                    
                    // TODO: Corrigate version. Definitely didn't exist in MKKE(472), first seen in SWG(486).
                    if (stream.Version > 472)
                    {
                        // Modification?
                        stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }

                    // Pre-Context
                    DeserializeNext();
                }

                public override string Decompile()
                {
#if DEBUG_HIDDENTOKENS
                    if (Struct != null)
                    {
                        Decompiler.PreComment = $"Struct:{Struct.GetOuterGroup()}";
                    }
#endif
                    return $"{DecompileNext()}.{Property.Name}";
                }
            }
        }
    }
}