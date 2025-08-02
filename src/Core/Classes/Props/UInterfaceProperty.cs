using System;
using System.Diagnostics;
using UELib.Branch;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UInterfaceProperty/Core.InterfaceProperty
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE3, BuildGeneration.UE4)]
    public class UInterfaceProperty : UProperty
    {
        #region Serialized Members

        /// <summary>
        ///     The interface class that this property references.
        /// </summary>
        [StreamRecord]
        public UClass InterfaceClass { get; set; }

        #endregion

        public UInterfaceProperty()
        {
            Type = PropertyType.InterfaceProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            InterfaceClass = stream.ReadObject<UClass>();
            stream.Record(nameof(InterfaceClass), InterfaceClass);

            Debug.Assert(InterfaceClass != null);
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 32)
            {
                var vd0 = stream.ReadName();
                stream.Record(nameof(vd0), vd0);
            }
#endif
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            Debug.Assert(InterfaceClass != null);
            stream.Write(InterfaceClass);

#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                stream.LicenseeVersion >= 32)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
        }

        public override string GetFriendlyType()
        {
            return InterfaceClass != null ? InterfaceClass.GetFriendlyType() : "@NULL";
        }
    }
}
