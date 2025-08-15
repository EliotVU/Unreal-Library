using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE3.SA2.Tokens
{
    public class Int64ConstToken : UStruct.UByteCodeDecompiler.Token
    {
        public long Value;

        public override void Deserialize(IUnrealStream stream)
        {
            Value = stream.ReadInt64();
            Script.AlignSize(sizeof(long));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Value);
            Script.AlignSize(sizeof(long));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return PropertyDisplay.FormatLiteral(Value);
        }
    }
}
