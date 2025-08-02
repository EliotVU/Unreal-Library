using System;
using System.Diagnostics;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UObjectProperty/Core.ObjectProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UObjectProperty : UProperty
    {
        #region Serialized Members

        /// <summary>
        ///     The UObject that this property references.
        /// </summary>
        [StreamRecord]
        public UObject Object { get; set; }

        #endregion

        public UObjectProperty()
        {
            Type = PropertyType.ObjectProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Object = stream.ReadObject<UObject>();
            stream.Record(nameof(Object), Object);

            Debug.Assert(Object != null);
#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                // version >= 17 for UComponentProperty?
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

            Debug.Assert(Object != null);
            stream.Write(Object);

#if ROCKETLEAGUE
            if (stream.Build == UnrealPackage.GameBuild.BuildName.RocketLeague &&
                // version >= 17 for UComponentProperty?
                stream.LicenseeVersion >= 32)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
        }

        public override string GetFriendlyType()
        {
            return Object != null ? Object.GetFriendlyType() : "@NULL";
        }
    }
}
