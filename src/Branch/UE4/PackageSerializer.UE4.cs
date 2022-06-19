using System.Diagnostics;
using UELib.Core;

namespace UELib.Branch.UE4
{
    public class PackageSerializerUE4 : IPackageSerializer
    {
        private const int MaxNameLengthUE4 = 1024;
        
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
            stream.Write(item.Name);

            // TODO: Re-calculate these for situations where the name may have been modified...
            if (stream.UE4Version < 504) return;
            stream.Write(item.NonCasePreservingHash);
            stream.Write(item.CasePreservingHash);
        }

        public void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            item.Name = stream.ReadText();
            Debug.Assert(item.Name.Length <= MaxNameLengthUE4, "Maximum name length exceeded! Possible corrupt or unsupported package.");
            
            if (stream.UE4Version < 504) return;
            item.NonCasePreservingHash = stream.ReadUInt16();
            item.CasePreservingHash = stream.ReadUInt16();
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
            item.ClassIndex = stream.ReadInt32();
            item.SuperIndex = stream.ReadInt32();
            if (stream.UE4Version >= 508) item.TemplateIndex = stream.ReadInt32();
            item.OuterIndex = stream.ReadInt32();
            item.ObjectName = stream.ReadNameReference();

            if (stream.UE4Version < 142) item.ArchetypeIndex = stream.ReadInt32();

            item.ObjectFlags = stream.ReadUInt32();

            if (stream.UE4Version >= 511)
            {
                item.SerialSize = (int)stream.ReadInt64();
                item.SerialOffset = (int)stream.ReadInt64();
            }
            else
            {
                item.SerialSize = stream.ReadInt32();
                item.SerialOffset = stream.ReadInt32();
            }

            item.IsForcedExport = stream.ReadInt32() > 0;
            item.IsNotForServer = stream.ReadInt32() > 0;
            item.IsNotForClient = stream.ReadInt32() > 0;
            if (stream.UE4Version < 196)
            {
                stream.ReadArray(out UArray<int> generationNetObjectCount);
            }

            item.PackageGuid = stream.ReadGuid();
            item.PackageFlags = stream.ReadUInt32();
            if (stream.UE4Version >= 365) item.IsNotForEditorGame = stream.ReadInt32() > 0;
            if (stream.UE4Version >= 485) item.IsAsset = stream.ReadInt32() > 0;
            if (stream.UE4Version >= 507)
            {
                int firstExportDependency = stream.ReadInt32();
                int serializationBeforeSerializationDependencies = stream.ReadInt32();
                int createBeforeSerializationDependencies = stream.ReadInt32();
                int serializationBeforeCreateDependencies = stream.ReadInt32();
                int createBeforeCreateDependencies = stream.ReadInt32();
            }
        }
    }
}