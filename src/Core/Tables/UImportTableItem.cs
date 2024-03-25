using System;
using UELib.Core;

namespace UELib
{
    /// <summary>
    /// An import table entry, represents a @UObject import within a package.
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

        [Obsolete] protected override string __ClassName => _ClassName;
        [Obsolete] protected override int __ClassIndex => (int)_ClassName;

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

        public override string GetReferencePath()
        {
            return $"{_ClassName}'{GetPath()}'";
        }

        public override string ToString()
        {
            return $"{ObjectName}({-(Index + 1)})";
        }

        [Obsolete("Use ToString()")]
        public string ToString(bool v)
        {
            return ToString();
        }

        public static explicit operator int(UImportTableItem item)
        {
            return -item.Index;
        }
    }
}
