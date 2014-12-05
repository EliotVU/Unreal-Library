using System;

namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
            public abstract class FieldToken : Token
            {
                public UObject Object{ get; private set; }
                public static UObject LastField{ get; internal set; }

                public override void Deserialize( IUnrealStream stream )
                {
                    Object = Decompiler._Container.TryGetIndexObject( stream.ReadObjectIndex() );
                    Decompiler.AlignObjectSize();
                }

                public override string Decompile()
                {
                    LastField = Object;
                    return Object != null ? Object.Name : "@NULL";
                }
            }

            public class NativeParameterToken : FieldToken
            {
                public override string Decompile()
                {
#if DEBUG
                    Decompiler._CanAddSemicolon = true;
                    Decompiler._MustCommentStatement = true;
                    return "native." + base.Decompile();
#else
                    return String.Empty;
#endif
                }
            }

            public class InstanceVariableToken : FieldToken{}
            public class LocalVariableToken : FieldToken{}
            public class StateVariableToken : FieldToken{}
            public class OutVariableToken : FieldToken{}

            public class DefaultVariableToken : FieldToken
            {
                public override string Decompile()
                {
                    return "default." + base.Decompile();
                }
            }

            public class DynamicVariableToken : Token
            {
                protected int LocalIndex;

                public override void Deserialize( IUnrealStream stream )
                {
                    LocalIndex = stream.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                }

                public override string Decompile()
                {
                    return "UnknownLocal_" + LocalIndex;
                }
            }

            public class UndefinedVariableToken : Token
            {
                public override string Decompile()
                {
                    return String.Empty;
                }
            }

            public class DelegatePropertyToken : FieldToken
            {
                public int NameIndex;

                public override void Deserialize( IUnrealStream stream )
                {
                    // FIXME: MOHA or general?
                    if( stream.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(int) );
                    }

                    // Unknown purpose.
                    NameIndex = stream.ReadNameIndex();
                    Decompiler.AlignNameSize();

                    // TODO: Corrigate version. Seen in version ~648(The Ball) may have been introduced earlier, but not prior 610.
                    if( stream.Version > 610 )
                    {
                        base.Deserialize( stream );
                    }
                }

                public override string Decompile()
                {
                    return Decompiler._Container.Package.GetIndexName( NameIndex );
                }
            }

            public class DefaultParameterToken : Token
            {
                internal static int     _NextParamIndex;
                private UField          _NextParam
                {
                    get{ try{return ((UFunction)Decompiler._Container).Params[_NextParamIndex++];}catch{return null;} }
                }

                public override void Deserialize( IUnrealStream stream )
                {
                    stream.ReadUInt16();    // Size
                    Decompiler.AlignSize( sizeof(ushort) );

                    // FIXME: MOHA or general?
                    if( stream.Version == 421 )
                    {
                        Decompiler.AlignSize( sizeof(ushort) );
                    }

                    DeserializeNext();  // Expression
                    DeserializeNext();  // EndParmValue
                }

                public override string Decompile()
                {
                    string expression = DecompileNext();
                    DecompileNext();    // EndParmValue
                    Decompiler._CanAddSemicolon = true;
                    var param = _NextParam;
                    var paramName = param != null ? param.Name : "@UnknownOptionalParam_" + (_NextParamIndex - 1);
                    return String.Format( "{0} = {1}", paramName, expression );
                }
            }

            public class BoolVariableToken : Token
            {
                public override void Deserialize( IUnrealStream stream )
                {
                    DeserializeNext();
                }

                public override string Decompile()
                {
                    return DecompileNext();
                }
            }

            public class InstanceDelegateToken : Token
            {
                public override void Deserialize( IUnrealStream stream )
                {
                    stream.ReadNameIndex();
                    Decompiler.AlignNameSize();
                }
            }
        }
    }
}