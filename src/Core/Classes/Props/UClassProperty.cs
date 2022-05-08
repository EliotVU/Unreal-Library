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
        public UClass ClassObject;

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

            int classIndex = _Buffer.ReadObjectIndex();
            ClassObject = (UClass)GetIndexObject(classIndex);
        }

        /// <inheritdoc/>
        public override string GetFriendlyType()
        {
            if (ClassObject != null)
            {
                return (string.Compare(ClassObject.Name, "Object", StringComparison.OrdinalIgnoreCase) == 0)
                    ? Object.GetFriendlyType()
                    : ($"class<{GetFriendlyInnerType()}>");
            }

            return "class";
        }

        public override string GetFriendlyInnerType()
        {
            return ClassObject != null ? ClassObject.GetFriendlyType() : "@NULL";
        }
    }
}