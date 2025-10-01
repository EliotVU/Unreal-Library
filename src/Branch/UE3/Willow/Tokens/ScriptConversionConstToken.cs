using System;
using UELib.Core;

namespace UELib.Branch.UE3.Willow.Tokens;

public class ScriptConversionConstToken : UStruct.UByteCodeDecompiler.Token
{
    public byte OpCode;

    public override void Deserialize(IUnrealStream stream)
    {
        OpCode = stream.ReadByte();
        Script.AlignSize(sizeof(byte));

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
                Script.AlignSize(sizeof(ulong));

                DeserializeNullTerminatedByteArray();
                break;

            case 3:
                DeserializeNullTerminatedByteArray();

                stream.ReadInt64();
                Script.AlignSize(sizeof(ulong));
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

            Script.AlignSize(count + 1);
        }
    }

    // FIXME: Incomplete, unlikely to work correctly
    public override void Serialize(IUnrealStream stream)
    {
        stream.Write(OpCode);
        Script.AlignSize(sizeof(byte));

        switch (OpCode)
        {
            case 0:
                SerializeNullTerminatedByteArray();
                break;

            case 1:
                SerializeNullTerminatedByteArray();
                SerializeNullTerminatedByteArray();
                SerializeNullTerminatedByteArray();
                break;

            case 2:
                stream.Write((ulong)0);
                Script.AlignSize(sizeof(ulong));

                SerializeNullTerminatedByteArray();
                break;

            case 3:
                SerializeNullTerminatedByteArray();

                stream.Write((ulong)0);
                Script.AlignSize(sizeof(ulong));
                break;

            case 4:
            case 5:
            case 6:
                SerializeNullTerminatedByteArray();
                break;

            default: throw new NotImplementedException("Bad conversion type");
        }

        return;

        void SerializeNullTerminatedByteArray()
        {
            stream.Write((byte)0);
            Script.AlignSize(sizeof(byte));
        }
    }

    public override string Decompile(UStruct.UByteCodeDecompiler decompiler)
    {
        return DecompileNext(decompiler);
    }
}
