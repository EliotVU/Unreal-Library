using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE3.SFX.Tokens
{
    [ExprToken(ExprToken.StringConst)]
    public class StringRefConstToken : UStruct.UByteCodeDecompiler.Token
    {
        /// <summary>
        /// Index to the string in a Tlk file.
        /// </summary>
        public int Index;

        public override void Deserialize(IUnrealStream stream)
        {
            stream.Read(out Index);
            Script.AlignSize(sizeof(int));
        }

        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Index);
            Script.AlignSize(sizeof(int));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            // FIXME: pseudo syntax
            return $"strref[{Index}]";
        }
    }
}
