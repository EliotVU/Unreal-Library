using System.ComponentModel;
using System.Runtime.InteropServices;
using UELib.Branch;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements FLightmassPrimitiveSettings
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LightmassPrimitiveSettings : IUnrealSerializableClass
    {
        [MarshalAs(UnmanagedType.I1)] public bool UseTwoSidedLighting;
        [MarshalAs(UnmanagedType.I1)] public bool ShadowIndirectOnly;
        [MarshalAs(UnmanagedType.I1)] public bool UseEmissiveForStaticLighting;

        public float EmissiveLightFalloffExponent;
        public float EmissiveLightExplicitInfluenceRadius;
        public float EmissiveBoost;
        public float DiffuseBoost;
        public float SpecularBoost;

        [DefaultValue(1.0f)] public float FullyOccludedSamplesFraction;

        public void Deserialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassShadowIndirectOnlyOptionAdded)
            {
                stream.Read(out UseTwoSidedLighting);
                stream.Read(out ShadowIndirectOnly);
                stream.Read(out FullyOccludedSamplesFraction);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassAdded)
            {
                stream.Read(out UseEmissiveForStaticLighting);
                stream.Read(out EmissiveLightFalloffExponent);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassExplicitEmissiveLightRadiusAdded)
            {
                stream.Read(out EmissiveLightExplicitInfluenceRadius);
            }

            stream.Read(out EmissiveBoost);
            stream.Read(out DiffuseBoost);
            stream.Read(out SpecularBoost);
        }

        public void Serialize(IUnrealStream stream)
        {
            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassShadowIndirectOnlyOptionAdded)
            {
                stream.Write(UseTwoSidedLighting);
                stream.Write(ShadowIndirectOnly);
                stream.Write(FullyOccludedSamplesFraction);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassAdded)
            {
                stream.Write(UseEmissiveForStaticLighting);
                stream.Write(EmissiveLightFalloffExponent);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassExplicitEmissiveLightRadiusAdded)
            {
                stream.Write(EmissiveLightExplicitInfluenceRadius);
            }

            stream.Write(EmissiveBoost);
            stream.Write(DiffuseBoost);
            stream.Write(SpecularBoost);
        }
    }
}