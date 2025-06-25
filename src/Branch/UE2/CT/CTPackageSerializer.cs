using UELib.Core;

namespace UELib.Branch.UE2.CT;

public class CTPackageSerializer : PackageSerializerBase
{
    public override void Serialize(IUnrealStream stream, UNameTableItem item) => item.Serialize(stream);

    public override void Deserialize(IUnrealStream stream, UNameTableItem item) => item.Deserialize(stream);

    public override void Serialize(IUnrealStream stream, UImportTableItem item) => item.Serialize(stream);

    public override void Deserialize(IUnrealStream stream, UImportTableItem item) => item.Deserialize(stream);

    public override void Serialize(IUnrealStream stream, UExportTableItem item)
    {
        stream.WriteIndex(item.ClassIndex);
        stream.WriteIndex(item.SuperIndex);
        stream.Write(item.OuterIndex);

        if (stream.Version >= 159)
        {
            stream.WriteIndex(0);
        }

        stream.Write(item.ObjectName);
        stream.Write((uint)item.ObjectFlags);

        if (stream.Version >= 152)
        {
            stream.Write(item.SerialSize);
            stream.Write(item.SerialOffset);
        }
        else
        {
            stream.WriteIndex(item.SerialSize);
            if (item.SerialSize > 0)
            {
                stream.WriteIndex(item.SerialOffset);
            }
        }
    }

    public override void Deserialize(IUnrealStream stream, UExportTableItem item)
    {
        item.ClassIndex = stream.ReadIndex();
        item.SuperIndex = stream.ReadIndex();
        item.OuterIndex = stream.ReadInt32();

        if (stream.Version >= 159)
        {
            UPackageIndex v0c = stream.ReadIndex();
        }

        item.ObjectName = stream.ReadName();
        item.ObjectFlags = stream.ReadUInt32();
        if (stream.Version >= 152)
        {
            item.SerialSize = stream.ReadInt32();
            item.SerialOffset = stream.ReadInt32();
        }
        else
        {
            item.SerialSize = stream.ReadIndex();
            if (item.SerialSize > 0)
            {
                item.SerialOffset = stream.ReadIndex();
            }
        }
    }
}
