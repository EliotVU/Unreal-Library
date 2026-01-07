using System.Runtime.InteropServices;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine;

/// <summary>
///     Implements UStaticMesh/Engine.StaticMesh
///
///     Stub, not actually supported by any means.
/// </summary>
[UnrealRegisterClass]
public class UStaticMesh : UPrimitive
{
    #region Script Members

    // These were converted to ScriptProperties starting with package version 114 (except for UseSimpleKarmaCollision)
    [UnrealProperty] public bool UseSimpleLineCollision { get; set; }
    [UnrealProperty] public bool UseSimpleBoxCollision { get; set; } = true;
    [UnrealProperty] public bool UseSimpleKarmaCollision { get; set; } = true;
    [UnrealProperty] public bool UseVertexColor { get; set; }

    [UnrealProperty] public UArray<StaticMeshMaterial>? Materials { get; set; }

    #endregion

    public struct StaticMeshMaterial : IUnrealSerializableClass
    {
        [UnrealProperty] public UObject/*UMaterial*/? Material;
        [UnrealProperty] public bool EnableCollision;
        [UnrealProperty("OldEnableCollision")] private bool _OldEnableCollision;

        public void Deserialize(IUnrealStream stream)
        {
            Material = stream.ReadObject<UObject?>();
            EnableCollision = stream.ReadIndex() > 0;
            _OldEnableCollision = stream.ReadIndex() > 0;
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteObject(Material);
            stream.WriteIndex(EnableCollision ? 1 : 0);
            stream.WriteIndex(_OldEnableCollision ? 1 : 0);
        }
    }

    public override void Deserialize(IUnrealStream stream)
    {
        if (stream.Version >= 85)
        {
            // Inherit from UPrimitive::Deserialize
            base.Deserialize(stream);
        }
        else
        {
            DeserializeBase(stream, typeof(UObject));
        }

        // Not implemented.
    }

    public override void Serialize(IUnrealStream stream)
    {
        if (stream.Version >= 85)
        {
            // Inherit from UPrimitive::Serialize
            base.Serialize(stream);
        }
        else
        {
            SerializeBase(stream, typeof(UObject));
        }

        // Not implemented.
    }
}
