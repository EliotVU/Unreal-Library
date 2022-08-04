using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE2.DNF.Tokens
{
    public class IntConstWordToken : UStruct.UByteCodeDecompiler.Token
    {
        public ushort Value;

        public override void Deserialize(IUnrealStream stream)
        {
            Value = stream.ReadUInt16();
            Decompiler.AlignSize(sizeof(ushort));
        }

        public override string Decompile()
        {
            return PropertyDisplay.FormatLiteral(Value);
        }
    }
}