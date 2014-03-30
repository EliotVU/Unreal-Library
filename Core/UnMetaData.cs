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
        public sealed class UMetaField : IUnrealDecompilable, IUnrealSerializableClass
        {
            public int					FieldIndex;			// ObjectIndex		
            public string				FieldName;			// UT3, Mirrors Edge
            public UArray<UMetaTag> 	MetaTags;
            public UnrealPackage		Owner;

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
                    FieldIndex = stream.ReadObjectIndex();
                }
                MetaTags = new UArray<UMetaTag>();
                MetaTags.Deserialize( stream, tag => tag.Owner = Owner );
            }

            /// <summary>
            /// Decompiles this object into a text format of:
            /// 
            /// LeftArrow
            ///		"ForEach MetaTags"
            ///		Tag.Decompile()|
            ///	RightArrow
            /// </summary>
            /// <returns></returns>
            public string Decompile()
            {
                var tags = new List<UMetaTag>( MetaTags.Count );
                tags.AddRange( MetaTags );

                // We remove this because OrderIndex is an implicit added MetaTag.
                var oItag = GetMetaTag( "OrderIndex" );
                if( oItag != null )
                {
                    tags.Remove( oItag );
                }

                oItag = GetMetaTag( "ToolTip" );
                if( oItag != null )
                {
                    tags.Remove( oItag );
                }

                /*OItag = GetMetaTag( "ToolTip" );
                if( OItag != null )
                {
                    tags.Remove( OItag );
                }*/

                if( tags.Count <= 0 ) 
                    return String.Empty;

                var output = "<";
                foreach( var tag in tags )
                {
                    output += tag.Decompile() + (tag != tags.Last() ? "|" : ">");
                }
                return output;
            }

            public UMetaTag GetMetaTag( string tagName )
            {
                return MetaTags.Find( tag => tag.TagName == tagName );
            }
        }															    

        public sealed class UMetaTag : IUnrealDecompilable, IUnrealSerializableClass
        {
            public UName			TagName;
            public string 			TagValue;
            public UnrealPackage	Owner;

            public void Serialize( IUnrealStream stream )
            {
                throw new NotImplementedException();
            }

            public void Deserialize( IUnrealStream stream )
            {
                TagName = stream.ReadNameReference();
                TagValue = stream.ReadText();
            }

            /// <summary>
            /// Decompiles this object into a text format of:
            /// 
            /// TagName=TagValue
            /// </summary>
            /// <returns></returns>
            public string Decompile()
            {								
                return TagName + "=" + TagValue;
            }
        }

        private UArray<UMetaField> _MetaFields;
        #endregion

        #region Constructors
        protected override void Deserialize()
        {
            base.Deserialize();
            _MetaFields = new UArray<UMetaField>();
            _MetaFields.Deserialize( _Buffer, field => field.Owner = Package );
        }

        public override void PostInitialize()
        {
            base.PostInitialize();
            // Link the metas to the owners
            foreach( var metaProp in _MetaFields )
            {
                var field = (UField)Package.GetIndexObject( metaProp.FieldIndex );
                if( field != null )
                {
                    field.Meta = metaProp;
                }
            }
        }
        #endregion

        #region Decompilation
        /// <summary>
        /// Decompiles this object into a text format of:
        /// 
        ///	Meta Count _MetaFields.Count
        ///		
        /// "ForEach _MetaFields"
        /// 
        ///		fieldname+Field.Decompile()
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            // UE3 Debug
            BeginDeserializing();

            if( _MetaFields == null )
            {
                return "No MetaFields found!";
            }

            var content = FormatHeader();
            foreach( var field in _MetaFields )
            {
                var fieldname = field.FieldIndex != 0 ? GetIndexObject( field.FieldIndex ).GetOuterGroup() : field.FieldName;																			  // Max string length!
                var fieldOutput = field.Decompile();
                if( fieldOutput.Length != 0 )
                {
                    content += "\r\n" + fieldname + field.Decompile();
                }
            }	
            return content;
        }

        protected override string FormatHeader()
        {
            return _MetaFields == null ? "No MetaFields found!" : "Meta count: " + _MetaFields.Count + "\r\n";
        }

        public string GetUniqueMetas()
        {
            var output = String.Empty;
            var tags = new List<UMetaTag>();
            foreach( var field in _MetaFields )
            {
                foreach( var dfield in field.MetaTags )
                {
                    var ut = new UMetaTag{Owner = dfield.Owner};
                    if( tags.Find( tag => tag.TagName == dfield.TagName ) != null )
                    {
                        continue;
                    }			
                    ut.TagName = dfield.TagName;
                    ut.TagValue = field.FieldName;
                    tags.Add( ut );
                }
            }

            foreach( var ut in tags )
            {
                var tagsOutput = ut.Decompile().TrimEnd( '=' );
                if( tagsOutput.Length != 0 )
                {
                    output += tagsOutput + "\r\n";
                }
            }
            return output;
        }
        #endregion
    }
}
