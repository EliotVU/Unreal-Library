using System;
using UELib.Branch;

namespace UELib
{
    public struct UGenerationTableItem : IUnrealSerializableClass
    {
        private int _ExportCount;
        private int _NameCount;
        private int _NetObjectCount;

        public int ExportCount
        {
            get => _ExportCount;
            set => _ExportCount = value;
        }

        public int NameCount
        {
            get => _NameCount;
            set => _NameCount = value;
        }

        public int NetObjectCount
        {
            get => _NetObjectCount;
            set => _NetObjectCount = value;
        }

        [Obsolete] public object ExportsCount => ExportCount;
        [Obsolete] public object NamesCount => NameCount;
        [Obsolete] public object NetObjectsCount => NetObjectCount;
        
        [Obsolete] public const int VNetObjectsCount = 322;

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(_ExportCount);
            stream.Write(_NameCount);
            if (stream.Version >= (uint)PackageObjectLegacyVersion.NetObjectCountAdded &&
                stream.UE4Version < 186)
            {
                stream.Write(_NetObjectCount);
            }
        }

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out _ExportCount);
            stream.Read(out _NameCount);
            if (stream.Version >= (uint)PackageObjectLegacyVersion.NetObjectCountAdded &&
                stream.UE4Version < 186)
            {
                stream.Read(out _NetObjectCount);
            }
        }
    }
}