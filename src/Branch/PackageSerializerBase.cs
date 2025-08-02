namespace UELib.Branch
{
    public abstract class PackageSerializerBase : IPackageSerializer
    {
        public abstract void Serialize(IUnrealStream stream, UNameTableItem item);
        public abstract void Deserialize(IUnrealStream stream, UNameTableItem item);
        public abstract void Serialize(IUnrealStream stream, UImportTableItem item);
        public abstract void Deserialize(IUnrealStream stream, UImportTableItem item);
        public abstract void Serialize(IUnrealStream stream, UExportTableItem item);
        public abstract void Deserialize(IUnrealStream stream, UExportTableItem item);
    }
}