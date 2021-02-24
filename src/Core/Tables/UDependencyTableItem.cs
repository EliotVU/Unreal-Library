using System.Collections.Generic;
using UELib.JsonDecompiler.Core;

namespace UELib.JsonDecompiler
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