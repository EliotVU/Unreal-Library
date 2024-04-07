using System;
using System.ComponentModel;
using System.Numerics;
using UELib.Annotations;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements FPoly
    /// </summary>
    [Output("Polygon")]
    public class Poly : IUnrealSerializableClass, IAcceptable
    {
        [CanBeNull] public UObject Actor;

        public int BrushPoly = -1;

        [DefaultValue(null)] [Output("Item", OutputSlot.Parameter)]
        public UName ItemName;

        [DefaultValue(null)] [Output("Texture")]
        public UObject Material;

        [DefaultValue(3584u)] [Output("Flags", OutputSlot.Parameter)]
        public uint PolyFlags = 3584u;

        [DefaultValue(-1)] [Output(OutputSlot.Parameter)]
        public int Link = -1;

        [DefaultValue(32.0f)] [Output(OutputSlot.Parameter)]
        public float ShadowMapScale = 32.0f;

        //[Output(OutputSlot.Parameter)] 
        public LightingChannelContainer LightingChannels;

        [Output("Origin", OutputFlags.ShorthandProperty)]
        public UVector Base;

        [Output(OutputFlags.ShorthandProperty)]
        public UVector Normal;

        [DefaultValue(0)] [Output("U")] public short PanU;
        [DefaultValue(0)] [Output("V")] public short PanV;

        [Output(OutputFlags.ShorthandProperty)]
        public UVector TextureU;

        [Output(OutputFlags.ShorthandProperty)]
        public UVector TextureV;

        [Output(OutputFlags.ShorthandProperty)]
        public UArray<UVector> Vertex;

        [DefaultValue(32.0f)] public float LightMapScale = 32.0f; // Not exported for some reason

        public LightmassPrimitiveSettings LightmassSettings;

        [DefaultValue(null)] public UProcBuildingRuleset Ruleset;
        [DefaultValue(null)] public UName RulesetVariation;

        public void Accept(IVisitor visitor)
        {
            visitor.Visit(this);
        }

        public TResult Accept<TResult>(IVisitor<TResult> visitor)
        {
            return visitor.Visit(this);
        }

        public void Deserialize(IUnrealStream stream)
        {
            // Always 16
            int verticesCount = stream.Version < (uint)PackageObjectLegacyVersion.FixedVerticesToArrayFromPoly
                ? stream.ReadIndex()
                : -1;

            stream.ReadStruct(out Base);
            stream.ReadStruct(out Normal);
            stream.ReadStruct(out TextureU);
            stream.ReadStruct(out TextureV);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.FixedVerticesToArrayFromPoly)
            {
                stream.Read(out Vertex);
            }
            else
            {
                stream.ReadArray(out Vertex, verticesCount);
            }

            PolyFlags = stream.ReadUInt32();
            if (stream.Version < 250)
            {
                PolyFlags |= 0xe00;
            }

            Actor = stream.ReadObject<UObject>();
            if (stream.Version < (uint)PackageObjectLegacyVersion.TextureDeprecatedFromPoly)
            {
                Material = stream.ReadObject<UObject>();
            }

            ItemName = stream.ReadNameReference();
            // The fact this is serialized after ItemName indicates we may have had both (A texture and material) at one point.
            if (stream.Version >= (uint)PackageObjectLegacyVersion.MaterialAddedToPoly)
            {
                Material = stream.ReadObject<UObject>();
            }

            Link = stream.ReadIndex();
            BrushPoly = stream.ReadIndex();

            if (stream.Version < (uint)PackageObjectLegacyVersion.PanUVRemovedFromPoly)
            {
                PanU = stream.ReadInt16();
                PanV = stream.ReadInt16();
                
                // Fix UV
                var newBase = (Vector3)Base;
                newBase -= (Vector3)TextureU / ((Vector3)TextureU).LengthSquared() * PanU;
                newBase -= (Vector3)TextureV / ((Vector3)TextureV).LengthSquared() * PanV;
                Base = (UVector)newBase;
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightMapScaleAddedToPoly &&
                stream.Version < (uint)PackageObjectLegacyVersion.LightMapScaleRemovedFromPoly)
            {
                LightMapScale = stream.ReadFloat();
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.ShadowMapScaleAddedToPoly)
            {
                ShadowMapScale = stream.ReadFloat();
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightingChannelsAddedToPoly)
            {
                stream.ReadStructMarshal(out LightingChannels);
            }
            else
            {
                LightingChannels.Initialized = true;
                LightingChannels.BSP = true;
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.LightmassAdded)
            {
                stream.ReadStruct(out LightmassSettings);
            }

            if (stream.Version >= (uint)PackageObjectLegacyVersion.UProcBuildingReferenceAddedToPoly)
            {
                if (stream.Version >= (uint)PackageObjectLegacyVersion.PolyRulesetVariationTypeChangedToName)
                {
                    stream.Read(out RulesetVariation);
                }
                else
                {
                    stream.Read(out Ruleset);
                }
            }
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
    }
}
