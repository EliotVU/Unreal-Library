namespace UELib
{
    /// <summary>
    /// An import table entry, representing a @UObject dependency in a package.
    /// This includes the name of the package that this dependency belongs to.
    /// </summary>
    public sealed class UImportTableItem : UObjectTableItem, IUnrealSerializableClass
    {
        #region Serialized Members

        private UName _PackageName;
        public UName PackageName
        {
            get => _PackageName;
            set => _PackageName = value;
        }

        private UName _ClassName;
        public UName ClassName
        {
            get => _ClassName;
            set => _ClassName = value;
        }

        #endregion

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(_PackageName);
            stream.Write(_ClassName);
            stream.Write(_OuterIndex); // Always an ordinary integer
            stream.Write(_ObjectName);
        }

        public void Deserialize(IUnrealStream stream)
        {
            _PackageName = stream.ReadNameReference();
            _ClassName = stream.ReadNameReference();
            _OuterIndex = stream.ReadInt32(); // ObjectIndex, though always written as 32bits regardless of build.
            _ObjectName = stream.ReadNameReference();
        }

        public override string ToString()
        {
            return $"{ObjectName}({-(Index + 1)})";
        }

        public static explicit operator int(UImportTableItem item)
        {
            return -item.Index;
        }
    }
}