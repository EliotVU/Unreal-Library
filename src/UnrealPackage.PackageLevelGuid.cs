using UELib.Core;

namespace UELib;

public sealed partial class UnrealPackage
{
    public struct PackageLevelGuid : IUnrealSerializableClass
    {
        public string LevelName;
        public UArray<UGuid> ObjectGuids;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out LevelName);
            stream.ReadArray(out ObjectGuids);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(LevelName);
            stream.WriteArray(ObjectGuids);
        }
    }
}