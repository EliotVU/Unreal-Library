using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.GIGANTIC.Tokens
{
    [ExprToken(ExprToken.MetaCast)]
    public class JsonRefVariableToken : UStruct.UByteCodeDecompiler.Token
    {
        /// <summary>
        /// FIXME: Unknown format, appears to be a token wrapper for a "InstanceVariable" where the variable is a JsonRef object.
        /// </summary>
        public override string Decompile()
        {
            return DecompileNext();
        }

        public override void Deserialize(IUnrealStream stream)
        {
            DeserializeNext();
        }
    }
}
