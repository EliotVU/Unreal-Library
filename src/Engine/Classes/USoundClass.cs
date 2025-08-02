using System;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements USoundClass/Engine.SoundClass
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USoundClass : UObject
    {
        #region Serialized Members

        // Using ValueType to allow for null keys in the map.
        [StreamRecord]
        public UMap<ValueTuple<UObject?>/*USoundNode*/, USoundCue.NodeEditorData>? EditorData { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedEditorDataToUSoundClass)
            {
                EditorData = stream.ReadMap(() => ValueTuple.Create(stream.ReadObject()), stream.ReadStruct<USoundCue.NodeEditorData>);
                stream.Record(nameof(EditorData), EditorData);
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedEditorDataToUSoundClass)
            {
                stream.WriteMap(EditorData, key => stream.WriteObject(key.Item1), stream.WriteStruct);
            }
        }
    }
}
