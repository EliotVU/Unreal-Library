using UELib.Branch.UE2.Eon;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Dynamic Array Property
    /// </summary>
    [UnrealRegisterClass]
    public class UArrayProperty : UProperty
    {
        #region Serialized Members

        public UProperty InnerProperty;

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UArrayProperty class.
        /// </summary>
        public UArrayProperty()
        {
            Type = PropertyType.ArrayProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();
#if ADVENT
            if (_Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Advent)
            {
                InnerProperty = EonEngineBranch.SerializeFProperty<UProperty>(_Buffer)!;
                return;
            }
#endif
            InnerProperty = _Buffer.ReadObject<UProperty>();
            Record(nameof(InnerProperty), InnerProperty);
        }

        /// <inheritdoc/>
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

            return InnerProperty.IsClassType("ClassProperty") || InnerProperty.IsClassType("DelegateProperty")
                ? $" {InnerProperty.FormatFlags()}{InnerProperty.GetFriendlyType()} "
                : (InnerProperty.FormatFlags() + InnerProperty.GetFriendlyType());
        }
    }
}
