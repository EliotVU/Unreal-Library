using System;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UClassProperty/Core.ClassProperty
    /// </summary>
    [UnrealRegisterClass]
    public class UClassProperty : UObjectProperty
    {
        #region Serialized Members

        // MetaClass
        public UClass? MetaClass { get; set; }

        #endregion

        /// <summary>
        /// Creates a new instance of the UELib.Core.UClassProperty class.
        /// </summary>
        public UClassProperty()
        {
            Type = PropertyType.ClassProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            MetaClass = _Buffer.ReadObject<UClass>();
            Record(nameof(MetaClass), MetaClass);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            if (MetaClass != null)
            {
                return MetaClass.Name == UnrealName.Object
                    ? Object.GetFriendlyType()
                    : ($"Class<{GetFriendlyInnerType()}>");
            }

            return "Class";
        }

        public override string GetFriendlyInnerType()
        {
            return MetaClass != null ? MetaClass.GetFriendlyType() : "@NULL";
        }
    }
}
