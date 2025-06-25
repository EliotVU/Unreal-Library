using UELib.Branch;
using UELib.Flags;

namespace UELib.Core
{
    /// <summary>
    ///     Implements USoundCue/Engine.SoundCue
    /// </summary>
    [UnrealRegisterClass]
    [BuildGeneration(BuildGeneration.UE3)]
    public class USoundCue : UObject
    {
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

        // UnrealProperty
        public UObject/*USoundNode*/ FirstNode;

        #region Serialized Members

        // Serialized
        public UMap<UObject/*USoundNode*/, NodeEditorData>? EditorData { get; set; }

        #endregion

        public USoundCue()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            if (Package.IsConsoleCooked())
            {
                return;
            }

            if (EditorData == null)
            {
                stream.Write(0);
            }
            else
            {
                stream.Write(EditorData.Count);
                foreach (var editorData in EditorData)
                {
                    stream.Write(editorData.Key);
                    var nodeEditorData = editorData.Value;
                    stream.WriteStruct(ref nodeEditorData);
                }
            }
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            if (Package.Summary.PackageFlags.HasFlag(PackageFlag.Cooked))
            {
                return;
            }

            int c = stream.ReadInt32();
            EditorData = new UMap<UObject, NodeEditorData>(c);
            for (int i = 0; i < c; ++i)
            {
                stream.Read(out UObject key);
                stream.ReadStruct(out NodeEditorData value);

                // Can be null sometimes.
                if (key == null)
                {
                    continue;
                }

                EditorData.Add(key, value);
            }

            stream.Record(nameof(EditorData), EditorData);
        }
    }
}
