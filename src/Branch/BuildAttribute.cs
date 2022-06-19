using System;

namespace UELib.Branch
{
    /// <summary>
    /// Not yet usable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BuildAttribute : Attribute
    {
        public readonly UnrealPackage.GameBuild.BuildName Build;

        public BuildAttribute(UnrealPackage.GameBuild.BuildName build)
        {
            Build = build;
        }
    }
}
