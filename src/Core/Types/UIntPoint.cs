using System.Numerics;
using System.Runtime.InteropServices;

namespace UELib.Core;

/// <summary>
///     Implements FIntPoint/UObject.IntPoint
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public record struct UIntPoint : IUnrealSerializableClass, IUnrealAtomicStruct
{
    public float X, Y;

    public UIntPoint(float x, float y)
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

    public static unsafe explicit operator Vector2(UIntPoint u)
    {
        return *(Vector2*)&u;
    }

    public static unsafe explicit operator UIntPoint(Vector2 m)
    {
        return *(UIntPoint*)&m;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }
}