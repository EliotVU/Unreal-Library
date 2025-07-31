using System.Diagnostics.Contracts;
using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE2.DNF.Tokens
{
    [ExprToken(ExprToken.DynArrayEmpty)]
    public class DynamicArrayEmptyToken : UStruct.UByteCodeDecompiler.DynamicArrayMethodToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // Array
            Expression = Script.DeserializeNextToken(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            // Array
            Contract.Assert(Expression != null);
            Script.SerializeToken(stream, Expression);
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            decompiler.MarkSemicolon();

            return $"{DecompileNext(decompiler)}.Empty()";
        }
    }
}
