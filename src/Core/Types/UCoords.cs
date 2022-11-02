using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FCoords/UObject.Coords
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct UCoords : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public UVector Origin;
        public UVector XAxis;
        public UVector YAxis;
        public UVector ZAxis;

        public UCoords(ref UVector origin, ref UVector xAxis, ref UVector yAxis, ref UVector zAxis)
        {
            Origin = origin;
            XAxis = xAxis;
            YAxis = yAxis;
            ZAxis = zAxis;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.ReadStruct(out Origin);
            stream.ReadStruct(out XAxis);
            stream.ReadStruct(out YAxis);
            stream.ReadStruct(out ZAxis);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.WriteStruct(ref Origin);
            stream.WriteStruct(ref XAxis);
            stream.WriteStruct(ref YAxis);
            stream.WriteStruct(ref ZAxis);
        }
    }
}