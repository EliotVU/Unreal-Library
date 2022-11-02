using System.Numerics;
using System.Runtime.InteropServices;

namespace UELib.Core
{
    /// <summary>
    ///     Implements FVector2D/UObject.Vector2D or FPoint (<=UE2)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct UVector2D : IUnrealSerializableClass, IUnrealAtomicStruct
    {
        public float X, Y;

        public UVector2D(float x, float y)
        {
            X = x;
            Y = y;
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out X);
            stream.Read(out Y);
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(X);
            stream.Write(Y);
        }

        public static unsafe explicit operator Vector2(UVector2D u)
        {
            return *(Vector2*)&u;
        }

        public static unsafe explicit operator UVector2D(Vector2 m)
        {
            return *(UVector2D*)&m;
        }

        public override int GetHashCode()
        {
            return ((Vector2)this).GetHashCode();
        }
    }
}