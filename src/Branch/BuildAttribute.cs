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

    /// <summary>
    /// Not yet usable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
    public class BuildGenerationAttribute : Attribute
    {
        public readonly BuildGeneration Generation;
        public readonly int EngineVersion = -1;

        public BuildGenerationAttribute(BuildGeneration generation)
        {
            Generation = generation;
        }

        public BuildGenerationAttribute(BuildGeneration generation, int engineVersion)
        {
            Generation = generation;
            EngineVersion = engineVersion;
        }
    }
}