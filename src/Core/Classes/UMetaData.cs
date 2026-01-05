using System.Diagnostics.Contracts;
using UELib.Branch;
using UELib.ObjectModel.Annotations;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UMetaData/Core.MetaData
    /// </summary>
    [UnrealRegisterClass(InternalClassFlags.Preload)]
    [BuildGeneration(BuildGeneration.UE3)]
    public sealed class UMetaData : UObject
    {
        #region Serialized Members

        [StreamRecord]
        public UMap<ObjectMetaKey, ObjectTags> ObjectTagsMap { get; set; } = [];

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            ObjectTagsMap = stream.ReadMap(stream.ReadStruct<ObjectMetaKey>, stream.ReadClass<ObjectTags>);
            stream.Record(nameof(ObjectTagsMap), ObjectTagsMap);

            foreach (var pair in ObjectTagsMap)
            {
                if (pair.Key.Object is UField field) field.MetaData = pair.Value;
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.WriteMap(ObjectTagsMap, stream.WriteStruct, stream.WriteClass);
        }

        public override string Decompile()
        {
            return string.Join("\r\n", ObjectTagsMap.Select(pair => pair.Key + pair.Value.Decompile()));
        }

        public struct ObjectMetaKey : IUnrealSerializableClass
        {
            public UObject Object { get; private set; }

            /// <summary>
            ///     The path to the object, e.g. "Core.Object:Vector.X".
            ///     Legacy support for versions before <see cref="PackageObjectLegacyVersion.ChangedUMetaDataObjectPathToReference" />
            /// </summary>
            private string? _LegacyObjectPath;

            // ReSharper disable once ConvertToPrimaryConstructor
            public ObjectMetaKey(UObject obj)
            {
                Object = obj ?? throw new ArgumentNullException(nameof(obj));
            }

            public void Deserialize(IUnrealStream stream)
            {
                if (stream.Version >= (uint)PackageObjectLegacyVersion.ChangedUMetaDataObjectPathToReference)
                {
                    Object = stream.ReadObject<UObject>();

                    return;
                }

                _LegacyObjectPath = stream.ReadString();
            }

            public void Serialize(IUnrealStream stream)
            {
                if (stream.Version >= (uint)PackageObjectLegacyVersion.ChangedUMetaDataObjectPathToReference)
                {
                    Contract.Assert(Object != null, "Cannot re-serialize from ObjectName to Object");
                    stream.WriteObject(Object);

                    return;
                }

                // FIXME: GetPath produces "Core.Object.Vector.X" instead of "Core.Object:Vector.X"
                stream.WriteString(_LegacyObjectPath ?? Object.GetPath());
            }

            public override int GetHashCode()
            {
                return Object?.GetHashCode() ?? _LegacyObjectPath!.GetHashCode();
            }

            public override string ToString()
            {
                return Object?.GetPath() ?? _LegacyObjectPath!;
            }
        }

        public sealed class ObjectTags : IUnrealSerializableClass, IUnrealDecompilable
        {
            public UMap<UName, string>? Tags { get; set; }

            public void Deserialize(IUnrealStream stream)
            {
                Tags = stream.ReadMap(stream.ReadName, stream.ReadString);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.WriteMap(Tags, key => stream.WriteName(key), stream.WriteString);
            }

            public string Decompile()
            {
                if (Tags == null || Tags.Count == 0) return string.Empty;

                // Filter out compiler-generated tags
                var tags = Tags
                           .Where(tag =>
                                  tag.Key != UnrealName.OrderIndex &&
                                  tag.Key != UnrealName.Tooltip)
                           .ToList()
                           .ConvertAll(tag => $"{tag.Key}={tag.Value}");

                return tags.Count == 0
                    ? string.Empty
                    : $"<{string.Join("|", tags)}>";
            }
        }
    }
}
