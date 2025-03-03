using System;

using UELib.Core;

namespace UELib.Branch.UE3.HUXLEY
{
    public class EngineBranchHuxley : DefaultEngineBranch
    {
        [Flags]
        public enum PackageFlags : uint
        {
            UseCrypt = 0x8000000
        }

        public EngineBranchHuxley(BuildGeneration generation) : base(generation)
        {
        }

        public override void PostDeserializeSummary(UnrealPackage linker, IUnrealStream stream, ref UnrealPackage.PackageFileSummary summary)
        {
            base.PostDeserializeSummary(linker, stream, ref summary);

            if (summary.PackageFlags.HasFlags((uint)PackageFlags.UseCrypt))
            {
                stream.Decoder = linker.Summary.LicenseeVersion >= 23
                    ? new CryptoDecoderHuxley(linker.PackageName)
                    : new CryptoDecoderHuxley(linker.Summary.Guid.A);
            }
        }
    }
}
