using System;
using System.Diagnostics;

namespace UELib.Branch
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BuildEngineBranchAttribute : Attribute
    {
        public readonly Type EngineBranchType;
        
        public BuildEngineBranchAttribute(Type engineBranchType)
        {
            EngineBranchType = engineBranchType;
            Debug.Assert(engineBranchType != null);
        }
    }
}
