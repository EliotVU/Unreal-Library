using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace UELib
{
    /// <summary>
    /// An import table entry, representing a @UObject dependency in a package.
    /// This includes the name of the package that this dependency belongs to.
    /// </summary>
    public sealed class UImportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        #region Serialized Members

        public UName PackageName;
        private UName _ClassName;

        [Pure]
        public override string ClassName => _ClassName;

        #endregion

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(PackageName);
            stream.Write(_ClassName);
            stream.Write(OuterTable != null ? (int)OuterTable.Object : 0); // Always an ordinary integer
            stream.Write(ObjectName);
        }

        public void Deserialize(IUnrealStream stream)
        {
#if AA2
            // Not attested in packages of LicenseeVersion 32
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.AA2
                && stream.Package.LicenseeVersion >= 33)
            {
                PackageName = stream.ReadNameReference();
                _ClassName = stream.ReadNameReference();
                ClassIndex = (int)_ClassName;
                byte unkByte = stream.ReadByte();
                Debug.WriteLine(unkByte, "unkByte");
                ObjectName = stream.ReadNameReference();
                OuterIndex = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
                return;
            }
#endif

            PackageName = stream.ReadNameReference();
            _ClassName = stream.ReadNameReference();
            ClassIndex = (int)_ClassName;
            OuterIndex = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
            ObjectName = stream.ReadNameReference();
        }

        #region Methods

        public override string ToString()
        {
            return $"{ObjectName}({-(Index + 1)})";
        }

        #endregion
    }
}