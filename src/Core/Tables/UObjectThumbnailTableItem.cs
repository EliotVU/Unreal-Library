using UELib.Branch;
using UELib.Core;

namespace UELib;

public class UObjectThumbnailTableItem : UTableItem, IUnrealSerializableClass
{
    public string ObjectClassName;
    public string ObjectPath;

    public int ThumbnailOffset;

    public UObjectThumbnail Thumbnail { get; set; }

    public void Deserialize(IUnrealStream stream)
    {
        if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedObjectClassNameToThumbnail)
        {
            stream.Read(out ObjectClassName);
        }

        stream.Read(out ObjectPath);
        stream.Read(out ThumbnailOffset);
    }

    public void Serialize(IUnrealStream stream)
    {
        if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedObjectClassNameToThumbnail)
        {
            stream.Write(ObjectClassName);
        }

        stream.Write(ObjectPath);
        stream.Write(ThumbnailOffset);
    }
}
