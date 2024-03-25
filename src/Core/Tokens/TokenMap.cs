using System;
using System.Collections.Generic;

namespace UELib.Core.Tokens
{
    public class TokenMap : Dictionary<byte, Type>
    {
        public TokenMap()
        {
        }

        public TokenMap(byte extendedNative) : base(extendedNative - 1)
        {
        }
    }
}
