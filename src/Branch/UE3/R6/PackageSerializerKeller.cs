using System.Diagnostics.Contracts;
using System.Text;
using UELib.Annotations;

namespace UELib.Branch.UE3.R6
{
    [UsedImplicitly]
    public class PackageSerializerKeller : PackageSerializerBase
    {
        public override void Serialize(IUnrealStream stream, UNameTableItem item)
        {
            if (stream.LicenseeVersion < 71)
            {
                item.Serialize(stream);

                return;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(item.Name);
            Contract.Assert(buffer.Length <= 0xFF);
            stream.Write((byte)buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        public override void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            if (stream.LicenseeVersion < 71)
            {
                item.Deserialize(stream);

                return;
            }

            byte[] buffer;
            stream.Read(out byte length);
            stream.Read(buffer = new byte[length], 0, length);
            item.Name = new string(Encoding.UTF8.GetChars(buffer));
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
