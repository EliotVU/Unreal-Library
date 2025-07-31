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
            Script.AlignSize(sizeof(ushort));
        }
        
        public override void Serialize(IUnrealStream stream)
        {
            stream.Write(Value);
            Script.AlignSize(sizeof(ushort));
        }

        public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
        {
            return PropertyDisplay.FormatLiteral(Value);
        }
    }
}