using System.Diagnostics;
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
                public UProperty Property;
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
                        stream.Read(out Property);
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

            [ExprToken(ExprToken.ClassContext)]
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

            [ExprToken(ExprToken.InterfaceContext)]
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

            [ExprToken(ExprToken.StructMember)]
            public class StructMemberToken : Token
            {
                public UField Property;

                /// <summary>
                /// Will be null if not deserialized (<see cref="Version"/> &lt; <see cref="PackageObjectLegacyVersion.StructReferenceAddedToStructMember"/>)
                /// </summary>
                public UStruct Struct;

                public byte IsCopy;
                public byte IsModification;

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
                    if (stream.Version >= (int)PackageObjectLegacyVersion.StructReferenceAddedToStructMember)
                    {
                        Struct = stream.ReadObject<UStruct>();
                        Decompiler.AlignObjectSize();
                        Debug.Assert(Struct != null);
                    }
#if MKKE
                    if (Package.Build == UnrealPackage.GameBuild.BuildName.MKKE)
                    {
                        goto skipToNext;
                    }
#endif
                    if (stream.Version >= (int)PackageObjectLegacyVersion.IsCopyAddedToStructMember)
                    {
                        // Copy?
                        IsCopy = stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }

                    if (stream.Version >= (int)PackageObjectLegacyVersion.IsModificationAddedToStructMember)
                    {
                        // Modification?
                        IsModification = stream.ReadByte();
                        Decompiler.AlignSize(sizeof(byte));
                    }

                skipToNext:

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
