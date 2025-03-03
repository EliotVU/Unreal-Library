using UELib.Core;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements FConvexVolume
    ///
    ///     <remarks>https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Runtime/Engine/FConvexVolume</remarks>
    /// </summary>
    public struct UConvexVolume : IUnrealSerializableClass
    {
        public UArray<UPlane> Planes;
        public UArray<UPlane> PermutedPlanes;

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadArray(out Planes);
            stream.ReadArray(out PermutedPlanes);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteArray(Planes);
            stream.WriteArray(PermutedPlanes);
        }
    }
}
