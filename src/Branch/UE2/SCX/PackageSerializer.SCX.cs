using System.Text;

namespace UELib.Branch.UE2.SCX
{
    public class PackageSerializerSCX : PackageSerializerBase
    {
        public override void Serialize(IUnrealStream stream, UNameTableItem item)
        {
            stream.Write((byte)item.Name.Length);
            stream.Write(Encoding.ASCII.GetBytes(item.Name));
            stream.Write((byte)0x00);
            stream.Write((uint)item.Flags);
        }

        public override void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            // With SC3 (v85) the null character is no longer included with its length
            int length = stream.ReadByte() + 1;

            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, length);

            string name = new(Encoding.ASCII.GetChars(buffer), 0, length - 1);

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
