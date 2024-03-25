using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Core
{
    // Not sure what this this byte-code is for; can occur in old Unreal packages.
    [ExprToken(ExprToken.ValidateObject)]
    public class ValidateObjectToken : UStruct.UByteCodeDecompiler.Token
    {
    }
}
