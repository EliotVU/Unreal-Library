#if GIGANTIC
using UELib.ObjectModel.Annotations;
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

        [StreamRecord]
        public UClass MetaClass { get; set; }

        #endregion

        public UJsonRefProperty()
        {
            Type = PropertyType.JsonRefProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            MetaClass = stream.ReadObject<UClass>();
            stream.Record(nameof(MetaClass), MetaClass);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(MetaClass);
        }

        public override string GetFriendlyType()
        {
            return $"JsonRef<{MetaClass.GetFriendlyType()}>";
        }
    }
}
#endif
