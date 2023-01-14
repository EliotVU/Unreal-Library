using System;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// Class Property
    ///
    /// var class'Actor' ActorClass;
    /// </summary>
    [UnrealRegisterClass]
    public class UClassProperty : UObjectProperty
    {
        #region Serialized Members

        // MetaClass
        public UClass MetaClass;

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
                return (string.Compare(MetaClass.Name, "Object", StringComparison.OrdinalIgnoreCase) == 0)
                    ? Object.GetFriendlyType()
                    : ($"class<{GetFriendlyInnerType()}>");
            }

            return "class";
        }

        public override string GetFriendlyInnerType()
        {
            return MetaClass != null ? MetaClass.GetFriendlyType() : "@NULL";
        }
    }
}
