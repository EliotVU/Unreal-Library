using UELib.Core;

namespace UELib.Branch.UE2.DNF.Tokens
{
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