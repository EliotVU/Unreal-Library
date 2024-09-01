#if GIGANTIC
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// JSON Reference Property
    ///
    /// <list>
    /// PropertyType Format:
    /// <code>JsonRef&lt;MetaClass&gt; JsonProperty</code>
    /// PropertyValue Format:
    /// <code>JsonRef&lt;MetaClass&gt;'ObjectReference'</code>
    /// </list>
    /// </summary>
    [UnrealRegisterClass]
    public class UJsonRefProperty : UProperty
    {
        #region Serialized Members

        public UClass MetaClass;

        #endregion

        public UJsonRefProperty()
        {
            Type = PropertyType.JsonRefProperty;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            MetaClass = _Buffer.ReadObject<UClass>();
            Record(nameof(MetaClass), MetaClass);
        }

        public override string GetFriendlyType()
        {
            return $"JsonRef<{MetaClass.GetFriendlyType()}>";
        }
    }
}
#endif
