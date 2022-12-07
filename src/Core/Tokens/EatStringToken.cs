using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    /// <summary>
    /// Proceeded by a string expression.
    /// </summary>
    [ExprToken(ExprToken.EatString)]
    public class EatStringToken : UStruct.UByteCodeDecompiler.Token
    {
        public override void Deserialize(IUnrealStream stream)
        {
            DeserializeNext();
        }

        public override string Decompile()
        {
            return DecompileNext();
        }
    }
}
