using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UELib.Branch;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.PrimitiveCast)]
            public class PrimitiveCastToken : Token
            {
                public CastToken CastOpCode = CastToken.None;

                [SuppressMessage("ReSharper", "StringLiteralTypo")]
                private static readonly Dictionary<CastToken, string> CastTypeNameMap =
                    new Dictionary<CastToken, string>
                    {
                        //{ CastToken.InterfaceToObject, "" },
                        { CastToken.InterfaceToString, "string" },
                        { CastToken.InterfaceToBool, "bool" },
                        { CastToken.RotatorToVector, "Vector" },
                        { CastToken.ByteToInt, "int" },
                        { CastToken.ByteToBool, "bool" },
                        { CastToken.ByteToFloat, "float" },
                        { CastToken.IntToByte, "byte" },
                        { CastToken.IntToBool, "bool" },
                        { CastToken.IntToFloat, "float" },
                        { CastToken.BoolToByte, "byte" },
                        { CastToken.BoolToInt, "int" },
                        { CastToken.BoolToFloat, "float" },
                        { CastToken.FloatToByte, "byte" },
                        { CastToken.FloatToInt, "int" },
                        { CastToken.FloatToBool, "bool" },
                        //{ CastToken.ObjectToInterface, "" },
                        { CastToken.ObjectToBool, "bool" },
                        { CastToken.NameToBool, "bool" },
                        { CastToken.StringToByte, "byte" },
                        { CastToken.StringToInt, "int" },
                        { CastToken.StringToBool, "bool" },
                        { CastToken.StringToFloat, "float" },
                        { CastToken.StringToVector, "Vector" },
                        { CastToken.StringToRotator, "Rotator" },
                        { CastToken.VectorToBool, "bool" },
                        { CastToken.VectorToRotator, "Rotator" },
                        { CastToken.RotatorToBool, "bool" },
                        { CastToken.ByteToString, "string" },
                        { CastToken.IntToString, "string" },
                        { CastToken.BoolToString, "string" },
                        { CastToken.FloatToString, "string" },
                        { CastToken.ObjectToString, "string" },
                        { CastToken.NameToString, "string" },
                        { CastToken.VectorToString, "string" },
                        { CastToken.RotatorToString, "string" },
                        { CastToken.DelegateToString, "string" },
                        { CastToken.StringToName, "name" }
                    };

                public void GetFriendlyCastName(out string castTypeName)
                {
                    CastTypeNameMap.TryGetValue(CastOpCode, out castTypeName);
                }

                protected virtual void DeserializeCastToken(IUnrealStream stream)
                {
                    CastOpCode = (CastToken)stream.ReadByte();
                    Decompiler.AlignSize(sizeof(byte));
                }

                private void RemapCastToken(IUnrealArchive stream)
                {
                    if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedInterfacesFeature) return;

                    // TODO: Could there be more?
                    switch (CastOpCode)
                    {
                        case CastToken.ObjectToInterface:
                            CastOpCode = CastToken.StringToName;
                            break;
                    }
                }

                public override void Deserialize(IUnrealStream stream)
                {
                    DeserializeCastToken(stream);
                    RemapCastToken(stream);
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    // Suppress implicit casts
                    switch (CastOpCode)
                    {
                        //case CastToken.IntToFloat:
                        //case CastToken.ByteToInt:
                        case CastToken.InterfaceToObject:
                        case CastToken.ObjectToInterface:
                            return DecompileNext();
                    }

                    GetFriendlyCastName(out string castTypeName);
                    if (castTypeName == default)
                    {
#if GIGANTIC
                        // HACK: Should implement a cast tokens table in the engine branch instead.
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.Gigantic)
                        {
                            switch ((uint)CastOpCode)
                            {
                                case 0x32:
                                case 0x33:
                                case 0x34:
                                    // FIXME: Unknown format
                                    castTypeName = $"JsonRef{(uint)CastOpCode:X}";
                                    break;
                            }
                        }
#endif
#if MASS_EFFECT
                        if (Package.Build == BuildGeneration.SFX)
                        {
                            switch ((uint)CastOpCode)
                            {
                                // StringRefToInt
                                case 0x5B:
                                    castTypeName = "int";
                                    break;

                                // StringRefToString
                                case 0x5C:
                                    castTypeName = "string";
                                    break;

                                // IntToStringRef
                                case 0x5D:
                                    castTypeName = "strref";
                                    break;
                            }
                        }
#endif
#if SA2
                        if (Package.Build == UnrealPackage.GameBuild.BuildName.SA2)
                        {
                            switch ((uint)CastOpCode)
                            {
                                // Int64ToString
                                case 0x61:
                                    castTypeName = "string";
                                    break;

                                // IntToInt64
                                case 0x62:
                                    castTypeName = "int64";
                                    break;

                                // Int64ToInt
                                case 0x63:
                                    castTypeName = "int";
                                    break;

                                // StringToInt64
                                case 0x64:
                                    castTypeName = "int64";
                                    break;

                                // Int64ToByte
                                case 0x65:
                                    castTypeName = "byte";
                                    break;

                                // Int64ToBool
                                case 0x66:
                                    castTypeName = "bool";
                                    break;

                                // Int64ToFloat
                                case 0x67:
                                    castTypeName = "float";
                                    break;

                                // BoolToShort
                                case 0x68:
                                    castTypeName = "short";
                                    break;

                                // FloatToShort
                                case 0x69:
                                    castTypeName = "short";
                                    break;

                                // IntToMemCrypt
                                case 0x6A:
                                    castTypeName = "MemCrypt<int>";
                                    break;

                                // FloatToMemCrypt
                                case 0x6B:
                                    castTypeName = "MemCrypt<float>";
                                    break;

                                // StringToMemCrypt
                                case 0x6C:
                                    castTypeName = "MemCrypt<string>";
                                    break;

                                // Int64ToMemCrypt
                                case 0x6D:
                                    castTypeName = "MemCrypt<int64>";
                                    break;
                            }
                        }
#endif
                    }

                    Debug.Assert(castTypeName != default, $"Detected an unresolved token '0x{CastOpCode:X}'.");
                    return $"{castTypeName}({DecompileNext()})";
                }
            }

            [ExprToken(ExprToken.PrimitiveCast)]
            public sealed class PrimitiveInlineCastToken : PrimitiveCastToken
            {
                protected override void DeserializeCastToken(IUnrealStream stream)
                {
                    CastOpCode = (CastToken)OpCode;
                }
            }

            [ExprToken(ExprToken.DynamicCast)]
            public class DynamicCastToken : Token
            {
                public UClass CastClass;

                public override void Deserialize(IUnrealStream stream)
                {
                    CastClass = stream.ReadObject<UClass>();
                    Decompiler.AlignObjectSize();

                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return $"{CastClass.Name}({DecompileNext()})";
                }
            }

            [ExprToken(ExprToken.MetaCast)]
            public class MetaClassCastToken : DynamicCastToken
            {
                public override string Decompile()
                {
                    return $"Class<{CastClass.Name}>({DecompileNext()})";
                }
            }

            [ExprToken(ExprToken.InterfaceCast)]
            public class InterfaceCastToken : DynamicCastToken
            {
            }
        }
    }
}
