#if GIGANTIC
using System;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    /// JSON Reference Property
    /// </summary>
    [UnrealRegisterClass]
    public class UJsonRefProperty : UProperty
    { 
        public UJsonRefProperty()
        {
            Type = PropertyType.JsonRefProperty;
        }

        public override string GetFriendlyType()
        {
            return "JsonRef";
        }
    }
}
#endif
