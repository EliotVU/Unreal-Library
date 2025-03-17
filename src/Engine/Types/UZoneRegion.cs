using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine;

/// <summary>
///     Implements FPointRegion/AActor.PointRegion
/// </summary>
public record struct UPointRegion : IUnrealSerializableClass
{
    // We don't want to cast to AZoneInfo because, not all actor derivatives are known.
    [Output("Zone")]
    public UObject ZoneActor;

    /// <summary>
    ///    Index to the leaf in the zone's BSP.
    /// </summary>
    [Output("iLeaf")]
    public int LeafIndex;

    /// <summary>
    ///   Index to the zone.
    /// </summary>
    [Output("ZoneNumber")]
    public byte ZoneIndex;

    public void Deserialize(IUnrealStream stream)
    {
        stream.Read(out ZoneActor);
        stream.Read(out LeafIndex);
        stream.Read(out ZoneIndex);
    }

    public void Serialize(IUnrealStream stream)
    {
        stream.Write(ZoneActor);
        stream.Write(LeafIndex);
        stream.Write(ZoneIndex);
    }
}