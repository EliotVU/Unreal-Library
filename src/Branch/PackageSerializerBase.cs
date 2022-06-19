namespace UELib.Branch
{
    public abstract class PackageSerializerBase : IPackageSerializer
    {
        public virtual void Serialize(IUnrealStream stream, IUnrealSerializableClass obj)
        {
            obj.Serialize(stream);
        }

        public virtual void Deserialize(IUnrealStream stream, IUnrealDeserializableClass obj)
        {
            obj.Deserialize(stream);
        }

        public abstract void Serialize(IUnrealStream stream, UNameTableItem item);
        public abstract void Deserialize(IUnrealStream stream, UNameTableItem item);
        public abstract void Serialize(IUnrealStream stream, UImportTableItem item);
        public abstract void Deserialize(IUnrealStream stream, UImportTableItem item);
        public abstract void Serialize(IUnrealStream stream, UExportTableItem item);
        public abstract void Deserialize(IUnrealStream stream, UExportTableItem item);
    }
}