using System;

namespace UELib.Branch
{
    /// <summary>
    /// Not yet usable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
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


    /// <summary>
    /// Not yet usable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
    public class BuildGenerationRangeAttribute : Attribute
    {
        public readonly BuildGeneration MinGeneration, MaxGeneration;
        public readonly int MinEngineVersion = -1, MaxEngineVersion = -1;

        public BuildGenerationRangeAttribute(BuildGeneration minGeneration, BuildGeneration maxGeneration)
        {
            MinGeneration = minGeneration;
            MaxGeneration = maxGeneration;
        }

        public BuildGenerationRangeAttribute(BuildGeneration minGeneration, int minEngineVersion, int maxEngineVersion, BuildGeneration maxGeneration)
        {
            MinGeneration = minGeneration;
            MaxGeneration = maxGeneration;

            MinEngineVersion = minEngineVersion;
            MaxEngineVersion = maxEngineVersion;
        }
    }
}