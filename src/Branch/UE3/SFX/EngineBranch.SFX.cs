using UELib.Branch.UE3.BL2.Tokens;
using UELib.Branch.UE3.SFX.Tokens;
using UELib.Core;
using UELib.Core.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.SFX
{
    public class EngineBranchSFX : DefaultEngineBranch
    {
        public EngineBranchSFX(BuildGeneration generation) : base(generation)
        {
        }

        public override void Setup(UnrealPackage linker)
        {
            base.Setup(linker);

            // FIXME: Temporary workaround
            if (linker.LicenseeVersion == 1008)
            {
                linker.Summary.LicenseeVersion = 112;
            }
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<PackageSerializerSFX>();
        }

        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = base.BuildTokenMap(linker);

            // Xenon
            if (linker.Build == UnrealPackage.GameBuild.BuildName.ME1 &&
                linker.Version == 391 && linker.LicenseeVersion == 92)
            {
                tokenMap[0x4A] = typeof(StringRefConstToken);
                tokenMap[0x4B] = typeof(DynamicArrayAddToken);
            }

            tokenMap[0x4F] = typeof(StringRefConstToken);

            if (linker.Build == UnrealPackage.GameBuild.BuildName.ME1)
            {
                return tokenMap;
            }

            tokenMap[0x5B] = typeof(LocalVariableToken<float>);
            tokenMap[0x5C] = typeof(LocalVariableToken<int>);
            tokenMap[0x5D] = typeof(LocalVariableToken<byte>);
            tokenMap[0x5E] = typeof(LocalVariableToken<UObject>);
            tokenMap[0x5F] = typeof(InstanceVariableToken);
            tokenMap[0x60] = typeof(InstanceVariableToken);
            tokenMap[0x61] = typeof(InstanceVariableToken);
            tokenMap[0x62] = typeof(InstanceVariableToken);
            tokenMap[0x63] = typeof(JumpIfNotVariableToken); // IfLocal
            tokenMap[0x64] = typeof(JumpIfNotVariableToken); // IfInstance
            tokenMap[0x65] = typeof(NamedFunctionToken);
            
            tokenMap[0x66] = typeof(BadToken);
            tokenMap[0x67] = typeof(BadToken);
            tokenMap[0x68] = typeof(BadToken);
            tokenMap[0x69] = typeof(BadToken);
            tokenMap[0x6A] = typeof(BadToken);
            tokenMap[0x6B] = typeof(BadToken);
            tokenMap[0x6C] = typeof(BadToken);
            tokenMap[0x6D] = typeof(BadToken);
            tokenMap[0x6E] = typeof(BadToken);
            tokenMap[0x6F] = typeof(BadToken);

            return tokenMap;
        }

        protected override void SetupTokenFactory(UnrealPackage linker)
        {
            if (linker.Build == UnrealPackage.GameBuild.BuildName.ME1)
            {
                base.SetupTokenFactory(linker);

                return;
            }

            var tokenMap = BuildTokenMap(linker);

            SetupTokenFactory<TokenFactory>(
                tokenMap,
                TokenFactory.FromPackage(linker.NTLPackage),
                0x70,
                0x80);
        }
    }
}
