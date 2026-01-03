using UELib.Branch.UE3.Willow.Tokens;
using UELib.Branch.UE3.SFX.Tokens;
using UELib.Core;
using UELib.Core.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch.UE3.SFX
{
    public class EngineBranchSFX : DefaultEngineBranch
    {
        public EngineBranchSFX(BuildGeneration generation) : base(BuildGeneration.SFX)
        {
        }

        public override void Setup(UnrealPackage package)
        {
            base.Setup(package);

            // FIXME: Temporary workaround
            if (package.LicenseeVersion == 1008)
            {
                package.Summary.LicenseeVersion = 112;
            }
        }

        protected override void SetupSerializer(UnrealPackage package)
        {
            SetupSerializer<PackageSerializerSFX>();
        }

        protected override TokenMap BuildTokenMap(UnrealPackage package)
        {
            var tokenMap = base.BuildTokenMap(package);

            // Xenon
            if (package.Build == UnrealPackage.GameBuild.BuildName.ME1 &&
                package.Version == 391 && package.LicenseeVersion == 92)
            {
                tokenMap[0x4A] = typeof(StringRefConstToken);
                tokenMap[0x4B] = typeof(DynamicArrayAddToken);
            }

            tokenMap[0x4F] = typeof(StringRefConstToken);

            if (package.Build == UnrealPackage.GameBuild.BuildName.ME1)
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

        protected override void SetupTokenFactory(UnrealPackage package)
        {
            if (package.Build == UnrealPackage.GameBuild.BuildName.ME1)
            {
                base.SetupTokenFactory(package);

                return;
            }

            var tokenMap = BuildTokenMap(package);

            SetupTokenFactory<TokenFactory>(
                tokenMap,
                TokenFactory.FromPackage(package.NTLPackage),
                0x70,
                0x80);
        }
    }
}
