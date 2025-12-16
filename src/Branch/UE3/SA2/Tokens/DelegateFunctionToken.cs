using System.Diagnostics.Contracts;
using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.SA2.Tokens
{
    [ExprToken(ExprToken.DelegateFunction)]
    public class DelegateFunctionToken : UStruct.UByteCodeDecompiler.FunctionToken
    {
        public UDelegateProperty DelegateProperty;
        public UName FunctionName;

        public override void Deserialize(IUnrealStream stream)
        {
            DelegateProperty = stream.ReadObject<UDelegateProperty>();
            Script.AlignObjectSize();

            FunctionName = DeserializeFunctionName(stream);
            DeserializeCall(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Contract.Assert(DelegateProperty != null);

            stream.WriteObject(DelegateProperty);
            Script.AlignObjectSize();

            SerializeFunctionName(stream, FunctionName);
            SerializeCall(stream);
            Script.SerializeDebugToken(stream, DebugInfo.EFP);
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            decompiler.MarkSemicolon();

            return DecompileCall(FunctionName, decompiler);
        }

        public override UFunction? FunctionCallee => DelegateProperty.Function ?? Script.Source.FindField<UFunction>(FunctionName);
    }
}
