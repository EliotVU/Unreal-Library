using UELib.Core;

namespace UELib
{
    public sealed class UDependencyTableItem : UTableItem, IUnrealDeserializableClass
    {
        #region Serialized Members

        public UArray<UObject> Dependencies;

        #endregion

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadArray(out Dependencies);
        }
    }
}