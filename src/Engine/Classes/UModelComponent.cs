using System;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UModelComponent/Engine.ModelComponent
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class UModelComponent : UPrimitiveComponent
    {
        #region Serialized Members

        [StreamRecord]
        public UObject Model { get; set; }

        [StreamRecord]
        public int ZoneIndex { get; set; }

        #endregion

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Model = stream.ReadObject();
            stream.Record(nameof(Model), Model);

            ZoneIndex = stream.ReadInt32();
            stream.Record(nameof(ZoneIndex), ZoneIndex);

            // TODO: Elements (structure not implemented), ComponentIndex, Nodes
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(Model);
            stream.Write(ZoneIndex);

            throw new NotSupportedException("Serialization of UModelComponent is not implemented yet.");
        }
    }
}
