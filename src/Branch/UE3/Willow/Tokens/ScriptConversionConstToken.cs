using System;
using UELib.Core;

namespace UELib.Branch.UE3.Willow.Tokens;

public class ScriptConversionConstToken : UStruct.UByteCodeDecompiler.Token
{
    public byte OpCode;

    public override void Deserialize(IUnrealStream stream)
    {
        OpCode = stream.ReadByte();
        Decompiler.AlignSize(sizeof(byte));

        switch (OpCode)
        {
            case 0:
                DeserializeNullTerminatedByteArray();
                break;

            case 1:
                DeserializeNullTerminatedByteArray();
                DeserializeNullTerminatedByteArray();
                DeserializeNullTerminatedByteArray();
                break;

            case 2:
                stream.ReadInt64();
                Decompiler.AlignSize(sizeof(ulong));

                DeserializeNullTerminatedByteArray();
                break;

            case 3:
                DeserializeNullTerminatedByteArray();

                stream.ReadInt64();
                Decompiler.AlignSize(sizeof(ulong));
                break;

            case 4:
            case 5:
            case 6:
                DeserializeNullTerminatedByteArray();
                break;

            default: throw new NotImplementedException("Bad conversion type");
        }

        return;

        void DeserializeNullTerminatedByteArray()
        {
            int count = 0;
            while (stream.ReadByte() != 0)
            {
                count++;
            }

            Decompiler.AlignSize(count + 1);
        }
    }

    public override string Decompile()
    {
        return DecompileNext();
    }
}
