using System;
using System.Globalization;

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
                public int Value{ get; private set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    Value = stream.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                }

                public override string Decompile()
                {
                    return String.Format( "{0}", Value );
                }
            }

            public class ByteConstToken : Token
            {
                public byte Value{ get; private set; }

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

                public override void Deserialize( IUnrealStream stream )
                {
                    Value = stream.ReadByte();
                    Decompiler.AlignSize( sizeof(byte) );
                }

                public override string Decompile()
                {
                    if( FieldToken.LastField != null )
                    {
                        switch( FieldToken.LastField.Outer.Name + FieldToken.LastField.Name )
                        {
                            case "ActorRemoteRole":
                            case "ActorRole":
                                return Enum.GetName( Package.Version >= 220 ? typeof(ENetRole3) : typeof(ENetRole), Value );

                            case "LevelInfoNetMode":
                            case "WorldInfoNetMode":
                                return Enum.GetName( typeof(ENetMode), Value );
                        }
                    }
                    return Value.ToString( CultureInfo.InvariantCulture );
                }
            }

            public class IntConstByteToken : Token
            {
                public byte Value{ get; private set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    Value = stream.ReadByte();
                    Decompiler.AlignSize( sizeof(byte) );
                }

                public override string Decompile()
                {
                    return String.Format( "{0:d}", Value );
                }
            }

            public class FloatConstToken : Token
            {
                public float Value{ get; private set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    Value = stream.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                }

                public override string Decompile()
                {
                    return Value.ToUFloat();
                }
            }

            public class ObjectConstToken : Token
            {
                public int ObjectIndex{ get; private set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    ObjectIndex = stream.ReadObjectIndex();
                    Decompiler.AlignObjectSize();
                }

                public override string Decompile()
                {
                    UObject obj = Decompiler._Container.GetIndexObject( ObjectIndex );
                    if( obj != null )
                    {
                        // class'objectclasshere'
                        string Class = obj.GetClassName();
                        if( String.IsNullOrEmpty( Class ) )
                        {
                            Class = "class";
                        }
                        return Class.ToLower() + "'" + obj.Name + "'";
                    }
                    return "none";
                }
            }

            public class NameConstToken : Token
            {
                public int NameIndex{ get; private set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    NameIndex = stream.ReadNameIndex();
                    Decompiler.AlignNameSize();
                }

                public override string Decompile()
                {
                    return "\'" + Decompiler._Container.Package.GetIndexName( NameIndex ) + "\'";
                }
            }

            public class StringConstToken : Token
            {
                public string Value{ get; private set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    Value = stream.UR.ReadAnsi();
                    Decompiler.AlignSize( Value.Length + 1 );   // inc null char
                }

                public override string Decompile()
                {
                    return "\"" + Value.Escape() + "\"";
                }
            }

            public class UniStringConstToken : Token
            {
                public string Value{ get; private set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    Value = stream.UR.ReadUnicode();
                    Decompiler.AlignSize( (Value.Length * 2) + 2 ); // inc null char
                }

                public override string Decompile()
                {
                    return "\"" + Value.Escape() + "\"";
                }
            }

            public class RotatorConstToken : Token
            {
                public struct Rotator
                {
                    public int Pitch, Yaw, Roll;
                }

                public Rotator Value;

                public override void Deserialize( IUnrealStream stream )
                {
                    Value.Pitch = stream.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                    Value.Yaw = stream.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                    Value.Roll = stream.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                }

                public override string Decompile()
                {
                    return "rot(" + Value.Pitch + ", " + Value.Yaw + ", " + Value.Roll + ")";
                }
            }

            public class VectorConstToken : Token
            {
                public float X, Y, Z;

                public override void Deserialize( IUnrealStream stream )
                {
                    X = stream.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                    Y = stream.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                    Z = stream.UR.ReadSingle();
                    Decompiler.AlignSize( sizeof(float) );
                }

                public override string Decompile()
                {
                    return String.Format( "vect({0}, {1}, {2})", X.ToUFloat(), Y.ToUFloat(), Z.ToUFloat() );
                }
            }
        }
    }
}