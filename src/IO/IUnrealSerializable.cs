namespace UELib.IO;

public interface IUnrealSerializable<in T> where T : IUnrealStream
{
    void Deserialize(T stream);
    void Serialize(T stream);
}