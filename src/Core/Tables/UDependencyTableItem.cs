using System.Collections.Generic;
using UELib.Core;

namespace UELib
{
    public sealed class UDependencyTableItem : UTableItem, IUnrealDeserializableClass
    {
        #region Serialized Members
        public List<int> Dependencies;
        #endregion

        public void Deserialize( IUnrealStream stream )
        {
            Dependencies.Deserialize( stream );
        }
    }
}