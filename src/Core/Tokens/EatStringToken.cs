using System.Diagnostics.Contracts;
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
        public UStruct.UByteCodeDecompiler.Token Expression;

        public override void Deserialize(IUnrealStream stream)
        {
            Expression = Script.DeserializeNextToken(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Contract.Assert(Expression != null);
            Script.SerializeToken(stream, Expression);
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return DecompileNext(decompiler);
        }
    }
}