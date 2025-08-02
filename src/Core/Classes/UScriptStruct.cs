using UELib.Branch;
using UELib.Flags;

namespace UELib.Core
{
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
    public class UScriptStruct : UStruct
    {
        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedStructFlagsToScriptStruct)
            {
                StructFlags = stream.ReadFlags32<StructFlag>();
                stream.Record(nameof(StructFlags), StructFlags);
            }

            DefaultProperties = DeserializeScriptProperties(stream, this);
            Properties = DefaultProperties;
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedStructFlagsToScriptStruct)
            {
                stream.Write((uint)StructFlags);
            }

            SerializeScriptProperties(stream, this, DefaultProperties);
            Properties = DefaultProperties;
        }
    }
}
