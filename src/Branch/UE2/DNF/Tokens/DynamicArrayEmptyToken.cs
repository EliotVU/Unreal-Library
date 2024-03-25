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
            DeserializeNext();
        }

        public override string Decompile()
        {
            Decompiler.MarkSemicolon();
            return $"{DecompileNext()}.Empty()";
        }
    }
}
