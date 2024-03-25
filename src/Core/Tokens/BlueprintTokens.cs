namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
#if UE4
            public class LetMulticastDelegateToken : LetDelegateToken
            {
                
            }

            public class LetObjToken : LetToken
            {
                
            }

            public class LetWeakObjPtrToken : LetToken
            {
                
            }

            public class PushExecutionFlowToken : Token
            {
                private int _Size;

                public override void Deserialize( IUnrealStream stream )
                {
                    _Size = stream.ReadInt32();
                    Decompiler.AlignSize( sizeof(int) );
                }

                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    Decompiler._MustCommentStatement = true;
                    return "PUSH " + _Size;
                }
            }

            public class PopExecutionFlowToken : Token
            {
                public override string Decompile()
                {
                    Decompiler._CanAddSemicolon = true;
                    Decompiler._MustCommentStatement = true;
                    return "POP";
                }
            }

            public class TracepointToken : Token
            {
                
            }

            public class WireTracepointToken : Token
            {
                
            }
#endif
        }
    }
}