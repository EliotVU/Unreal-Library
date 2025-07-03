namespace UELib.Branch.UE2.Eon;

public class EonPackageSerializer : PackageSerializerBase
{
    public override void Serialize(IUnrealStream stream, UNameTableItem item) => item.Serialize(stream);

    public override void Deserialize(IUnrealStream stream, UNameTableItem item) => item.Deserialize(stream);

    public override void Serialize(IUnrealStream stream, UImportTableItem item)
    {
        // OuterIndex changed to 16 bits.
        if (stream.Version < 141)
        {
            item.Serialize(stream);

            return;
        }

        stream.Write(item.ClassPackageName);
        stream.Write(item.ClassName);
        stream.Write((short)item.OuterIndex);
        stream.Write(item.ObjectName);
    }

    public override void Deserialize(IUnrealStream stream, UImportTableItem item)
    {
        // OuterIndex changed to 16 bits.
        if (stream.Version < 141)
        {
            item.Deserialize(stream);

            return;
        }

        item.ClassPackageName = stream.ReadNameReference();
        item.ClassName = stream.ReadNameReference();
        item.OuterIndex = stream.ReadInt16();
        item.ObjectName = stream.ReadNameReference();
    }

    public override void Serialize(IUnrealStream stream, UExportTableItem item)
    {
        // OuterIndex changed to 16 bits.
        if (stream.Version < 141)
        {
            item.Serialize(stream);

            return;
        }

        stream.WriteIndex((short)item.ClassIndex);
        stream.WriteIndex((short)item.SuperIndex);
        stream.Write((short)item.OuterIndex);

        stream.Write(item.ObjectName);
        stream.Write((uint)item.ObjectFlags);

        stream.WriteIndex(item.SerialSize);
        if (item.SerialSize > 0)
        {
            stream.WriteIndex(item.SerialOffset);
        }
    }

    public override void Deserialize(IUnrealStream stream, UExportTableItem item)
    {
        // OuterIndex changed to 16 bits.
        if (stream.Version < 141)
        {
            item.Deserialize(stream);

            return;
        }

        item.ClassIndex = (short)stream.ReadIndex();
        item.SuperIndex = (short)stream.ReadIndex();
        item.OuterIndex = stream.ReadInt16();

        item.ObjectName = stream.ReadNameReference();
        item.ObjectFlags = stream.ReadUInt32();

        item.SerialSize = stream.ReadIndex();
        if (item.SerialSize > 0)
        {
            item.SerialOffset = stream.ReadIndex();
        }
    }
}
