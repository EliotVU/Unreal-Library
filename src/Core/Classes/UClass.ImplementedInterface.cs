using UELib.Branch;

namespace UELib.Core;

public partial class UClass
{
    /// <summary>
    ///     Implements http://udn.epicgames.com/Three/UnrealScriptInterfaces.html
    /// </summary>
    public record struct ImplementedInterface : IUnrealSerializableClass
    {
        /// <summary>
        ///     The interface class that is implemented.
        /// </summary>
        public UClass InterfaceClass;

        /// <summary>
        ///     A struct property holding a pointer to the interface's vfTable, the property is prefixed with "VfTable_"
        ///
        ///     May be null if the implementation predates version <see cref="PackageObjectLegacyVersion.InterfaceClassesDeprecated"/>
        /// </summary>
        public UStructProperty? VfTableProperty;

        [BuildGeneration(BuildGeneration.UE4)]
        public bool IsImplementedByK2;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out InterfaceClass);
            stream.Read(out VfTableProperty);
#if UE4
            if (stream.UE4Version == 0)
            {
                return;
            }

            stream.Read(out IsImplementedByK2);
#endif
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(InterfaceClass);
            stream.Write(VfTableProperty);
#if UE4
            if (stream.UE4Version == 0)
            {
                return;
            }

            stream.Write(IsImplementedByK2);
#endif
        }
    }
}
