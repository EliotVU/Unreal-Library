using UELib.Core;

namespace UELib.Branch.UE3.SA2.Tokens
{
    public class DelegateFunctionToken : UStruct.UByteCodeDecompiler.FunctionToken
    {
        public UProperty DelegateProperty;
        public UName FunctionName;

        public override void Deserialize(IUnrealStream stream)
        {
            DelegateProperty = stream.ReadObject<UProperty>();
            Decompiler.AlignObjectSize();

            FunctionName = DeserializeFunctionName(stream);
            DeserializeCall(stream);
        }

        public override string Decompile()
        {
            Decompiler.MarkSemicolon();
            return DecompileCall(FunctionName);
        }
    }
}