using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
                    if (stream.Version >= VInterfaceClass) return;
                    
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
                    Debug.Assert(castTypeName != default, "Detected an unresolved token.");
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
                    return $"class<{CastClass.Name}>({DecompileNext()})";
                }
            }

            [ExprToken(ExprToken.InterfaceCast)]
            public class InterfaceCastToken : DynamicCastToken
            {
            }
        }
    }
}
