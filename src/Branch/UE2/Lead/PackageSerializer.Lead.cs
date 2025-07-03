namespace UELib.Branch.UE2.Lead
{
    public class PackageSerializerLead : PackageSerializerBase
    {
        public override void Serialize(IUnrealStream stream, UNameTableItem item)
        {
            stream.WriteIndex((byte)item.Name.Length);
            stream.Write(UnrealEncoding.ANSI.GetBytes(item.Name));
            stream.Write((uint)item.Flags);
        }

        public override void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            int length = stream.ReadIndex();

            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);

            string name = new(UnrealEncoding.ANSI.GetChars(buffer), 0, length);

            item.Name = name;
            item.Flags = stream.ReadUInt32();
        }

        public override void Serialize(IUnrealStream stream, UImportTableItem item)
        {
            item.Serialize(stream);
        }

        public override void Deserialize(IUnrealStream stream, UImportTableItem item)
        {
            item.Deserialize(stream);
        }

        public override void Serialize(IUnrealStream stream, UExportTableItem item)
        {
            item.Serialize(stream);
        }

        public override void Deserialize(IUnrealStream stream, UExportTableItem item)
        {
            item.Deserialize(stream);
        }
    }
}
