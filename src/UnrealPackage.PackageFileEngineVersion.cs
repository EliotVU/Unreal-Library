namespace UELib;

public sealed partial class UnrealPackage
{
    public struct PackageFileEngineVersion : IUnrealSerializableClass
    {
        public uint Major, Minor, Patch;
        public uint Changelist;
        public string Branch;

        public void Deserialize(IUnrealStream stream)
        {
            Major = stream.ReadUInt16();
            Minor = stream.ReadUInt16();
            Patch = stream.ReadUInt16();
            Changelist = stream.ReadUInt32();
            Branch = stream.ReadString();
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(Major);
            stream.Write(Minor);
            stream.Write(Patch);
            stream.Write(Changelist);
            stream.Write(Branch);
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }
    }
}