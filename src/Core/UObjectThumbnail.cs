namespace UELib.Core;

public struct UObjectThumbnail : IUnrealSerializableClass
{
    public int ImageWidth;
    public int ImageHeight;

    /// <summary>
    ///     .PNG data
    /// </summary>
    public UArray<byte> ImageData;

    public void Deserialize(IUnrealStream stream)
    {
        stream.Read(out ImageWidth);
        stream.Read(out ImageHeight);
        stream.ReadArray(out ImageData);
    }

    public void Serialize(IUnrealStream stream)
    {
        stream.Write(ImageWidth);
        stream.Write(ImageHeight);
        stream.WriteArray(ImageData);
    }
}