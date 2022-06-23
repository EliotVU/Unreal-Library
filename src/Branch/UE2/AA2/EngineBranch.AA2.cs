using static UELib.UnrealPackage;
using System.Diagnostics;
using System.IO;

namespace UELib.Branch.UE2.AA2
{
    [Build(GameBuild.BuildName.AA2)]
    public class EngineBranchAA2 : DefaultEngineBranch
    {
        /// Decoder initialization is handled in <see cref="UnrealPackage.Deserialize"/>
        public EngineBranchAA2(UnrealPackage package) : base(package)
        {
        }

        protected override void SetupSerializer(UnrealPackage package)
        {
            if (package.LicenseeVersion >= 33)
            {
                Serializer = new PackageSerializerAA2();
                return;
            }
            
            base.SetupSerializer(package);
        }

        public override void PostDeserializeSummary(IUnrealStream stream, ref PackageFileSummary summary)
        {
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