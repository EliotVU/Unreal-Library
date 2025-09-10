using System;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;
using UELib.UnrealScript;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            [ExprToken(ExprToken.IntZero)]
            public class IntZeroToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return "0";
                }
            }

            [ExprToken(ExprToken.IntOne)]
            public class IntOneToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return "1";
                }
            }

            [ExprToken(ExprToken.True)]
            public class TrueToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return "true";
                }
            }

            [ExprToken(ExprToken.False)]
            public class FalseToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return "false";
                }
            }

            [ExprToken(ExprToken.NoObject)]
            public class NoneToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return "none";
                }
            }

            [ExprToken(ExprToken.Self)]
            public class SelfToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return "self";
                }
            }

            [ExprToken(ExprToken.IntConst)]
            public class IntConstToken : Token
            {
                public int Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadInt32();
                    Script.AlignSize(sizeof(int));
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Value);
                    Script.AlignSize(sizeof(int));
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            [ExprToken(ExprToken.ByteConst)]
            public class ByteConstToken : Token
            {
                public byte Value;

                private enum ENetRole
                {
                    ROLE_None = 0,
                    ROLE_DumbProxy = 1,
                    ROLE_SimulatedProxy = 2,
                    ROLE_AutonomousProxy = 3,
                    ROLE_Authority = 4
                }

                private enum ENetRole3
                {
                    ROLE_None = 0,
                    ROLE_SimulatedProxy = 1,
                    ROLE_AutonomousProxy = 2,
                    ROLE_Authority = 3,
                    ROLE_MAX = 4
                }

                private enum ENetMode
                {
                    NM_Standalone = 0,
                    NM_DedicatedServer = 1,
                    NM_ListenServer = 2,
                    NM_Client = 3,
                    NM_MAX = 4
                }

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadByte();
                    Script.AlignSize(sizeof(byte));
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Value);
                    Script.AlignSize(sizeof(byte));
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    if (decompiler._ObjectHint != null)
                    {
                        switch (decompiler._ObjectHint.Outer.Name + decompiler._ObjectHint.Name)
                        {
                            case "ActorRemoteRole":
                            case "ActorRole":
                                return Enum.GetName(
                                    decompiler.Package.Version >= 220 ? typeof(ENetRole3) : typeof(ENetRole),
                                    Value);

                            case "LevelInfoNetMode":
                            case "WorldInfoNetMode":
                                return Enum.GetName(typeof(ENetMode), Value);
                        }
                    }

                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            [ExprToken(ExprToken.IntConstByte)]
            public class IntConstByteToken : Token
            {
                public byte Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadByte();
                    Script.AlignSize(sizeof(byte));
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Value);
                    Script.AlignSize(sizeof(byte));
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            [ExprToken(ExprToken.FloatConst)]
            public class FloatConstToken : Token
            {
                public float Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadFloat();
                    Script.AlignSize(sizeof(float));
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Value);
                    Script.AlignSize(sizeof(float));
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            // Not supported, but has existed here and there
            [ExprToken(ExprToken.StructConst)]
            public class StructConstToken : Token;

            [ExprToken(ExprToken.ObjectConst)]
            public class ObjectConstToken : Token
            {
                public UObject Value;

                [Obsolete("Use Value instead")] public UObject ObjectRef => Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadObject();
                    Script.AlignObjectSize();
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Value);
                    Script.AlignObjectSize();
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            [ExprToken(ExprToken.NameConst)]
            public class NameConstToken : Token
            {
                public UName Value;

                [Obsolete("Use Value instead")] public UName Name => Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadName();
                    Script.AlignNameSize();
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Value);
                    Script.AlignNameSize();
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            [ExprToken(ExprToken.StringConst)]
            public class StringConstToken : Token
            {
                public string Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadAnsiNullString();
                    Script.AlignSize(Value.Length + 1); // inc null char
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.WriteAnsiNullString(Value);
                    Script.AlignSize(Value.Length + 1); // inc null char
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            [ExprToken(ExprToken.UnicodeStringConst)]
            public class UnicodeStringConstToken : Token
            {
                public string Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadUnicodeNullString();
                    Script.AlignSize(Value.Length * 2 + 2); // inc null char
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.WriteUnicodeNullString(Value);
                    Script.AlignSize(Value.Length * 2 + 2); // inc null char
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            [ExprToken(ExprToken.RotationConst)]
            public class RotationConstToken : Token
            {
                public URotator Rotation;

                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadStruct(out Rotation);
                    Script.AlignSize(12);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.WriteStruct(Rotation);
                    Script.AlignSize(12);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return $"rot({PropertyDisplay.FormatLiteral(Rotation.Pitch)}, " +
                           $"{PropertyDisplay.FormatLiteral(Rotation.Yaw)}, " +
                           $"{PropertyDisplay.FormatLiteral(Rotation.Roll)})";
                }
            }

            [ExprToken(ExprToken.VectorConst)]
            public class VectorConstToken : Token
            {
                public UVector Vector;

                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadStruct(out Vector);
                    Script.AlignSize(12);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.WriteStruct(Vector);
                    Script.AlignSize(12);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return $"vect({PropertyDisplay.FormatLiteral(Vector.X)}, " +
                           $"{PropertyDisplay.FormatLiteral(Vector.Y)}, " +
                           $"{PropertyDisplay.FormatLiteral(Vector.Z)})";
                }
            }

            [ExprToken(ExprToken.RangeConst)]
            public class RangeConstToken : Token
            {
                public URange Range;

                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadStruct(out Range);
                    Script.AlignSize(8);
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.WriteStruct(Range);
                    Script.AlignSize(8);
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    return
                        $"rng({PropertyDisplay.FormatLiteral(Range.A)}, " +
                        $"{PropertyDisplay.FormatLiteral(Range.B)})";
                }
            }
        }
    }
}
