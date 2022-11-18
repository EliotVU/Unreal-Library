using static UELib.Core.UStruct.UByteCodeDecompiler;
using UELib.Core.Tokens;
using UELib.Branch.UE2.DNF.Tokens;
using UELib.Core;
using UELib.Tokens;

namespace UELib.Branch.UE2.DNF
{
    [Build(UnrealPackage.GameBuild.BuildName.DNF)]
    public class EngineBranchDNF : DefaultEngineBranch
    {
        public EngineBranchDNF(BuildGeneration generation) : base(generation)
        {
        }

        protected TokenMap BuildTokenMap(UnrealPackage linker)
        {
            return new TokenMap(0x80)
            {
                { 0x00, typeof(LocalVariableToken) },
                { 0x01, typeof(InstanceVariableToken) },
                { 0x02, typeof(DefaultVariableToken) },
                { 0x03, typeof(BadToken) },
                { 0x04, typeof(ReturnToken) },
                { 0x05, typeof(SwitchToken) },
                { 0x06, typeof(JumpToken) },
                { 0x07, typeof(JumpIfNotToken) },
                { 0x08, typeof(StopToken) },
                { 0x09, typeof(AssertToken) },
                { 0x0A, typeof(CaseToken) },
                { 0x0B, typeof(NothingToken) },
                { 0x0C, typeof(LabelTableToken) },
                { 0x0D, typeof(GotoLabelToken) },
                { 0x0E, typeof(EatStringToken) },
                { 0x0F, typeof(LetToken) },
                { 0x10, typeof(DynamicArrayElementToken) },
                { 0x11, typeof(NewToken) },
                { 0x12, typeof(ClassContextToken) },
                { 0x13, typeof(MetaClassCastToken) },
                { 0x14, typeof(LetBoolToken) },
                { 0x15, typeof(BadToken) },
                { 0x16, typeof(EndFunctionParmsToken) },
                { 0x17, typeof(SelfToken) },
                { 0x18, typeof(SkipToken) },
                { 0x19, typeof(ContextToken) },
                { 0x1A, typeof(ArrayElementToken) },
                { 0x1B, typeof(VirtualFunctionToken) },
                { 0x1C, typeof(FinalFunctionToken) },
                { 0x1D, typeof(IntConstToken) },
                { 0x1E, typeof(FloatConstToken) },
                { 0x1F, typeof(StringConstToken) },
                { 0x20, typeof(ObjectConstToken) },
                { 0x21, typeof(NameConstToken) },
                { 0x22, typeof(RotationConstToken) },
                { 0x23, typeof(VectorConstToken) },
                { 0x24, typeof(ByteConstToken) },
                { 0x25, typeof(IntZeroToken) },
                { 0x26, typeof(IntOneToken) },
                { 0x27, typeof(TrueToken) },
                { 0x28, typeof(FalseToken) },
                { 0x29, typeof(NativeParameterToken) },
                { 0x2A, typeof(NoObjectToken) },
                { 0x2B, typeof(BadToken) },
                { 0x2C, typeof(IntConstByteToken) },
                { 0x2D, typeof(BoolVariableToken) },
                { 0x2E, typeof(DynamicCastToken) },
                { 0x2F, typeof(IteratorToken) },
                { 0x30, typeof(IteratorPopToken) },
                { 0x31, typeof(IteratorNextToken) },
                { 0x32, typeof(StructCmpEqToken) },
                { 0x33, typeof(StructCmpNeToken) },
                { 0x34, typeof(UnicodeStringConstToken) },
                { 0x35, typeof(BadToken) },
                { 0x36, typeof(StructMemberToken) },
                { 0x37, typeof(DebugInfoToken) },
                { 0x38, typeof(GlobalFunctionToken) },
                // Primitive casts are downgraded below (we don't want to repeat these because they might be subject to change)
                { 0x39, typeof(BadToken) },
                { 0x3A, typeof(BadToken) },
                { 0x3B, typeof(BadToken) },
                { 0x3C, typeof(BadToken) },
                { 0x3D, typeof(BadToken) },
                { 0x3E, typeof(BadToken) },
                { 0x3F, typeof(BadToken) },
                { 0x40, typeof(BadToken) },
                { 0x41, typeof(BadToken) },
                { 0x42, typeof(BadToken) },
                { 0x43, typeof(BadToken) },
                { 0x44, typeof(BadToken) },
                { 0x45, typeof(BadToken) },
                { 0x46, typeof(BadToken) },
                { 0x47, typeof(BadToken) },
                { 0x48, typeof(BadToken) },
                { 0x49, typeof(BadToken) },
                { 0x4A, typeof(BadToken) },
                { 0x4B, typeof(BadToken) },
                { 0x4C, typeof(BadToken) },
                { 0x4D, typeof(BadToken) },
                { 0x4E, typeof(BadToken) },
                { 0x4F, typeof(BadToken) },
                { 0x50, typeof(BadToken) },
                { 0x51, typeof(BadToken) },
                { 0x52, typeof(BadToken) },
                { 0x53, typeof(BadToken) },
                { 0x54, typeof(BadToken) },
                { 0x55, typeof(BadToken) },
                { 0x56, typeof(BadToken) },
                { 0x57, typeof(BadToken) },
                { 0x58, typeof(BadToken) },
                { 0x59, typeof(BadToken) },
                { 0x5A, typeof(DynamicArrayLengthToken) },
                { 0x5B, typeof(DynamicArrayInsertToken) },
                { 0x5C, typeof(DynamicArrayAddToken) },
                { 0x5D, typeof(DynamicArrayRemoveToken) },
                { 0x5E, typeof(DelegateFunctionToken) },
                { 0x5F, typeof(DelegatePropertyToken) },
                // DNF has extended the ExtendNative tokens set from 0x60 to 0x80.
                { 0x60, typeof(LetDelegateToken) },
                { 0x61, typeof(VectorConstZeroToken) },
                { 0x62, typeof(VectorConstUnitZToken) },
                { 0x63, typeof(RotConstZeroToken) },
                { 0x64, typeof(IntConstWordToken) },
                { 0x65, typeof(RotConstBytesToken) },
                { 0x66, typeof(DynamicArrayEmptyToken) },
                { 0x67, typeof(BreakpointToken) },
                { 0x68, typeof(BadToken) },
                { 0x69, typeof(RotConstPitchToken) },
                { 0x6A, typeof(RotConstYawToken) },
                { 0x6B, typeof(RotConstRollToken) },
                { 0x6C, typeof(VectorXToken) },
                { 0x6D, typeof(VectorYToken) },
                { 0x6E, typeof(VectorZToken) },
                { 0x6F, typeof(VectorXYToken) },
                { 0x70, typeof(VectorXZToken) },
                { 0x71, typeof(VectorYZToken) },
                { 0x72, typeof(BadToken) },
                { 0x73, typeof(BadToken) },
                { 0x74, typeof(BadToken) },
                { 0x75, typeof(BadToken) },
                { 0x76, typeof(BadToken) },
                { 0x77, typeof(BadToken) },
                { 0x78, typeof(BadToken) },
                { 0x79, typeof(BadToken) },
                { 0x7A, typeof(BadToken) },
                { 0x7B, typeof(BadToken) },
                { 0x7C, typeof(BadToken) },
                { 0x7D, typeof(BadToken) },
                { 0x7E, typeof(BadToken) },
                { 0x7F, typeof(BadToken) },
            };
        }

        protected override void SetupTokenFactory(UnrealPackage linker)
        {
            var tokenMap = BuildTokenMap(linker);
            // DNF uses UE1 casting byte-codes (probably because under the hood the engine was upgraded from UE1 to UE2)
            DowngradePrimitiveCasts(tokenMap);
            // Undo downgrade, now this raises the question, which byte-code is set for DelegateToString if there is one?
            tokenMap[(byte)CastToken.DelegateToString] = typeof(DynamicArrayLengthToken);
            SetupTokenFactory<TokenFactory>(
                tokenMap, 
                TokenFactory.FromPackage(linker.NTLPackage), 
                0x80, 
                0x90);
        }
    }
}
