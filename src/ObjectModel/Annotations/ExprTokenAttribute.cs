using System;
using UELib.Tokens;

namespace UELib.ObjectModel.Annotations
{
    public class ExprTokenAttribute : Attribute
    {
        public readonly ExprToken ExprToken;

        public ExprTokenAttribute(ExprToken exprToken)
        {
            ExprToken = exprToken;
        }
    }
}
