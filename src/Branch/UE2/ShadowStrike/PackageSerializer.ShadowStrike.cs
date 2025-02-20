namespace UELib.Branch.UE2.ShadowStrike
{
    public class PackageSerializerShadowStrike : PackageSerializerBase
    {
        public override void Serialize(IUnrealStream stream, UNameTableItem item)
        {
            item.Serialize(stream);
        }

        public override void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            item.Deserialize(stream);
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
