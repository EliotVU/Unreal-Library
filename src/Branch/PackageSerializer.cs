namespace UELib.Branch
{
    /// <summary>
    /// Provides a way to override the default package Serialize/Deserialize methods for a particular <see cref="EngineBranch"/>.
    /// </summary>
    public interface IPackageSerializer
    {
        // Not yet called
        void Serialize(IUnrealStream stream, IUnrealSerializableClass obj);
        
        // Not yet called
        void Deserialize(IUnrealStream stream, IUnrealDeserializableClass obj);

        void Serialize(IUnrealStream stream, UNameTableItem item);
        void Deserialize(IUnrealStream stream, UNameTableItem item);
        void Serialize(IUnrealStream stream, UImportTableItem item);
        void Deserialize(IUnrealStream stream, UImportTableItem item);
        void Serialize(IUnrealStream stream, UExportTableItem item);
        void Deserialize(IUnrealStream stream, UExportTableItem item);
    }
}