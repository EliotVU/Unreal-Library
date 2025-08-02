using System.Diagnostics;

namespace UELib.Branch.UE4;

public class PackageSerializerUE4 : IPackageSerializer
{
    private const int MaxNameLengthUE4 = 1024;

    public void Serialize(IUnrealStream stream, UNameTableItem item)
    {
        stream.Write(item.Name);

        // TODO: Re-calculate these for situations where the name may have been modified...
        if (stream.UE4Version < 504)
        {
            return;
        }

        stream.Write(item.NonCasePreservingHash);
        stream.Write(item.CasePreservingHash);
    }

    public void Deserialize(IUnrealStream stream, UNameTableItem item)
    {
        item.Name = stream.ReadString();
        Debug.Assert(item.Name.Length <= MaxNameLengthUE4,
            "Maximum name length exceeded! Possible corrupt or unsupported package.");

        if (stream.UE4Version < 504)
        {
            return;
        }

        item.NonCasePreservingHash = stream.ReadUInt16();
        item.CasePreservingHash = stream.ReadUInt16();
    }

    public void Serialize(IUnrealStream stream, UImportTableItem item)
    {
        stream.Write(item.ClassPackageName);
        stream.Write(item.ClassName);
        stream.Write(item.OuterIndex);
        stream.Write(item.ObjectName);

        if (stream.UE4Version >= 520 && !stream.Package.ContainsEditorData())
        {
            stream.Write(item.ObjectPackageName);
        }
    }

    public void Deserialize(IUnrealStream stream, UImportTableItem item)
    {
        item.ClassPackageName = stream.ReadName();
        item.ClassName = stream.ReadName();
        item.OuterIndex = stream.ReadInt32();
        item.ObjectName = stream.ReadName();

        if (stream.UE4Version >= 520 && !stream.Package.ContainsEditorData())
        {
            item.ObjectPackageName = stream.ReadName();
        }
    }

    public void Serialize(IUnrealStream stream, UExportTableItem item)
    {
        stream.Write((int)item.ClassIndex);
        stream.Write((int)item.SuperIndex);
        if (stream.UE4Version >= 508)
        {
            stream.Write((int)item.TemplateIndex);
        }

        stream.Write((int)item.OuterIndex);
        stream.Write(item.ObjectName);

        if (stream.UE4Version < 142)
        {
            stream.Write((int)item.ArchetypeIndex);
        }

        stream.Write(item.ObjectFlags);

        if (stream.UE4Version >= 511)
        {
            stream.Write((long)item.SerialSize);
            stream.Write((long)item.SerialOffset);
        }
        else
        {
            stream.Write(item.SerialSize);
            stream.Write(item.SerialOffset);
        }

        stream.Write(item.IsForcedExport);
        stream.Write(item.IsNotForServer);
        stream.Write(item.IsNotForClient);

        if (stream.UE4Version < 196)
        {
            stream.WriteArray(item.GenerationNetObjectCount);
        }

        stream.WriteStruct(ref item.PackageGuid);
        stream.Write(item.PackageFlags);

        if (stream.UE4Version >= 365)
        {
            stream.Write(item.IsNotForEditorGame);
        }

        if (stream.UE4Version >= 485)
        {
            stream.Write(item.IsAsset);
        }

        if (stream.UE4Version >= 507)
        {
            stream.Write(item.FirstExportDependency);
            stream.Write(item.SerializationBeforeSerializationDependencies);
            stream.Write(item.CreateBeforeSerializationDependencies);
            stream.Write(item.SerializationBeforeCreateDependencies);
            stream.Write(item.CreateBeforeCreateDependencies);
        }
    }

    public void Deserialize(IUnrealStream stream, UExportTableItem item)
    {
        item.ClassIndex = stream.ReadInt32();
        item.SuperIndex = stream.ReadInt32();
        if (stream.UE4Version >= 508)
        {
            item.TemplateIndex = stream.ReadInt32();
        }

        item.OuterIndex = stream.ReadInt32();
        item.ObjectName = stream.ReadName();

        if (stream.UE4Version < 142)
        {
            item.ArchetypeIndex = stream.ReadInt32();
        }

        item.ObjectFlags = stream.ReadUInt32();

        if (stream.UE4Version >= 511)
        {
            // FIXME: Store as long
            item.SerialSize = (int)stream.ReadInt64();
            item.SerialOffset = (int)stream.ReadInt64();
        }
        else
        {
            item.SerialSize = stream.ReadInt32();
            item.SerialOffset = stream.ReadInt32();
        }

        item.IsForcedExport = stream.ReadBool();
        item.IsNotForServer = stream.ReadBool();
        item.IsNotForClient = stream.ReadBool();

        if (stream.UE4Version < 196)
        {
            stream.ReadArray(out item.GenerationNetObjectCount);
        }

        stream.ReadStruct(out item.PackageGuid);
        stream.Read(out item.PackageFlags);

        if (stream.UE4Version >= 365)
        {
            item.IsNotForEditorGame = stream.ReadBool();
        }

        if (stream.UE4Version >= 485)
        {
            item.IsAsset = stream.ReadBool();
        }

        if (stream.UE4Version >= 507)
        {
            item.FirstExportDependency = stream.ReadInt32();
            item.SerializationBeforeSerializationDependencies = stream.ReadInt32();
            item.CreateBeforeSerializationDependencies = stream.ReadInt32();
            item.SerializationBeforeCreateDependencies = stream.ReadInt32();
            item.CreateBeforeCreateDependencies = stream.ReadInt32();
        }
    }
}
