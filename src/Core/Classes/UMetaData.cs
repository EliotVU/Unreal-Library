using System;
using System.Collections.Generic;
using System.Linq;

namespace UELib.Core
{
    /// <summary>
    /// MetaData objects contain all the metadata of UField objects.
    /// </summary>
    [UnrealRegisterClass]
    public sealed class UMetaData : UObject
    {
        #region Serialized Members
        public sealed class UFieldData : IUnrealDecompilable, IUnrealSerializableClass
        {
            private string FieldName;

            public UField Field;

            // Dated qualified identifier to this meta data's field. e.g. UT3, Mirrors Edge
            public Dictionary<UName, string> TagsMap;

            public void Serialize( IUnrealStream stream )
            {
                throw new NotImplementedException();
            }

            public void Deserialize( IUnrealStream stream )
            {
                if( stream.Version <= 540 )
                {
                    // e.g. Core.Object.X
                    FieldName = stream.ReadText();
                }
                else
                {
                    // TODO: Possibly linked to a non-ufield?
                    Field = (UField)stream.ReadObject();
                    Field.MetaData = this;
                }

                var length = stream.ReadInt32();
                TagsMap = new Dictionary<UName, string>(length);
                for (var i = 0; i < length; ++ i) {
                    var key = stream.ReadNameReference();
                    var value = stream.ReadText();
                    TagsMap.Add(key, value);
                }
            }

            public string Decompile()
            {
                var tags = new List<string>(TagsMap.Count);
                foreach (var tag in TagsMap) {
                    if (tag.Key == "OrderIndex" || tag.Key == "ToolTip") {
                        continue;
                    }

                    tags.Add(tag.Key + "=" + tag.Value);
                }

                if( tags.Count <= 0 )
                    return String.Empty;

                return "<" + string.Join("|", tags) + ">";
            }

            public override string ToString()
            {
                return Field != null ? Field.GetOuterGroup() : FieldName;
            }
        }

        public UArray<UFieldData> MetaObjects;
        #endregion

        #region Constructors
        protected override void Deserialize()
        {
            base.Deserialize();
            MetaObjects = new UArray<UFieldData>();
            MetaObjects.Deserialize( _Buffer );
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
            if( MetaObjects == null )
            {
                return "";
            }
            return string.Join("\r\n", MetaObjects.ConvertAll<string>(data => data.ToString() + data.Decompile()));
        }
        #endregion
    }
}