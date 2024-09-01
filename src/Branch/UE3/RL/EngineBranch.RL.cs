using System;
using UELib.Core.Tokens;
using UELib.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.RL
{
    public class EngineBranchRL : DefaultEngineBranch
    {
        [Flags]
        public enum FunctionFlagsRL : ulong
        {
            Constructor = 0x400000000,
        }
        
        public EngineBranchRL(BuildGeneration generation) : base(BuildGeneration.UE3)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            if (linker.LicenseeVersion < 32)
            {
                return base.BuildTokenMap(linker);
            }

            var tokenMap = new TokenMap((byte)ExprToken.ExtendedNative)
            {
                { 0x00, typeof(UnresolvedToken) },
                { 0x01, typeof(NothingToken) },
                { 0x02, typeof(BadToken) },
                { 0x03, typeof(BadToken) },
                { 0x04, typeof(UnresolvedToken) },
                { 0x05, typeof(IntZeroToken) },
                { 0x06, typeof(UnresolvedToken) },
                { 0x07, typeof(BadToken) },
                { 0x08, typeof(UnresolvedToken) },
                { 0x09, typeof(UnresolvedToken) },
                { 0x0A, typeof(FloatConstToken) },
                { 0x0B, typeof(OutVariableToken) },
                { 0x0C, typeof(VirtualFunctionToken) },
                { 0x0D, typeof(BadToken) },
                { 0x0E, typeof(UnresolvedToken) },
                { 0x0F, typeof(UnresolvedToken) },
                { 0x10, typeof(UnresolvedToken) },
                { 0x11, typeof(UnresolvedToken) },
                { 0x12, typeof(UnresolvedToken) },
                { 0x13, typeof(BadToken) },
                { 0x14, typeof(BadToken) },
                { 0x15, typeof(UnresolvedToken) },
                { 0x16, typeof(NewToken) },
                { 0x17, typeof(UnresolvedToken) },
                { 0x18, typeof(UnresolvedToken) },
                { 0x19, typeof(UnresolvedToken) },
                { 0x1A, typeof(UnresolvedToken) },
                { 0x1B, typeof(UnresolvedToken) },
                { 0x1C, typeof(UnresolvedToken) },
                { 0x1D, typeof(UnresolvedToken) },
                { 0x1E, typeof(UnresolvedToken) },
                { 0x1F, typeof(UnresolvedToken) },
                { 0x20, typeof(UnresolvedToken) },
                { 0x21, typeof(UnresolvedToken) },
                { 0x22, typeof(UnresolvedToken) },
                { 0x23, typeof(UnresolvedToken) },
                { 0x24, typeof(UnresolvedToken) },
                { 0x25, typeof(UnresolvedToken) },
                { 0x26, typeof(InstanceVariableToken) },
                { 0x27, typeof(UnresolvedToken) },
                { 0x28, typeof(BadToken) },
                { 0x29, typeof(ReturnToken) },
                { 0x2A, typeof(BadToken) },
                { 0x2B, typeof(EndOfScriptToken) },
                { 0x2C, typeof(BadToken) },
                { 0x2D, typeof(UnresolvedToken) },
                { 0x2E, typeof(UnresolvedToken) },
                { 0x2F, typeof(BadToken) },
                { 0x30, typeof(ClassContextToken) },
                { 0x31, typeof(BadToken) },
                { 0x32, typeof(UnresolvedToken) },
                { 0x33, typeof(BadToken) },
                { 0x34, typeof(UnresolvedToken) },
                { 0x35, typeof(UnresolvedToken) },
                { 0x36, typeof(UnresolvedToken) },
                { 0x37, typeof(StringConstToken) },
                { 0x38, typeof(BadToken) },
                { 0x39, typeof(BadToken) },
                { 0x3A, typeof(UnresolvedToken) },
                { 0x3B, typeof(FinalFunctionToken) },
                { 0x3C, typeof(UnresolvedToken) },
                { 0x3D, typeof(UnresolvedToken) },
                { 0x3E, typeof(ContextToken) },
                { 0x3F, typeof(UnresolvedToken) },
                { 0x40, typeof(UnresolvedToken) },
                { 0x41, typeof(PrimitiveCastToken) },
                { 0x42, typeof(UnresolvedToken) },
                { 0x43, typeof(UnresolvedToken) },
                { 0x44, typeof(UnresolvedToken) },
                { 0x45, typeof(UnresolvedToken) },
                { 0x46, typeof(BadToken) },
                { 0x47, typeof(UnresolvedToken) },
                { 0x48, typeof(BadToken) },
                { 0x49, typeof(BadToken) },
                { 0x4A, typeof(UnresolvedToken) },
                { 0x4B, typeof(UnresolvedToken) },
                { 0x4C, typeof(LetBoolToken) },
                { 0x4D, typeof(BadToken) },
                { 0x4E, typeof(BadToken) },
                { 0x4F, typeof(BadToken) },
                { 0x50, typeof(UnresolvedToken) },
                { 0x51, typeof(UnresolvedToken) },
                { 0x52, typeof(BadToken) },
                { 0x53, typeof(BadToken) },
                { 0x54, typeof(NativeParameterToken) },
                { 0x55, typeof(UnicodeStringConstToken) },
                { 0x56, typeof(UnresolvedToken) },
                { 0x57, typeof(BadToken) },
                { 0x58, typeof(UnresolvedToken) },
                { 0x59, typeof(UnresolvedToken) },
                { 0x5A, typeof(BoolVariableToken) },
                { 0x5B, typeof(UnresolvedToken) },
                { 0x5C, typeof(UnresolvedToken) },
                { 0x5D, typeof(UnresolvedToken) },
                { 0x5E, typeof(UnresolvedToken) },
                { 0x5F, typeof(BadToken) },
                { 0x60, typeof(UnresolvedToken) },
                { 0x61, typeof(UnresolvedToken) },
                { 0x62, typeof(UnresolvedToken) },
                { 0x63, typeof(UnresolvedToken) },
                { 0x64, typeof(UnresolvedToken) },
                { 0x65, typeof(UnresolvedToken) },
                { 0x66, typeof(VirtualFunctionToken) },
                { 0x67, typeof(UnresolvedToken) },
                { 0x68, typeof(UnresolvedToken) },
                { 0x69, typeof(TrueToken) },
                { 0x6A, typeof(UnresolvedToken) },
                { 0x6B, typeof(UnresolvedToken) },
                { 0x6C, typeof(LocalVariableToken) },
                { 0x6D, typeof(LetToken) },
                // Prob LetDelegate
                { 0x6E, typeof(UnresolvedToken) },
                { 0x6F, typeof(EndFunctionParmsToken) },
            };

            return tokenMap;
        }

        protected override void SetupTokenFactory(UnrealPackage linker)
        {
            var tokenMap = BuildTokenMap(linker);
            SetupTokenFactory<TokenFactory>(
                tokenMap,
                TokenFactory.FromPackage(linker.NTLPackage),
                0x70,
                0x80);
        }
    }

    public class RLIdkToken : Token
    {
    }
}
