using UELib.Branch.UE2.DNF.Tokens;
using UELib.Branch.UE2.DVS.Tokens;
using UELib.Core;
using UELib.Core.Tokens;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE2.DVS
{
    public class EngineBranchDVS : DefaultEngineBranch
    {
        public EngineBranchDVS(BuildGeneration generation) : base(BuildGeneration.UE2)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);
            tokenMap[0x05] = typeof(SwitchToken);
            tokenMap[0x12] = typeof(ClassContextToken);
            tokenMap[0x19] = typeof(ContextToken);
            tokenMap[0x1B] = typeof(VirtualFunctionToken);
            tokenMap[0x1C] = typeof(FinalFunctionToken);
            tokenMap[0x38] = typeof(GlobalFunctionToken);
            tokenMap[0x43] = typeof(DelegateFunctionToken);
            tokenMap[0x46] = typeof(DynamicArrayEmptyToken);
            tokenMap[0x47] = typeof(UStruct.UByteCodeDecompiler.DynamicArraySortToken);
            tokenMap[0x48] = typeof(UStruct.UByteCodeDecompiler.ConditionalToken);
            tokenMap[0x49] = typeof(ColorConstToken);

            // cast token, 0x5B StructToString

            return tokenMap;
        }
    }

    [ExprToken(ExprToken.Switch)]
    public sealed class SwitchToken : UStruct.UByteCodeDecompiler.SwitchToken
    {
        // DVS: Missing PropertyType
        public override void Deserialize(IUnrealStream stream) => DeserializeNext();
    }

    [ExprToken(ExprToken.FinalFunction)]
    public sealed class FinalFunctionToken : UStruct.UByteCodeDecompiler.FinalFunctionToken
    {
        protected override void DeserializeCall(IUnrealStream stream)
        {
            uint skipSize = stream.ReadUInt16();
            Decompiler.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }
    }

    [ExprToken(ExprToken.VirtualFunction)]
    public sealed class VirtualFunctionToken : UStruct.UByteCodeDecompiler.VirtualFunctionToken
    {
        protected override void DeserializeCall(IUnrealStream stream)
        {
            uint skipSize = stream.ReadUInt16();
            Decompiler.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }
    }

    [ExprToken(ExprToken.GlobalFunction)]
    public sealed class GlobalFunctionToken : UStruct.UByteCodeDecompiler.GlobalFunctionToken
    {
        protected override void DeserializeCall(IUnrealStream stream)
        {
            uint skipSize = stream.ReadUInt16();
            Decompiler.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }
    }

    [ExprToken(ExprToken.DelegateFunction)]
    public sealed class DelegateFunctionToken : UStruct.UByteCodeDecompiler.DelegateFunctionToken
    {
        protected override void DeserializeCall(IUnrealStream stream)
        {
            uint skipSize = stream.ReadUInt16();
            Decompiler.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }
    }

    [ExprToken(ExprToken.Context)]
    public sealed class ContextToken : UStruct.UByteCodeDecompiler.ContextToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // A.?
            DeserializeNext();

            uint skipSize = stream.ReadUInt16();
            Decompiler.AlignSize(sizeof(ushort));

            // ?.B
            DeserializeNext();
        }
    }

    [ExprToken(ExprToken.ClassContext)]
    public sealed class ClassContextToken : UStruct.UByteCodeDecompiler.ClassContextToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // A.?
            DeserializeNext();

            uint skipSize = stream.ReadUInt16();
            Decompiler.AlignSize(sizeof(ushort));

            // ?.B
            DeserializeNext();
        }
    }
}
