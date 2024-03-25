using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FRotator/UObject.Rotator
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct URotator : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public int Pitch;
        public int Yaw;
        public int Roll;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out Pitch);
            stream.Read(out Yaw);
            stream.Read(out Roll);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(Pitch);
            stream.Write(Yaw);
            stream.Write(Roll);
        }
    }
}