namespace UELib.Core;

public partial class UClass
{
    /// <summary>
    ///     Implements FDependency.
    /// 
    ///     A legacy dependency struct that was used for incremental compilation (UnrealEd).
    /// </summary>
    public record struct Dependency : IUnrealSerializableClass
    {
        public UClass Class;
        public bool IsDeep;
        public uint ScriptTextCRC;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out Class);
#if DNF
            // No specified version
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                goto skipDeep;
            }
#endif
            stream.Read(out IsDeep);
        skipDeep:
            stream.Read(out ScriptTextCRC);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(Class);
#if DNF
            // No specified version
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF)
            {
                goto skipDeep;
            }
#endif
            stream.Write(IsDeep);
        skipDeep:
            stream.Write(ScriptTextCRC);
        }
    }
}
