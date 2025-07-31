using System.Diagnostics.Contracts;
using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.GIGANTIC.Tokens
{
    [ExprToken(ExprToken.MetaCast)]
    public class JsonRefVariableToken : UStruct.UByteCodeDecompiler.Token
    {
        public UStruct.UByteCodeDecompiler.Token Expression;

        /// <summary>
        /// FIXME: Unknown format, appears to be a token wrapper for a "InstanceVariable" where the variable is a JsonRef object.
        /// </summary>
        /// <param name="decompiler"></param>
        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return DecompileNext(decompiler);
        }

        public override void Deserialize(IUnrealStream stream)
        {
            Expression = Script.DeserializeNextToken(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Contract.Assert(Expression != null);
            Script.SerializeToken(stream, Expression);
        }
    }
}
