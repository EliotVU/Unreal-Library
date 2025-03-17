using System.Runtime.InteropServices;

namespace UELib.Core;

/// <summary>
///     Implements FTwoVectors/UObject.TwoVectors
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UTwoVectors : IUnrealSerializableClass, IUnrealAtomicStruct
{
    public UVector V1, V2;

    public UTwoVectors(UVector v1, UVector v2)
    {
        V1 = v1;
        V2 = v2;
    }

    public void Deserialize(IUnrealStream stream)
    {
        stream.ReadStruct(out V1);
        stream.ReadStruct(out V2);
    }

    public void Serialize(IUnrealStream stream)
    {
        stream.Write(ref V1);
        stream.Write(ref V2);
    }

    public override int GetHashCode()
    {
        return V1.GetHashCode() ^ V2.GetHashCode();
    }
}