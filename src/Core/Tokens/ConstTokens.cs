using System;
using System.Globalization;
using UELib.UnrealScript;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public class IntZeroToken : Token
            {
                public override string Decompile()
                {
                    return "0";
                }
            }

            public class IntOneToken : Token
            {
                public override string Decompile()
                {
                    return "1";
                }
            }

            public class TrueToken : Token
            {
                public override string Decompile()
                {
                    return "true";
                }
            }

            public class FalseToken : Token
            {
                public override string Decompile()
                {
                    return "false";
                }
            }

            public class NoneToken : Token
            {
                public override string Decompile()
                {
                    return "none";
                }
            }

            public class SelfToken : Token
            {
                public override string Decompile()
                {
                    return "self";
                }
            }

            public class IntConstToken : Token
            {
                public int Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadInt32();
                    Decompiler.AlignSize(sizeof(int));
                }

                public override string Decompile()
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

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
                    Decompiler.AlignSize(sizeof(byte));
                }

                public override string Decompile()
                {
                    if (FieldToken.LastField != null)
                        switch (FieldToken.LastField.Outer.Name + FieldToken.LastField.Name)
                        {
                            case "ActorRemoteRole":
                            case "ActorRole":
                                return Enum.GetName(Package.Version >= 220 ? typeof(ENetRole3) : typeof(ENetRole),
                                    Value);

                            case "LevelInfoNetMode":
                            case "WorldInfoNetMode":
                                return Enum.GetName(typeof(ENetMode), Value);
                        }

                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            public class IntConstByteToken : Token
            {
                public byte Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.ReadByte();
                    Decompiler.AlignSize(sizeof(byte));
                }

                public override string Decompile()
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            public class FloatConstToken : Token
            {
                public float Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.UR.ReadSingle();
                    Decompiler.AlignSize(sizeof(float));
                }

                public override string Decompile()
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            public class ObjectConstToken : Token
            {
                public UObject ObjectRef;

                public override void Deserialize(IUnrealStream stream)
                {
                    ObjectRef = stream.ReadObject();
                    Decompiler.AlignObjectSize();
                }

                public override string Decompile()
                {
                    return PropertyDisplay.FormatLiteral(ObjectRef);
                }
            }

            public class NameConstToken : Token
            {
                public UName Name;

                public override void Deserialize(IUnrealStream stream)
                {
                    Name = stream.ReadNameReference();
                    Decompiler.AlignNameSize();
                }

                public override string Decompile()
                {
                    return PropertyDisplay.FormatLiteral(Name);
                }
            }

            public class StringConstToken : Token
            {
                public string Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.UR.ReadAnsi();
                    Decompiler.AlignSize(Value.Length + 1); // inc null char
                }

                public override string Decompile()
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            public class UniStringConstToken : Token
            {
                public string Value;

                public override void Deserialize(IUnrealStream stream)
                {
                    Value = stream.UR.ReadUnicode();
                    Decompiler.AlignSize(Value.Length * 2 + 2); // inc null char
                }

                public override string Decompile()
                {
                    return PropertyDisplay.FormatLiteral(Value);
                }
            }

            public class RotationConstToken : Token
            {
                public URotator Rotation;

                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadStruct(out Rotation);
                    Decompiler.AlignSize(12);
                }

                public override string Decompile()
                {
                    return $"rot({PropertyDisplay.FormatLiteral(Rotation.Pitch)}, " +
                           $"{PropertyDisplay.FormatLiteral(Rotation.Yaw)}, " +
                           $"{PropertyDisplay.FormatLiteral(Rotation.Roll)})";
                }
            }

            public class VectorConstToken : Token
            {
                public UVector Vector;

                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadStruct(out Vector);
                    Decompiler.AlignSize(12);
                }

                public override string Decompile()
                {
                    return $"vect({PropertyDisplay.FormatLiteral(Vector.X)}, " +
                           $"{PropertyDisplay.FormatLiteral(Vector.Y)}, " +
                           $"{PropertyDisplay.FormatLiteral(Vector.Z)})";
                }
            }

            public class RangeConstToken : Token
            {
                public URange Range;
                
                public override void Deserialize(IUnrealStream stream)
                {
                    stream.ReadStruct(out Range);
                    Decompiler.AlignSize(8);
                }

                public override string Decompile()
                {
                    return
                        $"rng({PropertyDisplay.FormatLiteral(Range.A)}, " +
                        $"{PropertyDisplay.FormatLiteral(Range.B)})";
                }
            }
        }
    }
}