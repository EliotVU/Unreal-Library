using static UELib.UnrealPackage;
using System.Diagnostics;
using System.IO;
using UELib.Core;
using UELib.Core.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE2.AA2
{
    public class EngineBranchAA2 : DefaultEngineBranch
    {
        public EngineBranchAA2(BuildGeneration generation) : base(BuildGeneration.AGP)
        {
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            if (linker.LicenseeVersion < 33)
            {
                base.SetupSerializer(linker);
                return;
            }

            SetupSerializer<PackageSerializerAA2>();
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            if (linker.Build == GameBuild.BuildName.AA2_2_6)
            {
                var tokenMap = new TokenMap
                {
                    { 0x00, typeof(LocalVariableToken) },
                    { 0x01, typeof(InstanceVariableToken) },
                    { 0x02, typeof(DefaultVariableToken) },
                    { 0x03, typeof(BadToken) },
                    { 0x04, typeof(JumpToken) },
                    { 0x05, typeof(ReturnToken) },
                    { 0x06, typeof(SwitchToken) },
                    { 0x07, typeof(StopToken) },
                    { 0x08, typeof(JumpIfNotToken) },
                    { 0x09, typeof(NothingToken) },
                    { 0x0A, typeof(LabelTableToken) },
                    { 0x0B, typeof(AssertToken) },
                    { 0x0C, typeof(CaseToken) },
                    { 0x0D, typeof(EatStringToken) },
                    { 0x0E, typeof(LetToken) },
                    { 0x0F, typeof(GotoLabelToken) },
                    { 0x10, typeof(DynamicArrayElementToken) },
                    { 0x11, typeof(NewToken) },
                    { 0x12, typeof(ClassContextToken) },
                    { 0x13, typeof(MetaClassCastToken) },
                    { 0x14, typeof(LetBoolToken) },
                    { 0x15, typeof(EndFunctionParmsToken) },
                    { 0x16, typeof(SkipToken) },
                    { 0x17, typeof(BadToken) },
                    { 0x18, typeof(ContextToken) },
                    { 0x19, typeof(SelfToken) },
                    { 0x1A, typeof(FinalFunctionToken) },
                    { 0x1B, typeof(ArrayElementToken) },
                    { 0x1C, typeof(IntConstToken) },
                    { 0x1D, typeof(FloatConstToken) },
                    { 0x1E, typeof(StringConstToken) },
                    { 0x1F, typeof(VirtualFunctionToken) },
                    { 0x20, typeof(IntOneToken) },
                    { 0x21, typeof(VectorConstToken) },
                    { 0x22, typeof(NameConstToken) },
                    { 0x23, typeof(IntZeroToken) },
                    { 0x24, typeof(ObjectConstToken) },
                    { 0x25, typeof(ByteConstToken) },
                    { 0x26, typeof(RotationConstToken) },
                    { 0x27, typeof(FalseToken) },
                    { 0x28, typeof(TrueToken) },
                    { 0x29, typeof(NoObjectToken) },
                    { 0x2A, typeof(NativeParameterToken) },
                    { 0x2B, typeof(BadToken) },
                    { 0x2C, typeof(BoolVariableToken) },
                    { 0x2D, typeof(IteratorToken) },
                    { 0x2E, typeof(IntConstByteToken) },
                    { 0x2F, typeof(DynamicCastToken) },
                    { 0x30, typeof(BadToken) },
                    { 0x31, typeof(StructCmpNeToken) },
                    { 0x32, typeof(UnicodeStringConstToken) },
                    { 0x33, typeof(IteratorNextToken) },
                    { 0x34, typeof(StructCmpEqToken) },
                    { 0x35, typeof(IteratorPopToken) },
                    { 0x36, typeof(GlobalFunctionToken) },
                    { 0x37, typeof(StructMemberToken) },
                    { 0x38, typeof(PrimitiveCastToken) },
                    { 0x39, typeof(DynamicArrayLengthToken) },
                    { 0x3A, typeof(BadToken) },
                    { 0x3B, typeof(BadToken) },
                    { 0x3C, typeof(BadToken) },
                    { 0x3D, typeof(BadToken) },
                    { 0x3E, typeof(BadToken) },
                    { 0x3F, typeof(BadToken) },
                    { 0x40, typeof(BadToken) },
                    { 0x41, typeof(EndOfScriptToken) },
                    { 0x42, typeof(DynamicArrayRemoveToken) },
                    { 0x43, typeof(DynamicArrayInsertToken) },
                    { 0x44, typeof(DelegateFunctionToken) },
                    { 0x45, typeof(DebugInfoToken) },
                    { 0x46, typeof(LetDelegateToken) },
                    { 0x47, typeof(DelegatePropertyToken) },
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
                    { 0x59, typeof(BadToken) }
                };
                return tokenMap;
            }

            if (linker.LicenseeVersion >= 33)
            {
                var tokenMap = new TokenMap
                {
                    { 0x00, typeof(LocalVariableToken) },
                    { 0x01, typeof(InstanceVariableToken) },
                    { 0x02, typeof(DefaultVariableToken) },
                    { 0x03, typeof(BadToken) },
                    { 0x04, typeof(SwitchToken) },
                    { 0x05, typeof(ClassContextToken) },
                    { 0x06, typeof(JumpToken) },
                    { 0x07, typeof(GotoLabelToken) },
                    { 0x08, typeof(VirtualFunctionToken) },
                    { 0x09, typeof(IntConstToken) },
                    { 0x0A, typeof(JumpIfNotToken) },
                    { 0x0B, typeof(LabelTableToken) },
                    { 0x0C, typeof(FinalFunctionToken) },
                    { 0x0D, typeof(EatStringToken) },
                    { 0x0E, typeof(LetToken) },
                    { 0x0F, typeof(StopToken) },
                    { 0x10, typeof(NewToken) },
                    { 0x11, typeof(ContextToken) },
                    { 0x12, typeof(MetaClassCastToken) },
                    { 0x13, typeof(SkipToken) },
                    { 0x14, typeof(SelfToken) },
                    { 0x15, typeof(ReturnToken) },
                    { 0x16, typeof(EndFunctionParmsToken) },
                    { 0x17, typeof(BadToken) },
                    { 0x18, typeof(LetBoolToken) },
                    { 0x19, typeof(DynamicArrayElementToken) },
                    { 0x1A, typeof(AssertToken) },
                    { 0x1B, typeof(ByteConstToken) },
                    { 0x1C, typeof(NothingToken) },
                    { 0x1D, typeof(DelegatePropertyToken) },
                    { 0x1E, typeof(IntZeroToken) },
                    { 0x1F, typeof(LetDelegateToken) },
                    { 0x20, typeof(FalseToken) },
                    { 0x21, typeof(ArrayElementToken) },
                    { 0x22, typeof(EndOfScriptToken) },
                    { 0x23, typeof(TrueToken) },
                    { 0x24, typeof(BadToken) },
                    { 0x25, typeof(FloatConstToken) },
                    { 0x26, typeof(CaseToken) },
                    { 0x27, typeof(IntOneToken) },
                    { 0x28, typeof(StringConstToken) },
                    { 0x29, typeof(NoObjectToken) },
                    { 0x2A, typeof(NativeParameterToken) },
                    { 0x2B, typeof(BadToken) },
                    { 0x2C, typeof(DebugInfoToken) },
                    { 0x2D, typeof(StructCmpEqToken) },
                    // FIXME: Verify IteratorNext/IteratorPop?
                    { 0x2E, typeof(IteratorNextToken) },
                    { 0x2F, typeof(DynamicArrayRemoveToken) },
                    { 0x30, typeof(StructCmpNeToken) },
                    { 0x31, typeof(DynamicCastToken) },
                    { 0x32, typeof(IteratorToken) },
                    { 0x33, typeof(IntConstByteToken) },
                    { 0x34, typeof(BoolVariableToken) },
                    // FIXME: Verify IteratorNext/IteratorPop?
                    { 0x35, typeof(IteratorPopToken) },
                    { 0x36, typeof(UnicodeStringConstToken) },
                    { 0x37, typeof(StructMemberToken) },
                    { 0x38, typeof(BadToken) },
                    { 0x39, typeof(DelegateFunctionToken) },
                    { 0x3A, typeof(BadToken) },
                    { 0x3B, typeof(BadToken) },
                    { 0x3C, typeof(BadToken) },
                    { 0x3D, typeof(BadToken) },
                    { 0x3E, typeof(BadToken) },
                    { 0x3F, typeof(BadToken) },
                    { 0x40, typeof(ObjectConstToken) },
                    { 0x41, typeof(NameConstToken) },
                    { 0x42, typeof(DynamicArrayLengthToken) },
                    { 0x43, typeof(DynamicArrayInsertToken) },
                    { 0x44, typeof(PrimitiveCastToken) },
                    { 0x45, typeof(GlobalFunctionToken) },
                    { 0x46, typeof(VectorConstToken) },
                    { 0x47, typeof(RotationConstToken) },
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
                    { 0x59, typeof(BadToken) }
                };
                return tokenMap;
            }

            return base.BuildTokenMap(linker);
        }

        public override void PostDeserializeSummary(UnrealPackage linker,
            IUnrealStream stream,
            ref PackageFileSummary summary)
        {
            base.PostDeserializeSummary(linker, stream, ref summary);

            // Note: Never true, AA2 is not a detected build for packages with LicenseeVersion 27 or less
            // But we'll preserve this nonetheless
            if (stream.LicenseeVersion < 19) return;

            bool isEncrypted = stream.ReadInt32() > 0;
            if (isEncrypted)
            {
                // TODO: Use a stream wrapper instead; but this is blocked by an overly intertwined use of PackageStream.
                if (stream.LicenseeVersion >= 33)
                {
                    var decoder = new CryptoDecoderAA2();
                    stream.Decoder = decoder;
                }
                else
                {
                    var decoder = new CryptoDecoderWithKeyAA2();
                    stream.Decoder = decoder;

                    long nonePosition = summary.NameOffset;
                    stream.Seek(nonePosition, SeekOrigin.Begin);
                    byte scrambledNoneLength = stream.ReadByte();
                    decoder.Key = scrambledNoneLength;
                    stream.Seek(nonePosition, SeekOrigin.Begin);
                    byte unscrambledNoneLength = stream.ReadByte();
                    Debug.Assert((unscrambledNoneLength & 0x3F) == 5);
                }
            }

            // Always one
            //int unkCount = stream.ReadInt32();
            //for (var i = 0; i < unkCount; i++)
            //{
            //    // All zero
            //    stream.Skip(24);
            //    // Always identical to the package's GUID
            //    var guid = stream.ReadGuid();
            //}

            //// Always one
            //int unk2Count = stream.ReadInt32();
            //for (var i = 0; i < unk2Count; i++)
            //{
            //    // All zero
            //    stream.Skip(12);
            //}
        }
    }
}
