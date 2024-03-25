namespace UELib.Branch
{
    /// <summary>
    /// Simply redirects all serialize/deserialize calls to the default implementation.
    /// This is useful so that we can ensure that there's always a valid <see cref="EngineBranch.Serializer"/>
    /// 
    /// For overriding purposes see <see cref="PackageSerializerBase"/>
    /// </summary>
    public sealed class DefaultPackageSerializer : IPackageSerializer
    {
        public void Serialize(IUnrealStream stream, IUnrealSerializableClass obj)
        {
            obj.Serialize(stream);
        }

        public void Deserialize(IUnrealStream stream, IUnrealDeserializableClass obj)
        {
            obj.Deserialize(stream);
        }

        public void Serialize(IUnrealStream stream, UNameTableItem item)
        {
            item.Serialize(stream);
        }

        public void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            item.Deserialize(stream);
        }

        public void Serialize(IUnrealStream stream, UImportTableItem item)
        {
            item.Serialize(stream);
        }

        public void Deserialize(IUnrealStream stream, UImportTableItem item)
        {
            item.Deserialize(stream);
        }

        public void Serialize(IUnrealStream stream, UExportTableItem item)
        {
            item.Serialize(stream);
        }

        public void Deserialize(IUnrealStream stream, UExportTableItem item)
        {
            item.Deserialize(stream);
        }
    }
}