using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.IO;
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
                private byte _CastOpCode;

                public CastToken CastOpCode { get; protected set; } = CastToken.None;
                public Token Expression;

                private static readonly Dictionary<CastToken, string> CastTypeNameMap =
                    new()
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

                public void GetFriendlyCastName(out string? castTypeName)
                {
                    CastTypeNameMap.TryGetValue(CastOpCode, out castTypeName);
                }

                protected virtual void DeserializeCastToken(IUnrealStream stream)
                {
                    _CastOpCode = stream.ReadByte();
                    Script.AlignSize(sizeof(byte));

                    RemapCastToken(stream);
                }

                protected virtual void SerializeCastToken(IUnrealStream stream)
                {
                    stream.Write(_CastOpCode);
                    Script.AlignSize(sizeof(byte));
                }

                private void RemapCastToken(IUnrealArchive stream)
                {
                    CastOpCode = (CastToken)_CastOpCode;
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
                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    SerializeCastToken(stream);

                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    // Suppress implicit casts
                    switch (CastOpCode)
                    {
                        //case CastToken.IntToFloat:
                        //case CastToken.ByteToInt:
                        case CastToken.InterfaceToObject:
                        case CastToken.ObjectToInterface:
                            return DecompileNext(decompiler);
                    }

                    GetFriendlyCastName(out string? castTypeName);
                    if (castTypeName == null)
                    {
#if GIGANTIC
                        // HACK: Should implement a cast tokens table in the engine branch instead.
                        if (decompiler.Package.Build == UnrealPackage.GameBuild.BuildName.Gigantic)
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
                        if (decompiler.Package.Build == BuildGeneration.SFX)
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
                        if (decompiler.Package.Build == UnrealPackage.GameBuild.BuildName.SA2)
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

                    Services.LibServices.LogService.SilentAssert(castTypeName != null, $"Detected an unresolved token '0x{CastOpCode:X}'.");
                    return $"{castTypeName}({DecompileNext(decompiler)})";
                }
            }

            [ExprToken(ExprToken.PrimitiveCast)]
            public sealed class PrimitiveInlineCastToken : PrimitiveCastToken
            {
                protected override void DeserializeCastToken(IUnrealStream stream)
                {
                    CastOpCode = (CastToken)OpCode;
                }

                // No serialization needed for inline casts.
                protected override void SerializeCastToken(IUnrealStream stream)
                {
                }
            }

            [ExprToken(ExprToken.DynamicCast)]
            public class DynamicCastToken : Token
            {
                public UClass CastClass;
                public Token Expression;

                public override void Deserialize(IUnrealStream stream)
                {
                    CastClass = stream.ReadObject<UClass>();
                    Script.AlignObjectSize();

                    Expression = Script.DeserializeNextToken(stream);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.WriteObject(CastClass);
                    Script.AlignObjectSize();

                    Contract.Assert(Expression != null);
                    Script.SerializeToken(stream, Expression);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return $"{CastClass.Name}({DecompileNext(decompiler)})";
                }
            }

            [ExprToken(ExprToken.MetaCast)]
            public class MetaClassCastToken : DynamicCastToken
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return $"Class<{CastClass.Name}>({DecompileNext(decompiler)})";
                }
            }

            [ExprToken(ExprToken.InterfaceCast)]
            public class InterfaceCastToken : DynamicCastToken;
        }
    }
}
