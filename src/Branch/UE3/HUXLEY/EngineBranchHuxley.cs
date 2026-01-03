using System;
using UELib.IO;

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

        public override void PostDeserializeSummary(UnrealPackage package, IUnrealStream stream,
            ref UnrealPackage.PackageFileSummary summary)
        {
            base.PostDeserializeSummary(package, stream, ref summary);

            if (summary.PackageFlags.HasFlags((uint)PackageFlags.UseCrypt))
            {
                var decoder = package.Summary.LicenseeVersion >= 23
                    ? new CryptoDecoderHuxley(package.PackageName)
                    : new CryptoDecoderHuxley(package.Summary.Guid.A);

                package.Archive.Decoder = decoder;
                package.Stream.SwapReaderBaseStream(new EncodedStream(stream.UR._BaseReader.BaseStream, decoder));
            }
        }
    }
}
