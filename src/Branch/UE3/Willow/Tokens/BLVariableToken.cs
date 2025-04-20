using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.Willow.Tokens
{
    // Maybe the same as the AttributeVariableToken?
    [ExprToken(ExprToken.InstanceVariable)]
    public class BLVariableToken : UStruct.UByteCodeDecompiler.FieldToken;
}
