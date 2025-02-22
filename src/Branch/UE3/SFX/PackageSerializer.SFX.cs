using UELib.Annotations;

namespace UELib.Branch.UE3.SFX
{
    [UsedImplicitly]
    public class PackageSerializerSFX : PackageSerializerBase
    {
        public override void Serialize(IUnrealStream stream, UNameTableItem item)
        {
            stream.Write(item._Name);
            if (stream.LicenseeVersion >= 142 && stream.LicenseeVersion != 1008)
            {
                return;
            }

            if (stream.LicenseeVersion >= 102)
            {
                stream.Write((uint)item._Flags);

                return;
            }

            stream.Write(item._Flags);
        }

        // FIXME: Fails on Mass Effect 2 (513, 0130)
        public override void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            stream.Read(out item._Name);
            if (stream.LicenseeVersion >= 142 && stream.LicenseeVersion != 1008)
            {
                return;
            }

            if (stream.LicenseeVersion >= 102)
            {
                stream.Read(out uint flags);
                item._Flags = flags;

                return;
            }
            
            stream.Read(out item._Flags);
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
