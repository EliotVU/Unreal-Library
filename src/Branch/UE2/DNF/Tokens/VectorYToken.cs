using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class VectorYToken : UStruct.UByteCodeDecompiler.Token
    {
        public float Y;

        public override void Deserialize(IUnrealStream stream)
        {
            Y = stream.ReadFloat();
            Script.AlignSize(sizeof(float));
        }
        
        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Y);
            Script.AlignSize(sizeof(float));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return $"vecty({PropertyDisplay.FormatLiteral(Y)})";
        }
    }
}