using System;
using UELib.Branch;
using UELib.Core;
using UELib.Flags;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USoundCue/Engine.SoundCue
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USoundCue : UObject
    {
        #region Script Properties

        [UnrealProperty]
        public UObject?/*USoundNode*/ FirstNode { get; set; }

        #endregion

        #region Serialized Members

        // Using ValueType to allow for null keys in the map.
        [StreamRecord]
        public UMap<ValueTuple<UObject?>/*USoundNode*/, NodeEditorData>? EditorData { get; set; }

        #endregion

        public USoundCue()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (Package.Summary.PackageFlags.HasFlag(PackageFlag.Cooked)
#if REMEMBERME
                // Early builds are missing the cooked guard.
                && stream.Build != UnrealPackage.GameBuild.BuildName.RememberMe
#endif
            )
            {
                return;
            }

            EditorData = stream.ReadMap(() => ValueTuple.Create(stream.ReadObject()), stream.ReadStruct<NodeEditorData>);
            stream.Record(nameof(EditorData), EditorData);
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (Package.Summary.PackageFlags.HasFlag(PackageFlag.Cooked)
#if REMEMBERME
                // Early builds are missing the cooked guard.
                && stream.Build != UnrealPackage.GameBuild.BuildName.RememberMe
#endif
            )
            {
                return;
            }

            stream.WriteMap(EditorData, key => stream.WriteObject(key.Item1), stream.WriteStruct);
        }

        /// <summary>
        ///     Implements Engine.SoundCue.SoundNodeEditorData
        /// </summary>
        public record struct NodeEditorData : IUnrealSerializableClass
        {
            public int X, Y;

            public void Deserialize(IUnrealStream stream)
            {
                stream.Read(out X);
                stream.Read(out Y);
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(X);
                stream.Write(Y);
            }
        }
    }
}
