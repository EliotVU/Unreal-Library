using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements ESheerAxis/UObject.ESheerAxis
    /// </summary>
    public enum SheerAxis : byte
    {
        None = 0,
        XY = 1,
        XZ = 2,
        YX = 3,
        YZ = 4,
        ZX = 5,
        ZY = 6
    }

    /// <summary>
    ///     Implements FScale/UObject.Scale
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UScale : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public UVector Scale;
        public float SheerRate;
        public SheerAxis SheerAxis;

        public UScale(ref UVector scale, float sheerRate, SheerAxis sheerAxis)
        {
            Scale = scale;
            SheerRate = sheerRate;
            SheerAxis = sheerAxis;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadStruct(out Scale);
            stream.Read(out SheerRate);
            var sheerAxis = (byte)SheerAxis;
            stream.Read(out sheerAxis);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteStruct(ref Scale);
            stream.Write(SheerRate);
            stream.Write((byte)SheerAxis);
        }
    }
}