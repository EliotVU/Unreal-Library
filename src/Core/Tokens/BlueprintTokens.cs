namespace UELib.Core
{
    public partial class UStruct
    {
        public partial class UByteCodeDecompiler
        {
#if UE4
            public class LetMulticastDelegateToken : LetDelegateToken;

            public class LetObjToken : LetToken;

            public class LetWeakObjPtrToken : LetToken;

            public class PushExecutionFlowToken : Token
            {
                public int Size;

                public override void Deserialize(IUnrealStream stream)
                {
                    Size = stream.ReadInt32();
                    Script.AlignSize(sizeof(int));
                }

                public override void Serialize(IUnrealStream stream)
                {
                    stream.Write(Size);
                    Script.AlignSize(sizeof(int));
                }

                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();
                    decompiler.MarkCommentStatement();

                    return "PUSH " + Size;
                }
            }

            public class PopExecutionFlowToken : Token
            {
                public override string Decompile(UByteCodeDecompiler decompiler)
                {
                    decompiler.MarkSemicolon();
                    decompiler.MarkCommentStatement();

                    return "POP";
                }
            }

            public class TracepointToken : Token;

            public class WireTracepointToken : Token;
#endif
        }
    }
}