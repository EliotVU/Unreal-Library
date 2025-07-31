using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorXToken : UStruct.UByteCodeDecompiler.Token
    {
        public float X;

        public override void Deserialize(IUnrealStream stream)
        {
            X = stream.ReadFloat();
            Script.AlignSize(sizeof(float));
        }
        
        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(X);
            Script.AlignSize(sizeof(float));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"vectx({PropertyDisplay.FormatLiteral(X)})";
        }
    }
}