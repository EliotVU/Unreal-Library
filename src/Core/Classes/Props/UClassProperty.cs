using System;
using UELib.ObjectModel.Annotations;
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

        /// <summary>
        ///     The metaclass e.g. "Class<Object>" where Object is the defined metaclass.
        /// </summary>
        [StreamRecord]
        public UClass? MetaClass { get; set; }

        #endregion

        public UClassProperty()
        {
            Type = PropertyType.ClassProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            MetaClass = stream.ReadObject<UClass?>();
            stream.Record(nameof(MetaClass), MetaClass);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(MetaClass);
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
