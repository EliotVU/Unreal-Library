using System;
using System.Linq;

namespace UELib.Core
{
    /// <summary>
    /// MetaData objects contain all the metadata of UField objects.
    /// </summary>
    [UnrealRegisterClass]
    public sealed class UMetaData : UObject
    {
        public sealed class UFieldData : IUnrealDecompilable, IUnrealSerializableClass
        {
            private string _FieldName;

            private UField _Field;

            // Dated qualified identifier to this meta data's field. e.g. UT3, Mirrors Edge
            public UMap<string, string> Tags;

            public void Serialize(IUnrealStream stream)
            {
                throw new NotImplementedException();
            }

            public void Deserialize(IUnrealStream stream)
            {
                if (stream.Version <= 540)
                {
                    // e.g. Core.Object.X
                    _FieldName = stream.ReadText();
                }
                else
                {
                    // TODO: Possibly linked to a non-ufield?
                    _Field = stream.ReadObject<UField>();
                    _Field.MetaData = this;
                }

                int length = stream.ReadInt32();
                Tags = new UMap<string, string>(length);
                for (var i = 0; i < length; ++i)
                {
                    var key = stream.ReadNameReference();
                    string value = stream.ReadText();
                    Tags.Add(key.Name, value);
                }
            }

            public string Decompile()
            {
                if (Tags.Count == 0)
                {
                    return string.Empty;
                }

                // Filter out compiler-generated tags
                var tags = Tags
                    .Where((tag) => tag.Key != "OrderIndex" && tag.Key != "ToolTip")
                    .ToList()
                    .ConvertAll((tag) => $"{tag.Key}={tag.Value}");

                return tags.Count == 0 ? string.Empty : $"<{string.Join("|", tags)}>";
            }

            public override string ToString()
            {
                return _Field != null ? _Field.GetOuterGroup() : _FieldName;
            }
        }

        #region Serialized Members
        
        private UArray<UFieldData> _Fields;

        public UArray<UFieldData> Fields
        {
            get => _Fields;
            set => _Fields = value;
        }

        #endregion

        #region Constructors

        protected override void Deserialize()
        {
            base.Deserialize();
            _Buffer.ReadArray(out _Fields);
            Record(nameof(_Fields), _Fields);
        }

        #endregion

        #region Decompilation

        /// <summary>
        /// Decompiles this object into a text format of:
        ///
        /// Meta Count _MetaFields.Count
        ///
        /// "ForEach _MetaFields"
        ///
        ///     fieldname+Field.Decompile()
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            // UE3 Debug
            BeginDeserializing();
            if (_Fields == null)
            {
                return "";
            }

            return string.Join("\r\n", _Fields.ConvertAll(data => data + data.Decompile()));
        }

        #endregion

        [Obsolete()]
        public string GetUniqueMetas()
        {
            return "";
        }
    }
}
