using System;
using System.Diagnostics;
using UELib.Branch.UE2.Eon;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UArrayProperty/Core.ArrayProperty
    /// </summary>
    [UnrealRegisterClass]
    public partial class UArrayProperty : UProperty
    {
        #region Serialized Members

        /// <summary>
        ///     The property that defines the type of elements in the array.
        /// </summary>
        [StreamRecord]
        public UProperty InnerProperty { get; set; }

        #endregion

        public UArrayProperty()
        {
            Type = PropertyType.ArrayProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent)
            {
                InnerProperty = EonEngineBranch.DeserializeFProperty<UProperty>(stream)!;

                return;
            }
#endif
            InnerProperty = stream.ReadObject<UProperty>();
            stream.Record(nameof(InnerProperty), InnerProperty);

            Debug.Assert(InnerProperty != null);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
#if ADVENT
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Advent)
            {
                throw new NotSupportedException("This package version is not supported!");
            }
#endif
            Debug.Assert(InnerProperty != null);
            stream.Write(InnerProperty);
        }

        public override string GetFriendlyType()
        {
            return $"array<{GetFriendlyInnerType()}>";
        }

        public override string GetFriendlyInnerType()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (InnerProperty == null)
            {
                return "@NULL";
            }

            return InnerProperty.Type == PropertyType.ClassProperty ||
                   InnerProperty.Type == PropertyType.DelegateProperty
                ? $" {InnerProperty.FormatFlags()}{InnerProperty.GetFriendlyType()} "
                : InnerProperty.FormatFlags() + InnerProperty.GetFriendlyType();
        }
    }
}
